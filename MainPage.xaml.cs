using Mapsui;
using Mapsui.Extensions;
using Mapsui.Nts.Extensions;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.UI.Maui;
using NetTopologySuite.Geometries;
using System.Diagnostics;
using System.Text.Json;
using Trevoria.Helpers;
using Trevoria.Model;

namespace Trevoria
{
    public partial class MainPage : ContentPage
    {
        private Mapsui.Map _map;

        private Microsoft.Maui.Devices.Sensors.Location _userPosition;

        public List<TouristicPlacesModel> Places { get; set; }

        private Dictionary<string, Pin> _pins = new();

        public MainPage()
        {
            InitializeComponent();
            LoadMap();
        }

        private async Task LoadMap()
        {
            _userPosition = await GetUserLocation();


            _map = new Mapsui.Map();
            _map.Layers.Add(OpenStreetMap.CreateTileLayer());

            UpdateCurrentUserLocation();

            CenterMapOnLocation(_map, _userPosition.Latitude, _userPosition.Longitude);

            LoadJsonData();

            foreach (var item in Places)
            {
                if (item.type == "museum")
                {
                    AddPin(item.lat, item.lon, item.name, Colors.Black);
                }
                if (item.type == "park")
                {
                    AddPin(item.lat, item.lon, item.name, Colors.Green);
                }
            }
            List<Coordinate> coordinates = new()
            {
                new Coordinate(_userPosition.Longitude, _userPosition.Latitude),
                new Coordinate(_pins["Музей Ломоносова"].Position.Longitude,  _pins["Музей Ломоносова"].Position.Latitude),
                new Coordinate(_pins["Черевковский краеведческий музей"].Position.Longitude,  _pins["Черевковский краеведческий музей"].Position.Latitude),
                new Coordinate(_pins["Архангельский областной краеведческий музей"].Position.Longitude,  _pins["Архангельский областной краеведческий музей"].Position.Latitude),

            };

            DrawLine(coordinates);

            

            mapView.Map = _map;

        }

        private void DrawLine(IEnumerable<Coordinate> coordinates)
        {
            LineString line = MapsuiUtils.CreateLineString(coordinates);


            ICollection<IStyle> styles = [
            MapsuiUtils.GetRoundedLineStyle(5, Mapsui.Styles.Color.Green, PenStyle.Solid),
        ];

            _map.Layers.Add(MapsuiUtils.CreateLinestringLayer(line, "Line Layer", styles));
        }

        public async void LoadJsonData()
        {
            try
            {
                using var stream = FileSystem.OpenAppPackageFileAsync("arkhangelsk_places.json").Result;
                using var reader = new StreamReader(stream);
                var json = reader.ReadToEnd();
                Places = JsonSerializer.Deserialize<List<TouristicPlacesModel>>(json) ?? new List<TouristicPlacesModel>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Load error: {ex.Message}");
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
                _pins.Add(name, pin);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Load error: {ex.Message}");
            }
        }

        private void DeletePin(string name)
        {
            try
            {
                mapView.Pins.Remove(_pins[name]);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Load error: {ex.Message}");
            }
        }

        private void CenterMapOnLocation(Mapsui.Map map, double latitude, double longitude)
        {
            var centerOfUser = new MPoint(latitude, longitude);

            var sphericalMercatorCoordinate = SphericalMercator.FromLonLat(centerOfUser.Y, centerOfUser.X).ToMPoint();

            map.Navigator.CenterOnAndZoomTo(sphericalMercatorCoordinate, map.Navigator.Resolutions[15], 750);
        }

        public async Task<Microsoft.Maui.Devices.Sensors.Location?> GetUserLocation()
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
                Console.WriteLine($"Ошибка: {ex.Message}");
                return null;
            }
        }

        private void CenterUserLocation_Clicked(object sender, EventArgs e)
        {
            UpdateCurrentUserLocation();
            CenterMapOnLocation(_map, _userPosition.Latitude, _userPosition.Longitude);
        }

        private void UpdateCurrentUserLocation()
        {
            var userLocation = new Mapsui.UI.Maui.Position(_userPosition.Latitude, _userPosition.Longitude);
            mapView.MyLocationEnabled = true;
            mapView.MyLocationLayer.UpdateMyLocation(userLocation);
        }

        private void FavouritesViewButton_Clicked(object sender, EventArgs e)
        {
            MainContentView.Content = new Views.FavouritesContentView();
        }

        private void RouteViewButton_Clicked(object sender, EventArgs e)
        {
            MainContentView.Content = new Views.RouteContentView();
        }

        private void HistoryViewButton_Clicked(object sender, EventArgs e)
        {
            MainContentView.Content = new Views.HistoryContentView();
        }

        private void ProfileViewButton_Clicked(object sender, EventArgs e)
        {
            MainContentView.Content = new Views.ProfileContentView();
        }
    }
}