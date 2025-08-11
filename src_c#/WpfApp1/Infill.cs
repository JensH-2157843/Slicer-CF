using System.Windows;

namespace WpfApp1;
using Clipper2Lib;

public class Infill
{
    private SlicerSettings? _settings;

    public Infill(SlicerSettings settings)
    {
        _settings = settings;
    }

    public static (PointD, PointD) getMinMaxpointFromPath(PathsD paths)
    {
        var min = new PointD(Double.PositiveInfinity, Double.PositiveInfinity);
        var max = new PointD(Double.NegativeInfinity, Double.NegativeInfinity);

        foreach (var path in paths)
        {
            foreach (var point in path)
            {
                if (point.x >= max.x)
                {
                    max.x = point.x;
                }

                if (point.y >= max.y)
                {
                    max.y = point.y;
                }
                if (point.x <= min.x)
                {
                    min.x = point.x;
                }

                if (point.y <= min.y)
                {
                    min.y = point.y;
                }
            }
        }
        return (min, max);
    }

    public static (PointD, PointD) getMinMaxpointFromPaths(Dictionary<int, Dictionary<string, PathsD>> path)
    {
        var min = new PointD(Double.PositiveInfinity, Double.PositiveInfinity);
        var max = new PointD(Double.NegativeInfinity, Double.NegativeInfinity);

        foreach (var paths in path)
        {
            var (localMin, localMax) = getMinMaxpointFromPath(paths.Value["PERIMETER"]);

            if (localMin.x <= min.x && localMin.y <= min.y)
            {
                min = localMin;
            }

            if (localMax.x >= max.x && localMax.y >= max.y)
            {
                max = localMax;
            }
        }

        return (min, max);
    }
    
    public Dictionary<int, Dictionary<string, PathsD>> create_Infill(
        Dictionary<int, Dictionary<string, PathsD>> paths)
    {
        // var infill_procent = _settings.
        var (min, max) = getMinMaxpointFromPaths(paths);
        
        

        return paths;
    }
}