using Mapsui;
using Mapsui.Extensions;
using Mapsui.Projections;
using Mapsui.Tiling;
using Mapsui.UI.Maui;

namespace Trevoria
{
    public partial class MainPage : ContentPage
    {

        public MainPage()
        {
            InitializeComponent();
            LoadMap();
        }

        private async Task LoadMap()
        {
            var userLocation = await GetUserLocation();
            var map = new Mapsui.Map();

            var osmLayer = OpenStreetMap.CreateTileLayer();
            map.Layers.Add(osmLayer);
            mapControl.Map = map;

            CenterMapOnLocation(map, userLocation.Latitude, userLocation.Longitude);

        }

        private void CenterMapOnLocation(Mapsui.Map map, double latitude, double longitude)
        {
            var centerOfUser = new MPoint(latitude, longitude);

            var sphericalMercatorCoordinate = SphericalMercator.FromLonLat(centerOfUser.Y, centerOfUser.X).ToMPoint();

            map.Navigator.CenterOnAndZoomTo(sphericalMercatorCoordinate, map.Navigator.Resolutions[15]);
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
    }
}
