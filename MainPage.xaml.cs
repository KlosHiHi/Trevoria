using Mapsui;
using Mapsui.Extensions;
using Mapsui.Nts.Extensions;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.UI.Maui;
using NetTopologySuite.Geometries;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using Trevoria.Helpers;
using Trevoria.Model;

namespace Trevoria
{
    public partial class MainPage : ContentPage, INotifyPropertyChanged
    {
        private Microsoft.Maui.Graphics.Color _color = (Microsoft.Maui.Graphics.Color)Application.Current.Resources["Primary"];

        private Mapsui.Map _map;

        private Views.RouteContentView _createRoutePage = new();
        private Microsoft.Maui.Devices.Sensors.Location _userPosition = new();
        private Microsoft.Maui.Devices.Sensors.Location _whereFromPosition = new();
        private Microsoft.Maui.Devices.Sensors.Location _whereToPosition = new();

        private RouteInfo _currentRoute;
        private List<RouteInfo> _routes = new();
        private string _movementType;
        private string _routeType;
        private string _pointTo;
        private string _pointFrom;

        public event PropertyChangedEventHandler PropertyChanged;

        public List<OsmTourismPlaceModel> TouristicPlaces { get; set; }
        public List<OsmCity> LocalityTown { get; set; }

        public string RouteType
        {
            get => _routeType;
            set
            {
                if (_routeType != value)
                {
                    _routeType = value;

                    OnPropertyChanged();
                }
                ;
            }
        }
        public string MovementType
        {
            get => _movementType;
            set
            {
                if (_movementType != value)
                {
                    _movementType = value;
                    OnPropertyChanged();
                }

            }
        }
        public string PointTo
        {
            get => _pointTo;
            set
            {
                if (_pointTo != value)
                {
                    _pointTo = value;
                    OnPropertyChanged();
                }
            }
        }
        public string PointFrom
        {
            get => _pointFrom;
            set
            {
                if (_pointFrom != value)
                {
                    _pointFrom = value;
                    OnPropertyChanged();
                }
            }
        }
        private List<Coordinate> _lineCoordinates;
        private Dictionary<string, Pin> _mapPinsDictionary = new();

        private Dictionary<string, int> _natureDictionary = new()
        {
            { "park", 4 },
            { "zoo", 5 },
            { "theme_park", 5 },
            { "viewpoint", 4 },
            { "picnic_site", 3 },
            { "camp_site", 5 },
            { "nature_reserve", 4 },
            { "artwork", 2 }
        };

        private Dictionary<string, int> _artDictionary = new()
        {
            { "museum", 2 },
            { "gallery", 5 },
            { "artwork", 2 },
            { "attraction", 5 }
        };

        private Dictionary<string, int> _foodDictionary = new()
        {
            { "restaurant", 5 },
            { "cafe", 5 },
            { "bar", 5 },
            { "pub", 5 },
            { "food_court", 2 }
        };

        private Dictionary<string, int> _historyDictionary = new()
        {
            { "museum", 5 },
            { "monument", 5 },
            { "memorial", 5 },
            { "castle", 5 },
        };


        public MainPage()
        {
            InitializeComponent();
            InitAsync();


            _createRoutePage = new();
            _createRoutePage.TypeChanged += OnTypeChanged;
        }

        private void OnTypeChanged()
        {
            MovementType = _createRoutePage.MovementType;
            RouteType = _createRoutePage.RouteType;
            PointFrom = _createRoutePage.PlaceFrom;
            PointTo = _createRoutePage.PlaceTo;

            var coords = GetCityCoordinatesByName(PointFrom);
            if (coords != null)
            {
                _whereFromPosition.Latitude = coords.Value.lat;
                _whereFromPosition.Longitude = coords.Value.lon;
            }
            coords = GetCityCoordinatesByName(PointTo);

            if (coords != null)
            {
                _whereToPosition.Latitude = coords.Value.lat;
                _whereToPosition.Longitude = coords.Value.lon;
            }

            mapView.Pins.Clear();

            Dictionary<string, int> tourismTypes = RouteType switch
            {
                "nature" => _natureDictionary,
                "art" => _artDictionary,
                "food" => _foodDictionary,
                "history" => _historyDictionary,
            };

            BuildTourismRouteAsync(
                start: (_whereFromPosition.Latitude, _whereFromPosition.Longitude),
                end: (_whereToPosition.Latitude, _whereToPosition.Longitude),
                MovementType,
                tourismTypes: tourismTypes
            );
        }

        private async void InitAsync()
        {
            await LoadMap();
            LocalityTown = await GetArkhangelskCitiesFromOsmAsync();
            //TouristicPlaces = await GetArkhangelskTourismPlacesFromOsmAsync();
            await LoadCurrentRouteAsync();

            // Если есть сохранённый маршрут — отрисовать его
            if (_currentRoute != null && _currentRoute.Waypoints != null && _currentRoute.Waypoints.Count > 1)
            {
                DrawRouteByWaypoints(_currentRoute.Waypoints);
            }
        }

        private (double lat, double lon)? GetCityCoordinatesByName(string cityName)
        {
            if (string.IsNullOrWhiteSpace(cityName) || LocalityTown == null)
                return null;

            var city = LocalityTown
                .FirstOrDefault(c => string.Equals(c.Name, cityName, StringComparison.OrdinalIgnoreCase));

            if (city != null)
                return (city.Lat, city.Lon);

            return null;
        }

        private async Task LoadMap()
        {
            _userPosition = await GetUserLocationAsync();
            _map = new Mapsui.Map();
            _map.Layers.Add(OpenStreetMap.CreateTileLayer());

            UpdateCurrentUserLocation(_userPosition.Latitude, _userPosition.Longitude);

            CenterMapOnLocation(_map, _userPosition.Latitude, _userPosition.Longitude);

            mapView.Map = _map;
        }

        public async Task<List<OsmCity>> GetArkhangelskCitiesFromOsmAsync()
        {
            var cities = new List<OsmCity>();
            string overpassUrl = "https://overpass-api.de/api/interpreter";
            string query = """
    [out:json][timeout:25];
    area["ISO3166-2"="RU-ARK"]->.searchArea;
    (
      node["place"~"city|town"](area.searchArea);
    );
    out body;
    """;

            using var client = new HttpClient();
            var content = new FormUrlEncodedContent(new[]
            {
        new KeyValuePair<string, string>("data", query)
    });
            var response = await client.PostAsync(overpassUrl, content);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("elements", out var elements))
                return cities;

            foreach (var el in elements.EnumerateArray())
            {
                if (el.TryGetProperty("tags", out var tags) && tags.TryGetProperty("name", out var nameProp))
                {
                    string name = nameProp.GetString();
                    double lat = el.GetProperty("lat").GetDouble();
                    double lon = el.GetProperty("lon").GetDouble();
                    cities.Add(new OsmCity { Name = name, Lat = lat, Lon = lon });
                }
            }
            return cities;
        }

        private void DrawRouteByWaypoints(List<Coordinate> waypoints, Mapsui.Styles.Color? color = null)
        {
            if (waypoints == null || waypoints.Count < 2)
                return;

            var lineString = new LineString(waypoints.Select(v => SphericalMercator.FromLonLat(v.X, v.Y).ToCoordinate()).ToArray());

            var lineStyle = MapsuiUtils.GetRoundedLineStyle(
                5,
                color ?? Mapsui.Styles.Color.LawnGreen,
                PenStyle.Solid
            );

            var existingLayer = _map.Layers.FirstOrDefault(l => l.Name == "RouteLayer");
            if (existingLayer != null)
                _map.Layers.Remove(existingLayer);

            var routeLayer = MapsuiUtils.CreateLinestringLayer(lineString, "RouteLayer", [lineStyle]);
            _map.Layers.Add(routeLayer);
            mapView.Map = _map;
        }


        private async Task BuildTourismRouteAsync(
            (double lat, double lon) start,
            (double lat, double lon) end,
            string movementType,
            Dictionary<string, int> tourismTypes)
        {
            double width;
            switch (movementType)
            {
                case "foot": width = 0.025; break;
                case "bike": width = 0.15; break;
                case "car": width = 0.6; break;
                default: width = 0.1; break;
            }

            double dx = end.lon - start.lon;
            double dy = end.lat - start.lat;
            double length = Math.Sqrt(dx * dx + dy * dy);
            if (length == 0) return;

            double perpX = -dy / length;
            double perpY = dx / length;

            double halfWidth = width / 2.0;
            var p1 = (lat: start.lat + perpY * halfWidth, lon: start.lon + perpX * halfWidth);
            var p2 = (lat: start.lat - perpY * halfWidth, lon: start.lon - perpX * halfWidth);
            var p3 = (lat: end.lat - perpY * halfWidth, lon: end.lon - perpX * halfWidth);
            var p4 = (lat: end.lat + perpY * halfWidth, lon: end.lon + perpX * halfWidth);

            double minLat = new[] { p1.lat, p2.lat, p3.lat, p4.lat }.Min();
            double maxLat = new[] { p1.lat, p2.lat, p3.lat, p4.lat }.Max();
            double minLon = new[] { p1.lon, p2.lon, p3.lon, p4.lon }.Min();
            double maxLon = new[] { p1.lon, p2.lon, p3.lon, p4.lon }.Max();

            string bbox = $"{minLat},{minLon},{maxLat},{maxLon}";
            // Добавляем запрос по amenity для food
            string query = $@"
[out:json][timeout:25];
(
  node[""tourism""]({bbox});
  way[""tourism""]({bbox});
  relation[""tourism""]({bbox});
  node[""amenity""~""restaurant|cafe|bar|pub|food_court""]({bbox});
  way[""amenity""~""restaurant|cafe|bar|pub|food_court""]({bbox});
  relation[""amenity""~""restaurant|cafe|bar|pub|food_court""]({bbox});
);
out center;
";

            var places = new List<OsmTourismPlaceModel>();
            using (var client = new HttpClient())
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("data", query)
                });
                var response = await client.PostAsync("https://overpass-api.de/api/interpreter", content);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("elements", out var elements))
                {
                    foreach (var el in elements.EnumerateArray())
                    {
                        string name = null;
                        double lat = 0, lon = 0;
                        string tourismType = null;
                        string amenityType = null;

                        if (el.TryGetProperty("tags", out var tags))
                        {
                            if (tags.TryGetProperty("name", out var nameProp))
                                name = nameProp.GetString();
                            if (tags.TryGetProperty("tourism", out var tourismProp))
                                tourismType = tourismProp.GetString();
                            if (tags.TryGetProperty("amenity", out var amenityProp))
                                amenityType = amenityProp.GetString();
                        }

                        if (el.TryGetProperty("lat", out var latProp) && el.TryGetProperty("lon", out var lonProp))
                        {
                            lat = latProp.GetDouble();
                            lon = lonProp.GetDouble();
                        }
                        else if (el.TryGetProperty("center", out var center))
                        {
                            lat = center.GetProperty("lat").GetDouble();
                            lon = center.GetProperty("lon").GetDouble();
                        }

                        // Для food используем amenityType, для остальных — tourismType
                        if (!string.IsNullOrEmpty(name) && (!string.IsNullOrEmpty(tourismType) || !string.IsNullOrEmpty(amenityType)))
                            places.Add(new OsmTourismPlaceModel
                            {
                                Name = name,
                                Lat = lat,
                                Lon = lon,
                                TourismType = tourismType ?? amenityType // Для food будет amenityType
                            });
                    }
                }
            }

            var allPoints = new List<OsmTourismPlaceModel>();
            var rnd = new Random();
            foreach (var kv in tourismTypes)
            {
                var type = kv.Key;
                var count = kv.Value;
                var points = places
                    .Where(p => p.TourismType?.Equals(type, StringComparison.OrdinalIgnoreCase) == true)
                    .OrderBy(x => rnd.Next())
                    .Take(count)
                    .ToList();
                allPoints.AddRange(points);
            }

            var routePoints = new List<Coordinate>
            {
                new Coordinate(start.lon, start.lat)
            };

            var remaining = new List<OsmTourismPlaceModel>(allPoints);
            var currentLat = start.lat;
            var currentLon = start.lon;

            while (remaining.Count > 0)
            {
                var next = remaining.OrderBy(p => Distance(currentLat, currentLon, p.Lat, p.Lon)).First();
                routePoints.Add(new Coordinate(next.Lon, next.Lat));
                currentLat = next.Lat;
                currentLon = next.Lon;
                AddPin(next.Lat, next.Lon, next.Name, Colors.DarkGreen);
                remaining.Remove(next);
            }


            routePoints.Add(new Coordinate(end.lon, end.lat));
            DrawRouteByWaypoints(routePoints);

            _currentRoute = new RouteInfo
            {
                Name = $"{PointFrom} - {PointTo}",
                Waypoints = routePoints,
                RouteType = RouteType,
                MovementType = MovementType,
                Description = $"Маршрут из {PointFrom} в {PointTo} ({RouteType}, {MovementType})"
            };

            _routes.Add(_currentRoute);

            await SaveCurrentRouteAsync();
        }

        private string GetRouteFilePath()
        {
            var folder = FileSystem.Current.AppDataDirectory;
            return Path.Combine(folder, "routeinfo.json");
        }

        private async Task SaveCurrentRouteAsync()
        {
            if (_currentRoute == null) return;
            var json = JsonSerializer.Serialize(_currentRoute);
            var filePath = GetRouteFilePath();
            await File.WriteAllTextAsync(filePath, json);
        }


        private double DistanceToSegment(double px, double py, double x1, double y1, double x2, double y2)
        {
            double dx = x2 - x1;
            double dy = y2 - y1;
            if (dx == 0 && dy == 0)
                return Distance(px, py, x1, y1);
            double t = ((px - x1) * dx + (py - y1) * dy) / (dx * dx + dy * dy);
            t = Math.Max(0, Math.Min(1, t));
            double projX = x1 + t * dx;
            double projY = y1 + t * dy;
            return Distance(px, py, projX, projY);
        }

        private double Distance(double lat1, double lon1, double lat2, double lon2)
        {
            double dLat = lat1 - lat2;
            double dLon = lon1 - lon2;
            return Math.Sqrt(dLat * dLat + dLon * dLon);
        }

        public async Task<List<OsmTourismPlaceModel>> GetArkhangelskTourismPlacesFromOsmAsync()
        {
            var places = new List<OsmTourismPlaceModel>();
            string overpassUrl = "https://overpass-api.de/api/interpreter";
            string query = """
    [out:json][timeout:25];
    area["ISO3166-2"="RU-ARK"]->.searchArea;
    (
      node["tourism"](area.searchArea);
      way["tourism"](area.searchArea);
      relation["tourism"](area.searchArea);
    );
    out center;
    """;

            using var client = new HttpClient();
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("data", query)
            });
            var response = await client.PostAsync(overpassUrl, content);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("elements", out var elements))
                return places;

            foreach (var el in elements.EnumerateArray())
            {
                string name = null;
                double lat = 0, lon = 0;
                string tourismType = null;

                if (el.TryGetProperty("tags", out var tags))
                {
                    if (tags.TryGetProperty("name", out var nameProp))
                        name = nameProp.GetString();
                    if (tags.TryGetProperty("tourism", out var tourismProp))
                        tourismType = tourismProp.GetString();
                }

                if (el.TryGetProperty("lat", out var latProp) && el.TryGetProperty("lon", out var lonProp))
                {
                    lat = latProp.GetDouble();
                    lon = lonProp.GetDouble();
                }
                else if (el.TryGetProperty("center", out var center))
                {
                    lat = center.GetProperty("lat").GetDouble();
                    lon = center.GetProperty("lon").GetDouble();
                }

                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(tourismType))
                    places.Add(new OsmTourismPlaceModel { Name = name, Lat = lat, Lon = lon, TourismType = tourismType });
            }
            return places;
        }

        private List<OsmTourismPlaceModel> GetPlacesByTypeInArea(string? type, double minLat, double maxLat, double minLon, double maxLon)
        {
            if (TouristicPlaces == null)
                return new List<OsmTourismPlaceModel>();

            return TouristicPlaces
                .Where(p => p.TourismType?
                .Equals(type, StringComparison.OrdinalIgnoreCase) == true
                && p.Lat >= minLat && p.Lat <= maxLat && p.Lon >= minLon && p.Lon <= maxLon)
                .ToList();
        }

        private void AddPin(double lat, double lon, string label, Microsoft.Maui.Graphics.Color color)
        {
            var pin = new Mapsui.UI.Maui.Pin(mapView)
            {
                Label = label,
                Position = new Mapsui.UI.Maui.Position(lat, lon),
                Type = Mapsui.UI.Maui.PinType.Pin,
                Color = color
            };

            mapView.Pins.Add(pin);
        }

        private void SetLocationEntry_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (LocalityTown == null)
                return;
            var query = e.NewTextValue?.ToLower() ?? "";
            var suggestions = LocalityTown
                .Where(t => (t.Name ?? "").ToLower().Contains(query))
                .Take(5)
                .ToList();

            SuggestionsList.ItemsSource = suggestions;
            SuggestionsList.IsVisible = suggestions.Any();

            int visibleCount = suggestions.Count;
            SuggestionsList.HeightRequest = visibleCount * 44;

            double entryHeight = SetLocationEntry.Height > 0 ? SetLocationEntry.Height : 44;
            LocationFrame.HeightRequest = entryHeight + SuggestionsList.HeightRequest;
            if (!suggestions.Any())
            {
                LocationFrame.HeightRequest = SetLocationEntry.Height + 16;
            }
            if (SetLocationEntry.Text == string.Empty || visibleCount == 0)
            {
                LocationFrame.HeightRequest = SetLocationEntry.Height;
            }
        }

        private void SuggestionsList_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem is not OsmCity town)
            {
                SuggestionsList.SelectedItem = null;
                return;
            }

            SetLocationEntry.Text = town.Name;
            SuggestionsList.IsVisible = false;
            SuggestionsList.SelectedItem = null;

            LocationFrame.HeightRequest = SetLocationEntry.Height > 0 ? SetLocationEntry.Height : 60;
            try
            {
                if (_whereFromPosition == null)
                    _whereFromPosition = new Microsoft.Maui.Devices.Sensors.Location();

                _whereFromPosition.Latitude = town.Lat;
                _whereFromPosition.Longitude = town.Lon;
                UpdateCurrentUserLocation(_whereFromPosition.Latitude, _whereFromPosition.Longitude);
                CenterMapOnLocation(_map, town.Lat, town.Lon);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка: {ex.Message}");
            }
        }

        private void CenterMapOnLocation(Mapsui.Map map, double latitude, double longitude)
        {
            var centerOfUser = new MPoint(latitude, longitude);

            var sphericalMercatorCoordinate = SphericalMercator.FromLonLat(centerOfUser.Y, centerOfUser.X).ToMPoint();

            map.Navigator.CenterOnAndZoomTo(sphericalMercatorCoordinate, map.Navigator.Resolutions[15], 750);
        }

        public async Task<Microsoft.Maui.Devices.Sensors.Location?> GetUserLocationAsync()
        {
            try
            {
                var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                    return null;

                var location = await Geolocation.Default.GetLocationAsync();
                return location;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка: {ex.Message}");
                return null;
            }
        }

        private void CenterUserLocation_Clicked(object sender, EventArgs e)
        {
            UpdateCurrentUserLocation(_userPosition.Latitude, _userPosition.Longitude);
            CenterMapOnLocation(_map, _userPosition.Latitude, _userPosition.Longitude);
        }

        private async void UpdateCurrentUserLocation(double lat, double lon)
        {
            var userLocation = new Mapsui.UI.Maui.Position(lat, lon);
            _whereFromPosition.Latitude = userLocation.Latitude;
            _whereFromPosition.Longitude = userLocation.Longitude;
            mapView.MyLocationEnabled = true;
            mapView.MyLocationLayer.UpdateMyLocation(userLocation);
        }

        private void FavouritesViewButton_Clicked(object sender, EventArgs e)
        {
            MainContentView.Content = new Views.FavouritesContentView();

            ProfileViewButton.Background = Colors.Transparent;
            RouteViewButton.Background = Colors.Transparent;
            HistoryViewButton.Background = Colors.Transparent;

            ((Button)sender).Background = new SolidColorBrush(_color);

            ProfileViewButton.TextColor = Colors.DarkGray;
            RouteViewButton.TextColor = Colors.DarkGray;
            HistoryViewButton.TextColor = Colors.DarkGray;
            ((Button)sender).TextColor = Colors.White;
        }

        private void RouteViewButton_Clicked(object sender, EventArgs e)
        {
            MainContentView.Content = _createRoutePage;

            ProfileViewButton.Background = Colors.Transparent;
            HistoryViewButton.Background = Colors.Transparent;
            FavouritesViewButton.Background = Colors.Transparent;

            ((Button)sender).Background = new SolidColorBrush(_color);

            ProfileViewButton.TextColor = Colors.DarkGray;
            FavouritesViewButton.TextColor = Colors.DarkGray;
            HistoryViewButton.TextColor = Colors.DarkGray;
            ((Button)sender).TextColor = Colors.White;
        }

        private void HistoryViewButton_Clicked(object sender, EventArgs e)
        {
            MainContentView.Content = new Views.HistoryContentView();

            ProfileViewButton.Background = Colors.Transparent;
            RouteViewButton.Background = Colors.Transparent;
            FavouritesViewButton.Background = Colors.Transparent;

            ((Button)sender).Background = new SolidColorBrush(_color);

            ProfileViewButton.TextColor = Colors.DarkGray;
            RouteViewButton.TextColor = Colors.DarkGray;
            FavouritesViewButton.TextColor = Colors.DarkGray;
            ((Button)sender).TextColor = Colors.White;
        }

        private void ProfileViewButton_Clicked(object sender, EventArgs e)
        {
            MainContentView.Content = new Views.ProfileContentView();

            HistoryViewButton.Background = Colors.Transparent;
            RouteViewButton.Background = Colors.Transparent;
            FavouritesViewButton.Background = Colors.Transparent;

            ((Button)sender).Background = new SolidColorBrush(_color);

            RouteViewButton.TextColor = Colors.DarkGray;
            FavouritesViewButton.TextColor = Colors.DarkGray;
            HistoryViewButton.TextColor = Colors.DarkGray;
            ((Button)sender).TextColor = Colors.White;
        }

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        private async Task LoadCurrentRouteAsync()
        {
            var filePath = GetRouteFilePath();
            if (File.Exists(filePath))
            {
                var json = await File.ReadAllTextAsync(filePath);
                _currentRoute = JsonSerializer.Deserialize<RouteInfo>(json);
                if (_currentRoute != null)
                    _routes.Add(_currentRoute);
            }
        }
    }
}