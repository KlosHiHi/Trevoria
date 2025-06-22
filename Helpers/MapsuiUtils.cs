using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Nts.Extensions;
using Mapsui.Projections;
using Mapsui.Styles;
using NetTopologySuite.Geometries;

namespace Trevoria.Helpers;

public static class MapsuiUtils
{
    public static LineString CreateLineString(IEnumerable<Coordinate> coordinates)
    {
        return new LineString(coordinates.Select(v => SphericalMercator.FromLonLat(v.X, v.Y).ToCoordinate()).ToArray());
    }

    public static MemoryLayer CreateLinestringLayer(LineString lineString, string layerName = "Line String", ICollection<IStyle> styles = null)
    {
        return new MemoryLayer
        {
            Features = [
                new GeometryFeature {
                    Geometry = lineString,
                    Styles = styles
                }],
            Name = layerName
        };
    }

    public static IStyle GetRoundedLineStyle(double width, Mapsui.Styles.Color color, PenStyle penStyle = PenStyle.Solid, float opacity = 1, double minVisible = 0, double maxVisible = double.MaxValue)
    {
        return new VectorStyle
        {
            Line = new Pen
            {
                Color = color,
                PenStrokeCap = PenStrokeCap.Round,
                StrokeJoin = StrokeJoin.Round,
                PenStyle = penStyle,

                Width = width
            },
            MinVisible = minVisible,
            MaxVisible = maxVisible,
            Opacity = opacity
        };
    }
    public static IStyle GetSquaredLineStyle(double width, Mapsui.Styles.Color color, PenStyle penStyle = PenStyle.Solid, float opacity = 1, double minVisible = 0, double maxVisible = double.MaxValue)
    {
        return new VectorStyle
        {
            Line = new Pen
            {
                Color = color,
                PenStrokeCap = PenStrokeCap.Square,
                StrokeJoin = StrokeJoin.Miter,
                PenStyle = penStyle,
                Width = width
            },
            MinVisible = minVisible,
            MaxVisible = maxVisible,
            Opacity = opacity
        };
    }
}