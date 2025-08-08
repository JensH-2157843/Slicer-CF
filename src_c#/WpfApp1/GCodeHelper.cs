using Clipper2Lib;

namespace WpfApp1;

public class GCodeHelper
{
    /**
     * Calculates the extrusion amount required for moving between two points.
     * TODO: Check if this is the correct extrusion
     */
    public static double CalculateExtrusion(PointD p1, PointD p2, SlicerSettings slicerSettings, bool isPerimeter = false)
    {
        // Fetch slicer settings
        decimal nozzleSize = slicerSettings.NozzleDiameter;
        if (isPerimeter)
        {
            nozzleSize /= 2;
        }
        decimal layerHeight = slicerSettings.LayerHeight;
        double filamentArea = slicerSettings.FilamentArea;

        // Distance between the two points
        double distance = Math.Sqrt(Math.Pow(p2.x - p1.x, 2) + Math.Pow(p2.y - p1.y, 2));

        // Calculate extrusion amount based on the volume of extruded material
        double extrusionAmount = (distance * decimal.ToDouble(nozzleSize * layerHeight)) / filamentArea;

        return extrusionAmount;
    }
    
    /**
    * Calculates the Euclidean distance between two points to compare
    */
    private static double CalculateDistance(PointD p1, PointD p2)
    {
        return Math.Sqrt(Math.Pow(p2.x - p1.x, 2) + Math.Pow(p2.y - p1.y, 2));
    }
    
    /**
    * Use epsilon value to check distances, there might be rounding errors so
    * points might not exactly match
    */
    public static bool CheckPointsDistance(PointD p1, PointD p2)
    {
        var epsilon = 1e-6;
        return Math.Abs(p1.x - p2.x) < epsilon && Math.Abs(p1.y - p2.y) < epsilon;
    }
    

    public static Tuple<double, double> GetXAndYOffsetsToCenter(PathsD slice, SlicerSettings settings)
    {
        // Calculate the bounding box of the model
        
        double modelMinX = slice.Min(path => path.Min(point => point.x));
        double modelMaxX = slice.Max(path => path.Max(point => point.x));
        double modelMinY = slice.Min(path => path.Min(point => point.y));
        double modelMaxY = slice.Max(path => path.Max(point => point.y));
    
        double modelWidth = modelMaxX - modelMinX;
        double modelHeight = modelMaxY - modelMinY;

        // Calculate offsets for centering
        double xOffset = (decimal.ToDouble(settings.BedWidth / 2)) - (modelWidth / 2) - modelMinX;
        double yOffset = (decimal.ToDouble(settings.BedDepth / 2)) - (modelHeight / 2) - modelMinY;

        return new Tuple<double, double>(xOffset, yOffset);
    }
    
    
    
    /**
 * Optimizes the order of points in each path and the order of paths in the slice.
 */
    public static PathsD OptimizePaths(PathsD paths)
    {
        if (paths.Count == 0) return paths;

        PathsD optimizedPaths = new PathsD();
        PointD currentPosition = paths[0][0]; // Start from the first point of the first path

        while (paths.Count > 0)
        {
            // Find the nearest path to the current position
            int nearestPathIndex = FindNearestPath(currentPosition, paths);
            PathD nearestPath = paths[nearestPathIndex];

            // Optimize point order within the selected path
            PathD orderedPath = OrderPointsInPath(nearestPath, currentPosition);

            // Add the optimized path to the result
            optimizedPaths.Add(orderedPath);

            // Update the current position to the last point of the ordered path
            currentPosition = orderedPath[^1];

            // Remove the processed path from the input
            paths.RemoveAt(nearestPathIndex);
        }

        return optimizedPaths;
    }
    
        /**
     * Finds the index of the path nearest to the current position.
     */
private static int FindNearestPath(PointD currentPosition, PathsD paths)
{
    double minDistance = double.MaxValue;
    int nearestIndex = 0;

    for (int i = 0; i < paths.Count; i++)
    {
        double distance = CalculateDistance(currentPosition, paths[i][0]); // Distance to the first point of the path
        if (distance < minDistance)
        {
            minDistance = distance;
            nearestIndex = i;
        }
    }

    return nearestIndex;
}


/**
 * Orders the points in a path to start from the nearest point to the current position.
 */
private static PathD OrderPointsInPath(PathD path, PointD currentPosition)
{
    if (path.Count < 2) return path; // No reordering needed for trivial paths

    // Check if reversing the path reduces the distance from the current position
    double distanceNormal = CalculateDistance(currentPosition, path[0]);
    double distanceReversed = CalculateDistance(currentPosition, path[^1]);

    if (distanceReversed < distanceNormal)
    {
        path.Reverse();
    }

    return path;
}
    
    
    /**
     * Converts raw paths into valid printing paths.
     */
    public static List<PathsD> GetValidPaths(PathsD paths)
    {
        List<PathsD> validPaths = new List<PathsD>();

        while (paths.Count > 0)
        {
            var nextPath = paths[0];
            PathsD validPath = CreateValidPath(nextPath, paths);

            // Add only valid paths with enough points
            if (validPath.Count >= 2)
            {
                validPaths.Add(validPath);
            }
        }

        return validPaths;
    }
    
    /**
    * Creates a valid path from the provided starting path.
    */
    public static PathsD CreateValidPath(PathD startPath, PathsD allPaths)
    {
        PathsD resultPaths = new PathsD { startPath };
        PointD previousPoint = startPath[0];
        PathD currentPath = startPath;

        while (true)
        {
            // Find the next connected path
            PathD? nextPath = LookAtNextIntersection(previousPoint, currentPath, allPaths);

            if (nextPath == null || nextPath == startPath)
            {
                break; // No more connected paths or returned to start
            }

            resultPaths.Add(nextPath);
            previousPoint = FindCommonPoint(currentPath, nextPath);
            currentPath = nextPath;
        }

        // Remove used paths from the original list
        foreach (var path in resultPaths)
        {
            allPaths.Remove(path);
        }

        return resultPaths;
    }
    
    /**
    * Finds the next path connected to the current path.
    */
    public static PathD? LookAtNextIntersection(PointD previousPoint, PathD currentPath, PathsD allPaths)
    {
        foreach (var path in allPaths)
        {
            if (path == currentPath)
            {
                continue;
            }

            if (PathSharesPoint(path, currentPath, previousPoint))
            {
                return path;
            }
        }

        return null;
    }

    /**
    * Checks if two paths share a common point.
    */
    public static bool PathSharesPoint(PathD path1, PathD path2, PointD excludePoint)
    {
        foreach (var point in path1)
        {
            if (path2.Contains(point) && point != excludePoint)
            {
                return true;
            }
        }
        return false;
    }

    /**
     * Finds the common point between two paths.
     */
    public static PointD FindCommonPoint(PathD path1, PathD path2)
    {
        return path1.FirstOrDefault(point => path2.Contains(point));
    }


}