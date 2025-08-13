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
    
    public void UpdateInfill(SlicerSettings slicerSettings)
    {
        _settings = slicerSettings;
    }
    
    private PathsD maxShell(Dictionary<string, PathsD> paths)
    {
        int maxShell = 0;

        while (paths.ContainsKey($"SHELL{maxShell}"))
        {
            maxShell++;
        }
        --maxShell;
        
        if (maxShell == -1)
        {
            return paths["PERIMETER"];
        }
        
        return paths[$"SHELL{maxShell}"];
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
        double procent = _settings.Infill_Proc / 100.0;
        double spacing = Decimal.ToDouble(_settings.NozzleDiameter) / procent;

        PathsD Grid = new PathsD();
        
        bool turn = true;

        for (var x = min.x; x <= max.x; x += spacing)
        {
            PathD line = new PathD();
            if (turn)
            {
                line.Add(new PointD(x, min.y));
                line.Add(new PointD(x, max.y));
            }
            else
            {
                line.Add(new PointD(x, max.y));
                line.Add(new PointD(x, min.y));
            }
            turn = !turn;
            Grid.Add(line);
        }

        for (var y = min.y; y <= max.y; y += spacing)
        {
            PathD line = new PathD();
            if (turn)
            {
                line.Add(new PointD(min.x, y));
                line.Add(new PointD(max.x, y));
            }
            else
            {
                line.Add(new PointD(max.x, y));
                line.Add(new PointD(min.x, y));
            }
            turn = !turn;
            Grid.Add(line);
        }

        var c = new ClipperD();
        
        foreach (var path in paths)
        {
            var value = path.Value;
            var innerPath = maxShell(value);
            
            if (value.ContainsKey("FLOOR"))
            {
                var result = new PathsD();
                c.AddPaths(value["FLOOR_INFILL"], PathType.Clip);
                c.AddPaths(innerPath, PathType.Subject);
                c.Execute(ClipType.Difference,FillRule.NonZero,result);
                innerPath = result;
                c.Clear();
            }
            if (value.ContainsKey("ROOF"))
            {
                var result = new PathsD();
                c.AddPaths(value["ROOF_INFILL"], PathType.Clip);
                c.AddPaths(innerPath, PathType.Subject);
                c.Execute(ClipType.Difference,FillRule.NonZero,result);
                innerPath = result;
                c.Clear();
            }

            if (innerPath.Count > 0)
            {
                var result = new PathsD();
                c.AddPaths(innerPath, PathType.Clip);
                c.AddPaths(Grid, PathType.Subject, true);
                c.Execute(ClipType.Intersection,FillRule.NonZero,result, result);
                path.Value["INFILL"] = result;
                c.Clear();
            }
        }
        
        

        return paths;
    }
}