using Mapsui;
using Mapsui.Extensions;
using Mapsui.Projections;
using Mapsui.Tiling;
using Mapsui.UI.Maui;

namespace Trevoria
{
    public partial class MainPage : ContentPage
    {
        private Mapsui.Map _map;
        private Location _userPosition;

        public MainPage()
        {
            InitializeComponent();
            LoadMap();
        }

        private async Task LoadMap()
        {
            _userPosition = await GetUserLocation();
            _map = new Mapsui.Map();

            var osmLayer = OpenStreetMap.CreateTileLayer();

            UpdateCurrentUserLocation();

            _map.Layers.Add(osmLayer);

            CenterMapOnLocation(_map, _userPosition.Latitude, _userPosition.Longitude);
            mapView.Map = _map;
        }

        private void CenterMapOnLocation(Mapsui.Map map, double latitude, double longitude)
        {
            var centerOfUser = new MPoint(latitude, longitude);

            var sphericalMercatorCoordinate = SphericalMercator.FromLonLat(centerOfUser.Y, centerOfUser.X).ToMPoint();

            map.Navigator.CenterOnAndZoomTo(sphericalMercatorCoordinate, map.Navigator.Resolutions[15], 750);
        }

        public async Task<Location?> GetUserLocation()
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

        private void ChangeDirectionButton_Clicked(object sender, EventArgs e)
        {
            var temp = LocationFromEntry.Text;
            LocationFromEntry.Text = LocationToEntry.Text;
            LocationToEntry.Text = temp;
        }
    }
}
