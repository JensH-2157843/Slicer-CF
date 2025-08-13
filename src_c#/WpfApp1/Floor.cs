namespace WpfApp1;

using Clipper2Lib;

public class Floor
{
    private SlicerSettings _slicerSettings;

    public Floor(SlicerSettings slicerSettings)
    {
        _slicerSettings = slicerSettings;
    }

    public PathsD generateFloor(PathsD innerShell, bool XUpDown)
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
        if (currentKey < 2)
        {
            return current;
        }

        PathsD joined_paths = new PathsD(maxShell(all_paths[currentKey - 1]));
        for (var i = currentKey - 2; i >= currentKey - 2; i--)
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


    public Dictionary<int, Dictionary<string, PathsD>> createFloor(Dictionary<int, Dictionary<string, PathsD>> paths)
    {
        bool LeftToRight = false;
        Dictionary<int, PathsD> floors = new Dictionary<int, PathsD>();
        Dictionary<int, PathsD> floorInfill = new Dictionary<int, PathsD>();
        
        foreach (var path in paths)
        {
            var key = path.Key;
            var pathsC = path.Value;
            var innerShell = maxShell(pathsC);
            var p = isEligible(innerShell, key, paths);
            if (p.Count > 0 )
            {
                floors[key] = generateFloor(p, LeftToRight);
                floorInfill[key] = p;
                LeftToRight = !LeftToRight;
            }
        }

        foreach (var floor in floors)
        {
            var key = floor.Key;
            paths[key]["FLOOR"] = floor.Value;
            paths[key]["FLOOR_INFILL"] = floorInfill[key];
        }

        return paths; 
    }
}