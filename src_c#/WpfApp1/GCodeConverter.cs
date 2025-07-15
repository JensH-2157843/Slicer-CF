using System.IO;
using System.Numerics;
using System.Windows;
using System.Windows.Documents;
using Clipper2Lib;
using Microsoft.Win32;

namespace WpfApp1;

public class GCodeConverter
{
    // List of gcode commands
    List<string> gcodeList = new List<string>();
    
    /* Slicer settings */
    private SlicerSettings settings;
    
    public GCodeConverter(SlicerSettings settings)
    {
        this.settings = settings;
    }

    public void UpdateSlicerSettings(SlicerSettings updatedSettings)
    {
        this.settings = updatedSettings;
        
        Console.WriteLine("Slicer settings updated in GCODECONVERTER.");
        /*Console.WriteLine("Slicer settings updated in SLICER. New settings: ");
        Console.WriteLine("Layerheight: " + updatedSettings.LayerHeight);
        Console.WriteLine("NozzleTemperature: " + updatedSettings.NozzleTemperature);
        Console.WriteLine("BedTemperature: " + updatedSettings.BedTemperature);
        Console.WriteLine("FilamentDiameter: " + updatedSettings.FilamentDiameter);
        Console.WriteLine("NozzleDiameter: " + updatedSettings.NozzleDiameter);*/
    }
    
    /**
     * Finds the index of the path closest to current position.
     */
    private static int FindNearestPath(PointD currentPosition, PathsD paths)
    {
        double minDistance = double.MaxValue;
        int nearestIndex = 0;

        for (int i = 0; i < paths.Count; i++)
        {
            double distance = CalculateDistance(currentPosition, paths[i][0]);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestIndex = i;
            }
        }

        return nearestIndex;
    }
    
    /**
    * Makes sure the points in a path are ordered to reduce unecessary travel
    */
    private static PathD OrderPointsInPath(PathD path, PointD currentPosition)
    {
        double distanceToStart = CalculateDistance(currentPosition, path[0]);
        double distanceToEnd = CalculateDistance(currentPosition, path[^1]);

        if (distanceToEnd < distanceToStart)
        {
            path.Reverse();
        }

        return path;
    }

    
    /**
    * Calculates the Euclidean distance between two points.
    */
    private static double CalculateDistance(PointD p1, PointD p2)
    {
        return Math.Sqrt(Math.Pow(p2.x - p1.x, 2) + Math.Pow(p2.y - p1.y, 2));
    }

    private PathsD OrderPaths(PathsD paths)
    {
        PathsD orderedPaths = new PathsD();
        PointD currentPosition = paths[0][0];

        while (paths.Count > 0)
        {
            int nearestPathIndex = FindNearestPath(currentPosition, paths);
            PathD nearestPath = paths[nearestPathIndex];

            // Order the points in a path to minimize travel distance
            PathD orderedPath = OrderPointsInPath(nearestPath, currentPosition);

            orderedPaths.Add(orderedPath);
            currentPosition = orderedPath[^1];
            paths.RemoveAt(nearestPathIndex);
        }

        return orderedPaths;
    }
    
    /**
    * Checks if a given PathD is closed
    */
    public static bool IsPathClosed(PathD path)
    {
        if (path.Count < 2) return false;

        // Check if the first and last points are the same
        var first = path[0];
        var last = path[^1];

        // Use epsilon for floating-point comparisons
        const double epsilon = 1e-6;
        // return first.x == last.x && first.y == last.y;
        return Math.Abs(first.x - last.x) < epsilon && Math.Abs(first.y - last.y) < epsilon;
    }
    
    private List<string> GenerateGCodeFromSlice(PathsD slice, double xOffset, double yOffset, int layer)
    {
        List<string> gcode = new List<string>();
        double extrusionAmount = 0.0;
        
        // Order the paths such that extruder makes least amount of unessecary travel
        PathsD orderedPaths = OrderPaths(slice);
        // PathsD orderedPaths = slice;
        PointD prev = new PointD();
        PointD start = orderedPaths[0][0];
        bool isFirst = true;
        double tolerance = 0.000001;
        int counter = 0;

        
        // Generate G-code for each path
        foreach (var path in orderedPaths)
        {
            if (path.Count < 2) continue; // Skip invalid paths

            // Move to the start of the path
            PointD startPoint = path[0];
            if(! (Math.Abs(prev.x - startPoint.x) < tolerance && Math.Abs(startPoint.y - prev.y) < tolerance))
            {
                if(!(Math.Abs(prev.x - start.x) < tolerance && Math.Abs(start.y - prev.y) < tolerance) && (!isFirst || layer != 0))
                {
                    ++counter;
                    double closingExtrusion = GCodeHelper.CalculateExtrusion(prev, start, settings);
                    extrusionAmount += closingExtrusion;
                    gcode.Add(GCodeCommands.ExtrudeToPositionCommand(start.x + xOffset, start.y + yOffset,
                            extrusionAmount,
                            500));
                } else isFirst = false;
                gcode.Add(GCodeCommands.MoveToPositionCommand(startPoint.x + xOffset, startPoint.y + yOffset, 1500));
                start = startPoint;
                
            }
            // Extrude along the path
            for (int i = 1; i < path.Count; i++)
            {
                PointD previousPoint = path[i - 1];
                PointD currentPoint = path[i];

                // double distance = CalculateDistance(previousPoint, currentPoint);
                double extrusion = GCodeHelper.CalculateExtrusion(previousPoint, currentPoint, settings);
                extrusionAmount += extrusion;

                gcode.Add(GCodeCommands.ExtrudeToPositionCommand(currentPoint.x + xOffset, currentPoint.y + yOffset, extrusionAmount,
                    500));
                prev = currentPoint;
            }

            // Close the loop if the path is closed
            // (Isn't this done automatically with clipper -- last point is first point?)
            //if (IsPathClosed(path))
            //{
            //    ++ counter;
            //    PointD lastPoint = path[^1];
                // double closingDistance = CalculateDistance(lastPoint, startPoint);
            //    double closingExtrusion = GCodeHelper.CalculateExtrusion(lastPoint, startPoint, settings);
            //    extrusionAmount += closingExtrusion;

            //    gcode.Add(GCodeCommands.ExtrudeToPositionCommand(startPoint.x + xOffset, startPoint.y + yOffset,
            //        extrusionAmount,
            //        500));
            //}
        }
        
        //Console.WriteLine(counter);
        return gcode;
    }


    /**
     * Takes a single slice and produces gcode to print the given layer
     */
    private List<string> SliceToGCode(PathsD slice, int layer)
    {
        if (slice.Count == 0)
        {
            Console.WriteLine("No slice found");
            return new List<string>();
        }
        
        List<string> gcodeCommandList = new List<string>();
        
        // Calculate the bounding box of the model
        var (xOffset, yOffset) = GCodeHelper.GetXAndYOffsetsToCenter(slice, settings);

        var tempcode = GenerateGCodeFromSlice(slice, xOffset, yOffset, layer);
        gcodeCommandList.AddRange(tempcode);
        return gcodeCommandList;
        
        double extrusionAmount = 0.0;
        
        // Convert paths to valid printing sequences
        List<PathsD> validPaths = GCodeHelper.GetValidPaths(slice);
        
        // Generate G-code for each valid path
        foreach (var validPath in validPaths)
        {
            PointD startPoint = validPath[0][0];
            PointD previousPoint = startPoint;
            
            // Move to the starting point of the path (centered on the bed)
            gcodeCommandList.Add(GCodeCommands.MoveToPositionCommand(
                startPoint.x + xOffset, startPoint.y + yOffset, 2700));

            // Iterate through each segment in the path
            foreach (var path in validPath)
            {
                foreach (var point in path)
                {
                    if (GCodeHelper.CheckPointsDistance(previousPoint, point))
                    {
                        continue; // Skip redundant points
                    }

                    // Calculate extrusion for the move
                    extrusionAmount += GCodeHelper.CalculateExtrusion(previousPoint, point, settings);

                    // Generate extrusion G-code
                    gcodeCommandList.Add(GCodeCommands.ExtrudeToPositionCommand(
                        point.x + xOffset, point.y + yOffset, extrusionAmount, 500));
                
                    previousPoint = point;
                }
            }

            // Complete the path by returning to the start
            extrusionAmount += GCodeHelper.CalculateExtrusion(previousPoint, startPoint, settings);
            gcodeCommandList.Add(GCodeCommands.ExtrudeToPositionCommand(
                startPoint.x + xOffset, startPoint.y + yOffset, extrusionAmount, 500));
        }

        return gcodeCommandList;
    }
    
    /**
     * GCode to print a prime line at the side of the bed
     */
    private List<string> PrintPrimeLineCommands()
    {
        List<string> gcodeCommandList = new List<string>();
        
        PointD startPoint = new PointD(0.1, 20);
        PointD endPoint = new PointD(0.1, 200);
        
        double zHeight = 0.3;
        double extrusionAmount = GCodeHelper.CalculateExtrusion(startPoint, endPoint, settings);

        // Reset extruder
        gcodeCommandList.Add(GCodeCommands.ResetExtruderCommand());
        
        // Move the nozzle to coordinates (X=0.1, Y=20, Z=0.3)
        // at a speed of 5000 mm/min to prepare for the first print stroke.
        gcodeCommandList.Add(GCodeCommands.MoveToPositionCommand(startPoint.x, endPoint.y, zHeight, 5000));
        
        // Moves the nozzle along the Y-axis (from Y=20 to Y=200) at Z=0.3 mm
        // and a speed of 1500 mm/min while extruding 15 mm of filament.
        gcodeCommandList.Add(GCodeCommands.ExtrudeToPositionCommand(startPoint.x, endPoint.y, zHeight, extrusionAmount, 1500));
        
        return gcodeCommandList;;
    }

    private List<string> GetInitialCommands()
    {
        List<string> gcodeCommandList = new List<string>();
        // Home
        gcodeCommandList.Add(GCodeCommands.HomePrinterCommand());
        // General settings
        gcodeCommandList.Add(GCodeCommands.SetUnitsToMmCommand());
        
        // Absolute positioning
        gcodeCommandList.Add(GCodeCommands.UseAbsolutePositioningCommand());
        gcodeCommandList.Add(GCodeCommands.UseAbsoluteMovementCommand());
        
        // Raise nozzle to prevent scratching
        gcodeCommandList.Add(GCodeCommands.RaiseNozzleBeforeStartCommand());
        
        // Set temperature settings
        gcodeCommandList.AddRange(GetTemperatureSettingsCommands());
        
        // Print prime line
        gcodeCommandList.AddRange(PrintPrimeLineCommands());
        
        // Reset extruder postion to zero again to prepare for print
        gcodeCommandList.Add(GCodeCommands.ResetExtruderCommand());
        
        // Raise nozzle again to prepare for print
        gcodeCommandList.Add(GCodeCommands.RaiseNozzleBeforeStartCommand());

        return gcodeCommandList;
    }

    private void AddPrintDoneCommands()
    {
        gcodeList.AddRange(GetPrintDoneCommands());
    }

    private List<string> GetPrintDoneCommands()
    {
        List<string> gcodeCommandList = new List<string>();
        
        // Retract the filament to prevent oozing when print is done
        gcodeCommandList.Add(GCodeCommands.RetractFilamentCommand(2));

        // Get hot end out of the way
        gcodeCommandList.Add(GCodeCommands.PrintFinishedPositioningCommand());
        
        // Turn off fan, extruder and bed
        gcodeCommandList.Add(GCodeCommands.FanOffCommand());
        gcodeCommandList.Add(GCodeCommands.SetBedTemperatureCommand(0));
        gcodeCommandList.Add(GCodeCommands.SetExtruderTemperatureCommand(0));
        
        // Disable stepper motors
        gcodeCommandList.Add(GCodeCommands.DisableStepperMotorsCommand());
        return gcodeCommandList;
    }

    /**
     * Gets all the commands needed to se the temperature of the bed and extruder
     * to the temp provided in the settings.
     * Commands will first heat up the bed, wait until this is done, then heat
     * up the extruder and then wait until this is done before next command will
     * execute.
     */
    private List<string> GetTemperatureSettingsCommands()
    {
        List<string> gcodeCommandList = new List<string>();
        // Settings taken from the private variable
        gcodeCommandList.Add(GCodeCommands.SetBedTemperatureCommand(settings.BedTemperature));
        gcodeCommandList.Add(GCodeCommands.AwaitBedTemperatureCommand(settings.BedTemperature));
        
        gcodeCommandList.Add(GCodeCommands.SetExtruderTemperatureCommand(settings.NozzleTemperature));
        gcodeCommandList.Add(GCodeCommands.AwaitExtruderTemperatureCommand(settings.NozzleTemperature));
        
        return gcodeCommandList;
    }
    
    public void ExportGCodeForSingleSlice(PathsD slice, int layer)
    {
        List<string> gcodeCommandList = new List<string>();
        
        // Initial commands
        gcodeCommandList.AddRange(GetInitialCommands());
            
        List<string> generatedGCode = SliceToGCode(slice, layer);
        gcodeCommandList.Add($";LAYER: {layer}");
        gcodeCommandList.AddRange(generatedGCode);
        
        AddPrintDoneCommands();

        Console.WriteLine("Writing gcode to file");
        
        // Save to file
        SaveFileDialog saveFileDialog = new SaveFileDialog
        {
            Filter = "G-code Files (*.gcode)|*.gcode",
            Title = "Save G-code File"
        };

        if (saveFileDialog.ShowDialog() == true)
        {
            File.WriteAllLines(saveFileDialog.FileName, gcodeCommandList);
        }
    }


    private List<string> GetTransitionToNextLayerCommands(PathsD currentLayer, PathsD nextLayer)
    {
        List<string> gcodeCommandList = new List<string>();
        
        // Move up by layerheight
        gcodeCommandList.Add(GCodeCommands.MoveZCommand(settings.LayerHeight));
        
        // TODO: what else?
        
        return gcodeCommandList;
    }
    
    /**
     * Generate GCode for all layers in slices
     * with begin print and end print sequence
     */
    public void ExportGCode(Dictionary<int, PathsD> slices)
    {
        if (slices.Count == 0)
        {
            // Nothing to export
            MessageBox.Show("No slices to export");
            return;
        }
        
        // Previously used private variable gcodelist but not necessary
        
        List<string> gcodeCommandList = new List<string>();
        int numberOfLayers = slices.Count;
        int lastLayerIndex = numberOfLayers - 1;
        
        // Initial commands
        gcodeCommandList.AddRange(GetInitialCommands());
        
        
        // TODO: Properly convert slices to gcode

        // For each layer, slice to gcode and go up by layerheight
        foreach (var kvp in slices)
        {
            int layer = kvp.Key;
            PathsD slice = kvp.Value;
            
            List<string> generatedGCode = SliceToGCode(slice, layer);
            
            gcodeCommandList.Add($";LAYER: {layer}");
            gcodeCommandList.Add(GCodeCommands.MoveZCommand((layer + 1) * settings.LayerHeight));
            gcodeCommandList.AddRange(generatedGCode);
            
            // Check if this is the last layer
            if (layer == lastLayerIndex || numberOfLayers == 1)
            {
                // Finalize print
            }
            else
            {
                PathsD nextLayer = slices[layer+1];
                
                // Transition to next layer
                GetTransitionToNextLayerCommands(slice, nextLayer);
            }
            
            // Continue untill all layers are sliced
        }
        
        AddPrintDoneCommands();

        Console.WriteLine("Writing gcode to file");
        
        // Save to file
        SaveFileDialog saveFileDialog = new SaveFileDialog
        {
            Filter = "G-code Files (*.gcode)|*.gcode",
            Title = "Save G-code File"
        };

        if (saveFileDialog.ShowDialog() == true)
        {
            File.WriteAllLines(saveFileDialog.FileName, gcodeCommandList);
        }
    }
    
}