using System.Globalization;

namespace WpfApp1;

public class GCodeCommands
{
    /**
    * Homes all axes
    */
    public static string HomePrinterCommand()
    {
        return "G28 ; Home axes";
    }

    public static string SetUnitsToMmCommand()
    {
        return "G21 ; Set Units to MM";
    }

    /**
     * Will use absolute coordinates
     */
    public static string UseAbsolutePositioningCommand()
    {
        return "G90 ; Use Absolute Positioning";
    }
    
    /**
     * Will use relative coordinates
     */
    public static string UseRelativePositioningCommand()
    {
        return "G91 ; Use Relative Positioning";
    }

    /**
     * Set bed temperature
     */
    public static string SetBedTemperatureCommand(int temperature)
    {
        return $"M140 S{temperature} ; Set Bed Temperature";
    }

    /**
     * Wait until bed has reached given temperature
     */
    public static string AwaitBedTemperatureCommand(int temperature)
    {
        return $"M190 S{temperature} ; Awaiting Bed Temperature";
    }

    /**
     * Set extruder temperature
     */
    public static string SetExtruderTemperatureCommand(int temperature)
    {
        return $"M104 S{temperature} ; Set Extruder Temperature";

    }
    
    /**
    * Wait until extruder has reached given temperature
    */
    public static string AwaitExtruderTemperatureCommand(int temperature)
    {
        return $"M109 S{temperature} ; Awaiting Extruder Temperature";
    }

    public static string FanOnCommand()
    {
        return "M106 ; Fan On";
    }

    public static string FanOffCommand()
    {
        return "M107 ; Fan Off";
    }

    /**
     * This command is used to override G91 and put the E axis into absolute mode independent of the other axes.
     */
    public static string UseAbsoluteMovementCommand()
    {
        return "M82 ; Use Absolute Movement (extruder)";
    }

    /**
     * This command is used to override G90 and put the E axis into absolute mode independent of the other axes.
     */
    public static string UseRelativeMovementCommand()
    {
        return "M83 ; Use Relative Movement (extruder)";
    }

    public static string DisableStepperMotorsCommand()
    {
        return "M84 ; Disable Stepper Motors";
    }

    /**
     * Move to the position at (x,y) at current height without extruding
     */
    public static string MoveToPositionCommand(double x, double y, double feedRate)
    {
        return $"G0 X{Formatter.FormatDouble(x)} Y{Formatter.FormatDouble(y)} F{Formatter.FormatDouble(feedRate)}; Move To Position";
    }
    
    /**
    * Move to the position at (x,y,z) without extruding
    */
    public static string MoveToPositionCommand(double x, double y, double z, double feedRate)
    {
        return $"G0 X{Formatter.FormatDouble(x)} Y{Formatter.FormatDouble(y)} Z{Formatter.FormatDouble(z)} F{Formatter.FormatDouble(feedRate)}; Move To Position";
    }

    /**
     * Move to the position at (x,y) at current height while extruding
     */
    public static string ExtrudeToPositionCommand(double x, double y, double e, double feedRate)
    {
        return $"G1 X{Formatter.FormatDouble(x)} Y{Formatter.FormatDouble(y)} F{Formatter.FormatDouble(feedRate)} E{e.ToString()}; Extrude to Position";
    }
    
    /**
     * Move to the position at (x,y,z) while extruding
     */
    public static string ExtrudeToPositionCommand(double x, double y, double z, double e, double feedRate)
    {
        return $"G1 X{Formatter.FormatDouble(x)} Y{Formatter.FormatDouble(y)} Z{Formatter.FormatDouble(z)} F{Formatter.FormatDouble(feedRate)} E{Formatter.FormatDouble(e)}; Extrude to Position";
    }

    /**
     * Reset extruder position to zero
     */
    public static string ResetExtruderCommand()
    {
        return "G92 E0 ; Reset Extruder";
    }

    /**
     * Raises the nozzle just a little bit (2mm) above the bed
     * to prevent scratching the bed surface with a speed of 3000 mm/min
     */
    public static string RaiseNozzleBeforeStartCommand()
    {
        return "G0 Z2.0 F3000 ; Move Z Axis up little to prevent scratching of Heat Bed";
    }
    
    /**
     * Move to the position at (x,y) at current height while retracting filament
     */
    public static string RetractToPositionCommand(double retractionDistance, double feedRate, double x, double y)
    {
        return $"G1 X{Formatter.FormatDouble(x)} Y{Formatter.FormatDouble(y)} E{(Formatter.FormatDouble(-retractionDistance))} ; Retract and Move To Position";
    }

    public static string MoveZCommand(double z)
    {
        return $"G0 Z{Formatter.FormatDouble(z)} ; Raise or lower Z position";
    }

    public static string PrintFinishedPositioningCommand()
    {
        return "G0 X0 Y235 ;Present print";
    }

    public static string RetractFilamentCommand(double amount)
    {
        return $"G1 E-{amount} F1800; Retract Filament";
    }
}