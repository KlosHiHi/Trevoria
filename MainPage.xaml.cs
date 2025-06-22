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
using Trevoria.Helpers;
using Trevoria.Model;

namespace Trevoria
{
    public partial class MainPage : ContentPage, INotifyPropertyChanged
    {
        private Mapsui.Map _map;

        private Views.RouteContentView _createRoutePage = new();

        private Microsoft.Maui.Devices.Sensors.Location _userPosition;

        private Coordinate _whereFromPosition;
        private Coordinate _whereToPosition;

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

            //var query = PointTo?.ToLower() ?? "";
            //var pointTo = LocalityTown.Find(t => t.name.ToLower().Contains(query));
            //_whereToPosition.X = pointTo.lat;
            //_whereToPosition.Y = pointTo.lon;
        }

        private async void InitAsync()
        {
            await LoadMap();
            LocalityTown = await GetArkhangelskCitiesFromOsmAsync();
            TouristicPlaces = await GetArkhangelskTourismPlacesFromOsmAsync();
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

        private void CreateLineRoute()
        {
            switch (RouteType)
            {
                case "empty":

                    _lineCoordinates = new()
                    {
                        new Coordinate(_whereFromPosition.X, _whereFromPosition.Y),
                        new Coordinate(_whereToPosition.X, _whereToPosition.Y)
                    };

                    DrawRouteByWaypoints(_lineCoordinates);

                    break;
                case "nature":

                    _lineCoordinates = new()
                    {
                       new Coordinate(37.6173, 56.7558),
                        new Coordinate(49.7015, 47.2357),
                        new Coordinate(30.3141, 29.9386)
                    };
                    DrawRouteByWaypoints(_lineCoordinates);

                    break;
                case "art":

                    _lineCoordinates = new()
                    {
                        new Coordinate(27.6173, 55.7558),
                        new Coordinate(39.7015, 67.2357),
                        new Coordinate(30.3141, 19.9386)
                    };

                    DrawRouteByWaypoints(_lineCoordinates);

                    break;
                case "food":
                    _lineCoordinates = new()
                    {
                        new Coordinate(37.6173, 56.7558),
                        new Coordinate(9.7015, 47.2357),
                        new Coordinate(38.3141, 59.9386)
                    };

                    DrawRouteByWaypoints(_lineCoordinates);

                    break;
                case "history":

                    _lineCoordinates = new()
                    {
                        new Coordinate(3.6173, 55.7558),
                        new Coordinate(39.7015, 7.2357),
                        new Coordinate(3.3141, 60.9386)
                    };
                    DrawRouteByWaypoints(_lineCoordinates);

                    break;
                default:
                    break;
            }
        }

        private void AddPin(double latitude, double longitude, string name, Microsoft.Maui.Graphics.Color color)
        {
            var pin = new Pin(new MapView())
            {
                Position = new Mapsui.UI.Maui.Position(latitude, longitude),
                Type = PinType.Pin,
                Label = name,
                Color = color,
                Scale = 0.5f
            };

            mapView.Pins.Add(pin);

            try
            {
                _mapPinsDictionary.Add(name, pin);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Load error: {ex.Message}");
            }
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
                    _whereFromPosition = new Coordinate();

                _whereFromPosition.X = town.Lat;
                _whereFromPosition.Y = town.Lon;
                UpdateCurrentUserLocation(_whereFromPosition.X, _whereFromPosition.Y);
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
            mapView.MyLocationEnabled = true;
            mapView.MyLocationLayer.UpdateMyLocation(userLocation);
        }

        private void FavouritesViewButton_Clicked(object sender, EventArgs e)
        {
            MainContentView.Content = new Views.FavouritesContentView();

            ProfileViewButton.Background = Colors.Transparent;
            RouteViewButton.Background = Colors.Transparent;
            HistoryViewButton.Background = Colors.Transparent;
            ((Button)sender).Background = Colors.LawnGreen;

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
            ((Button)sender).Background = Colors.LawnGreen;

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
            ((Button)sender).Background = Colors.LawnGreen;

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
            ((Button)sender).Background = Colors.LawnGreen;

            RouteViewButton.TextColor = Colors.DarkGray;
            FavouritesViewButton.TextColor = Colors.DarkGray;
            HistoryViewButton.TextColor = Colors.DarkGray;
            ((Button)sender).TextColor = Colors.White;
        }
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
}