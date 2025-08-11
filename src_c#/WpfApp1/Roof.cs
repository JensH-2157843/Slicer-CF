namespace WpfApp1;

using Clipper2Lib;

public class Roof
{
    private SlicerSettings _slicerSettings;

    public Roof(SlicerSettings slicerSettings)
    {
        _slicerSettings = slicerSettings;
    }
    
    private PathsD generateRoof(PathsD innerShell, bool XUpDown)
    {
        PathsD floor = new PathsD();
        var (min, max) = Infill.getMinMaxpointFromPath(innerShell);
        
        
        if (XUpDown)
        { 
            bool xFirst = true;
            for (var y = min.y; y <= max.y; y += Decimal.ToDouble(_slicerSettings.NozzleDiameter))
            {
                PathD temp = new PathD();

                if (xFirst)
                {
                    temp.Add(new PointD(min.x, y));
                    temp.Add(new PointD(max.x, y));
                }
                else
                {
                    temp.Add(new PointD(max.x, y));
                    temp.Add(new PointD(min.x, y));
                }
                floor.Add(temp);
                xFirst = !xFirst;
            }
        }
        else
        {
            bool xFirst = true;
            for (var x = min.x; x <= max.x; x += Decimal.ToDouble(_slicerSettings.NozzleDiameter))
            {
                PathD temp = new PathD();

                if (xFirst)
                {
                    temp.Add(new PointD(x, min.y));
                    temp.Add(new PointD(x, max.y));
                }
                else
                {
                    temp.Add(new PointD(x, max.y));
                    temp.Add(new PointD(x, min.y));
                }
                floor.Add(temp);
                xFirst = !xFirst;
            }
        }
        ClipperD c = new ClipperD();
        c.AddOpenSubject(floor);
        c.AddClip(innerShell);
        var t = new PathsD();
        c.Execute(ClipType.Intersection, FillRule.NonZero, t, floor);
        return floor;
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

    private PathsD isEligible(PathsD current, int currentKey, Dictionary<int, Dictionary<string, PathsD>> all_paths)
    {
        if (currentKey >= all_paths.Keys.Count - 2)
            return current;

        PathsD joined_paths = new PathsD(maxShell(all_paths[currentKey + 1]));
        for (var i = currentKey + 2; i <= currentKey + 2; i++)
        {
            var c = new ClipperD(); // fresh instance for each operation
            c.AddPaths(joined_paths, PathType.Subject);
            c.AddPaths(maxShell(all_paths[i]), PathType.Clip);

            PathsD result = new PathsD();
            c.Execute(ClipType.Intersection, FillRule.NonZero, result);
            joined_paths = result;
        }
        
        {
            var c = new ClipperD();
            c.AddPaths(joined_paths, PathType.Clip);
            c.AddPaths(current, PathType.Subject);

            PathsD result = new PathsD();
            c.Execute(ClipType.Difference, FillRule.NonZero, result);
            joined_paths = result;
        }

        return joined_paths;
    }


    public Dictionary<int, Dictionary<string, PathsD>> createRoof(Dictionary<int, Dictionary<string, PathsD>> paths)
    {
        bool LeftToRight = false;
        Dictionary<int, PathsD> roofs = new Dictionary<int, PathsD>();

        foreach (var path in paths)
        {
            var key = path.Key;
            var pathsC = path.Value;
            var innerShell = maxShell(pathsC);
            var p = isEligible(innerShell, key, paths);
            if (p.Count > 0 )
            {
                roofs[key] = generateRoof(p, LeftToRight);
                LeftToRight = !LeftToRight;
            }
        }

        foreach (var roof in roofs)
        {
            var key = roof.Key;
            paths[key]["ROOF"] = roof.Value;
        }

        return paths; 
    }
}