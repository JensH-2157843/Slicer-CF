using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using Clipper2Lib;

namespace WpfApp1;
public class Slicer
{
    // private ModelVisual3D stlModel;
    // private GeometryModel3D geometryModel;
    private const decimal tolerance = 0.0000001m;
    private SlicerSettings settings;
    
    /*private PathD clip = new PathD
    {
        new PointD(-110, -110),
        new PointD(110, -110),
        new PointD(110, 110),
        new PointD(-110, 110),
        new PointD(-110, -110)
    };
    */

    public Slicer(SlicerSettings settings)
    {
        this.settings = settings;
    }

    public SlicerSettings Settings()
    {
        return this.settings;
    }

    public void UpdateSlicerSettings(SlicerSettings updatedSettings)
    {
        this.settings = updatedSettings;
        Console.WriteLine("Slicer settings updated in SLICER.");
        /*Console.WriteLine("Layerheight: " + updatedSettings.LayerHeight);
        Console.WriteLine("NozzleTemperature: " + updatedSettings.NozzleTemperature);
        Console.WriteLine("BedTemperature: " + updatedSettings.BedTemperature);
        Console.WriteLine("FilamentDiameter: " + updatedSettings.FilamentDiameter);
        Console.WriteLine("NozzleDiameter: " + updatedSettings.NozzleDiameter);*/
    }

    public PathsD? SliceAndGeneratePaths(decimal height, ModelVisual3D model, decimal scale)
    {
        // Apply transformations to content
        GeometryModel3D geometryModel = model.Content as GeometryModel3D;
        if (geometryModel == null)
        {
            MessageBox.Show("Cannot access GeometryModel3D from model content.");
            return null;
        }
        
        //var stlTransform = model.Transform;
        var scaleTransform = new ScaleTransform3D(decimal.ToDouble(scale), decimal.ToDouble(scale), decimal.ToDouble(scale));
        
        var intersections = SliceModelAtHeight(height, geometryModel, scaleTransform);
        if (intersections.Count > 0)
        {
            var paths = ProcessSegmentsWithClipper(intersections);
            return paths;
        }
        else
        {
            return null;
        }
    }
    
    private List<Point3D> TransformMeshPositions(MeshGeometry3D mesh, Transform3D transform)
    {
        return mesh.Positions.Select(point => transform.Transform(point)).ToList();
    }
    
    /**
     * Slice model using direct Plane-Triangle intersections and
     * connecting calculated intersection lines
     */
    private PathsD SliceModelAtHeight(decimal height, GeometryModel3D geometryModel, Transform3D transform)
    {
        // Create list of PathD paths (list of point pairs that indicate line segments)
        PathsD lineSegments = new PathsD();
        
        MeshGeometry3D mesh = geometryModel.Geometry as MeshGeometry3D;
        if (mesh == null)
        {
            MessageBox.Show("Invalid Geometry");
            return new PathsD();
        }

        // Apply the transformation to the positions
        var transformedPositions = TransformMeshPositions(mesh, transform);
        
        for (int i = 0; i < mesh.TriangleIndices.Count; i += 3)
        {
            Int32Collection indices = mesh.TriangleIndices;
            
            // Get the current triangle's points
            Point3D v1 = transformedPositions[indices[i]];
            Point3D v2 = transformedPositions[indices[i + 1]];
            Point3D v3 = transformedPositions[indices[i + 2]];
            
            // Slice the triangle and get the intersection points / line segments
            // with the slicing plane
            // Creates a list of line segments from the plane-triangle intersections
            var intersections = SliceTriangle(v1, v2, v3, decimal.ToDouble(height));
            lineSegments.AddRange(intersections);
            
            /*// Create polygons from the intersection line segments using the “Even-Odd” for the FillRule parameter
            // to ensure contours are correctly identified as solids or holes
            var solution = new PathsD();
            ClipperD clipper = new ClipperD();
            clipper.AddPaths(intersections, PathType.Subject, false);
            clipper.AddPath(clip, PathType.Clip, true);
            clipper.Execute(ClipType.Union, Clipper2Lib.FillRule.EvenOdd, solution);
            
            // Console.WriteLine(solution.Count);
                
            if (solution.Count() != 0)
            {
                lineSegments.Add(solution[0]);
                // Console.WriteLine($"Intersection: {slice.Value.Item1} -> {slice.Value.Item2}");
            }*/
        }
        
        return lineSegments;
    }


    public Dictionary<int, Dictionary<string, PathsD>> SliceModel(ModelVisual3D model, decimal scale)
    {
        var slicesDictionary = new Dictionary<int, Dictionary<string, PathsD>>();
        
        // Loop through the model layer by layer height and slice each layer
        decimal slicingHeight = 0.0m;
        
        // TODO: Are scale transformations applied here???
        
        double modelHeight = model.Content.Bounds.SizeZ * decimal.ToDouble(scale);
        int layer = 0;
        
        Console.WriteLine("in");

        while (decimal.ToDouble(slicingHeight + tolerance) < modelHeight)
        {
            PathsD? slice = SliceAndGeneratePaths(slicingHeight, model, scale);
            Dictionary<String, PathsD> New_layer = new Dictionary<string, PathsD>();
            if (slice == null)
            {
                Console.WriteLine("Warning! Empty slice.");
            }
            else
            {
                var Oslice = GCodeConverter.OrderPaths(slice);
                New_layer.Add("PERIMETER", Oslice);
                for (int i = 0; i < settings.NumberShells; ++i)
                {
                    PathsD p = Clipper.InflatePaths(Oslice,-0.3 - 0.4 * i,JoinType.Square,EndType.Polygon);
                    //p = Clipper.SimplifyPaths(p, 0.025);
                    if (p.Count() == 0)
                    {
                        Console.WriteLine("Warning! Empty slice.");
                        break;
                    }
                    foreach (PathD path in p)
                    {
                        path.Add(path[0]);
                    }
                    New_layer.Add($"SHELL{i}", p);
                }
            }
            slicesDictionary.Add(layer, New_layer);
            
            // Increase slicing height by layer height
            slicingHeight += settings.LayerHeight;
            layer++;
        }

        var floor = new Floor(settings);
        slicesDictionary = floor.createFloor(slicesDictionary);

        var roof = new Roof(settings);
        slicesDictionary = roof.createRoof(slicesDictionary);

        var infill = new Infill(settings);
        slicesDictionary = infill.create_Infill(slicesDictionary);
       
            
        
        return slicesDictionary;
    }
    
    
    /**
     * Slice triangle using Direct Plane-Triangle Intersection
     * given the slicing plane height: slicingPlaneHeight
     */
    [SuppressMessage("ReSharper.DPA", "DPA0002: Excessive memory allocations in SOH", MessageId = "type: System.Windows.Media.Media3D.Point3D[]; size: 200MB")]
    [SuppressMessage("ReSharper.DPA", "DPA0004: Closure object allocation", MessageId = "size: 54MB")]
    [SuppressMessage("ReSharper.DPA", "DPA0002: Excessive memory allocations in SOH", MessageId = "type: ArrayWhereIterator`1[System.Windows.Media.Media3D.Point3D]; size: 133MB")]
    [SuppressMessage("ReSharper.DPA", "DPA0002: Excessive memory allocations in SOH", MessageId = "type: System.Func`2[System.Windows.Media.Media3D.Point3D,System.Boolean]; size: 122MB")]
    private PathsD SliceTriangle(Point3D p1, Point3D p2, Point3D p3, double slicingPlaneHeight){
        var points = new[] { p1, p2, p3 };
        var lineSegments = new PathsD();

        // Determine which points of the triangle are below and above the slicing plane

        // if (p1.Z < slicingPlaneHeight && p2.Z > slicingPlaneHeight ||
        //     p1.Z > slicingPlaneHeight && p2.Z < slicingPlaneHeight)
        // {
        //     // There is an intersection on this slicing height from p1 to p2
        //     // Find the intersection point of the triangle
        //     
        //     intersectionPoints.Add();
        // ... 
        // This is equal to finding the points below / above the slicing plane
        // }
        
        var sliceHeight = slicingPlaneHeight + decimal.ToDouble(tolerance);
        var below = points.Where(v => v.Z < sliceHeight).ToArray();
        var above = points.Where(v => v.Z > sliceHeight).ToArray();

        // Case 1: 2 points below, 1 point above
        if (below.Length == 2 && above.Length == 1)
        {
            // Two intersections since two points of the triangle are below the slicing plane
            var s1 = FindIntersection(below[0], above[0], sliceHeight);
            var s2 = FindIntersection(below[1], above[0], sliceHeight);

            // Get linesegment from s1 to s2
            lineSegments.Add(new PathD(new[] { s1, s2 }));
        }
        
        // Case 2: 1 point below, 2 points above
        if (below.Length == 1 && above.Length == 2)
        {
            var s1 = FindIntersection(above[0], below[0], sliceHeight);
            var s2 = FindIntersection(above[1], below[0], sliceHeight);
            
            // Get linesegment from s1 to s2
            lineSegments.Add(new PathD(new[] { s1, s2 }));
        }
        
        // Case 3: An edge of the triangle is on the slicing plane, 
        // We solve this by using an offset of 0.0001 that is added to the slicingPlaneHeight
        
        // In all other cases, the triangle does not intersect the slicing plane
        return lineSegments;
    }
    
    
    /**
     * Find the intersection between two points a and b at the height of the slicing plane
     * to find the intersection point on the slicing plane
    */
    private PointD FindIntersection(Point3D a, Point3D b, double height)
    {
        // Calculate ratio of difference in height between slicing plane and point 'a'
        double t = (height - a.Z) / (b.Z - a.Z);
        
        // t is used to interpolate X & Y values between points a and b
        double x = a.X + t * (b.X - a.X);
        double y = a.Y + t * (b.Y - a.Y);

        return new PointD(x, y);
    }
    
    /**
    * Loop through all line segments and calculate distances between them to find the next segment
    */
    private PathsD ProcessSegmentsWithClipper(List<PathD> segments)
    {
        var paths = new PathsD();

        if (segments.Count == 0) return paths;
        
        var clip = new PathD
        {
            new PointD(-110, -110),
            new PointD(110, -110),
            new PointD(110, 110),
            new PointD(-110, 110),
            new PointD(-110, -110)
        };

        foreach (var segment in segments)
        {
            // Console.WriteLine("Found point: " + segment[0].ToString() +  " " +  segment[1].ToString());
            // Convert each line segment to a path in Clipper2
            paths.Add(segment);
        }
        
        // Create polygons from the intersection line segments using the “Even-Odd” for the FillRule parameter
        // to ensure contours are correctly identified as solids or holes
        var solution = new PathsD();
        ClipperD clipper = new ClipperD();
        clipper.AddPaths(paths, PathType.Subject);
        clipper.AddPath(clip, PathType.Clip, true);
        clipper.Execute(ClipType.Union, Clipper2Lib.FillRule.EvenOdd, solution);
            
        // Console.WriteLine(solution.Count);
                
        if (solution.Count() != 0)
        {
            paths.Add(solution[0]);
            Console.WriteLine($"Clipper added solution: {solution[0]} - {solution.Count()}");
        }
        
        /*// Use Clipper2 to union the paths into closed polygons
        var solution = new PathsD();
        ClipperD clipper = new ClipperD();
        
        clipper.AddPaths(paths, PathType.Subject, false);
        clipper.AddPath(clip, PathType.Clip, true);
        clipper.Execute(ClipType.Union, Clipper2Lib.FillRule.NonZero, solution);
        
        
        Console.WriteLine("Clipper solutions: " + solution.Count);*/

        return paths;
        
    }
    
    
        /*private List<PathD> testSlicingNaN(double height, ModelVisual3D model)
    {
        // Create a list of line segments from the plane-triangle intersections
        var lineSegments = new List<PathD>();
        
        // Get the Model3DGroup for the stl model that was loaded
        var modelGroup = model.Content as Model3DGroup;
        if (modelGroup == null)
        {
            MessageBox.Show("The STL model is not valid or cannot be processed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return new List<PathD>();
        }
        
        // Iterate through the models in the group to find MeshGeometry3D
        var mesh = modelGroup.Children
            .OfType<GeometryModel3D>()
            .Select(g => g.Geometry as MeshGeometry3D)
            .FirstOrDefault(m => m != null);
        
        var clip = new PathD
        {
            new PointD(-110, -110),
            new PointD(110, -110),
            new PointD(110, 110),
            new PointD(-110, 110),
            new PointD(-110, -110)
        };

        if (mesh == null || mesh.TriangleIndices.Count == 0 || mesh.Positions.Count == 0)
        {
            MessageBox.Show("No valid triangles found in the STL model.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return lineSegments;
        }
        
        for (int i = 0; i < mesh.TriangleIndices.Count; i += 3)
        {
            var v1 = mesh.Positions[mesh.TriangleIndices[i]];
            var v2 = mesh.Positions[mesh.TriangleIndices[i + 1]];
            var v3 = mesh.Positions[mesh.TriangleIndices[i + 2]];

            if (Math.Min(Math.Min(v1.Z, v2.Z), v3.Z) > height || Math.Max(Math.Max(v1.Z, v2.Z), v3.Z) < height) continue;

            var test = new PathD();
            if (Math.Abs(v1.Z - v2.Z) < decimal.ToDouble(tolerance) && Math.Abs(v1.Z - v3.Z) < decimal.ToDouble(tolerance) && Math.Abs(v1.Z - height) < decimal.ToDouble(tolerance))
            {
                Console.WriteLine("Test for NaN " + v1.ToString());
                test = new PathD
                {
                    new PointD(v1.X, v1.Y),
                    new PointD(v2.X, v2.Y),
                    new PointD(v3.X, v3.Y),
                    new PointD(v1.X, v1.Y),
                };
            }
            else
            {
                Console.WriteLine("Test for NaN " + v1.ToString());
                var intersectionPoints = new List<PointD>();
                intersectionPoints.Add(FindIntersection(v1, v2, height));
                intersectionPoints.Add(FindIntersection(v2, v3, height));
                intersectionPoints.Add(FindIntersection(v3, v1, height));

                // Add intersection points to the path
                if (intersectionPoints.Count < 2) continue;
                if (intersectionPoints.Count == 2)
                {
                    test.Add(intersectionPoints[0]);
                    test.Add(intersectionPoints[1]);
                    //test.Add(new PointD(intersectionPoints[0].x + 0.01, intersectionPoints[0].y + 0.01));
                    //test.Add(new PointD(intersectionPoints[1].x + 0.01, intersectionPoints[1].y + 0.01));
                    //test.Add(intersectionPoints[0]);
                }
                else if (intersectionPoints[0] == intersectionPoints[1] || intersectionPoints[1] == intersectionPoints[2])
                {
                    test.Add(intersectionPoints[0]);
                    test.Add(intersectionPoints[2]);
                    //test.Add(new PointD(intersectionPoints[0].x + 0.01, intersectionPoints[0].y + 0.01));
                    //test.Add(new PointD(intersectionPoints[2].x + 0.01, intersectionPoints[2].y + 0.01));
                    //test.Add(intersectionPoints[0]);
                }
                else 
                {
                    test.Add(intersectionPoints[0]);
                    test.Add(intersectionPoints[1]);
                    //test.Add(new PointD(intersectionPoints[0].x + 0.01, intersectionPoints[0].y + 0.01));
                    //test.Add(new PointD(intersectionPoints[1].x + 0.01, intersectionPoints[1].y + 0.01));
                    //test.Add(intersectionPoints[0]);
                }
                lineSegments.Add(test);
                continue;
            }
            
            if(test.Count == 0) continue;
            
            var solution = new PathsD();
            ClipperD clipper = new ClipperD();
            clipper.AddPath(test, PathType.Subject, false);
            clipper.AddPath(clip, PathType.Clip, true);
            clipper.Execute(ClipType.Union, Clipper2Lib.FillRule.EvenOdd, solution);
            
            Console.WriteLine(solution.Count);
                
            if (solution.Count() != 0)
            {
                lineSegments.Add(solution[0]);
                // Console.WriteLine($"Intersection: {slice.Value.Item1} -> {slice.Value.Item2}");
            }
        }
        
        return lineSegments;
    }*/

}