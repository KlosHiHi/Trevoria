using NetTopologySuite.Geometries;

namespace Trevoria.Model
{
    public class RouteInfo
    {
        public string Name { get; set; }

        public List<Coordinate> Waypoints { get; set; } = new();

        public string RouteType { get; set; }

        public string MovementType { get; set; }

        public string Description { get; set; }
    }
}
