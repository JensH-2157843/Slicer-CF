using System.ComponentModel;

namespace WpfApp1;

public class SlicerSettings : INotifyPropertyChanged
{
    
    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    
    // Default settings, can be overwritten
    public decimal LayerHeight = 0.20m; // mm
    public decimal NozzleDiameter = 0.4m; // mm
    
    public int NozzleTemperature = 200; // degrees celcius
    public int BedTemperature = 35; // degrees celcius

    public decimal FilamentDiameter = 1.75m; // mm
    public double  FilamentArea => Math.PI * Math.Pow(decimal.ToDouble(FilamentDiameter) / 2, 2);
    public decimal ExtrusionRate = 0.05m;

    public decimal BedWidth = 220m; // mm
    public decimal BedDepth = 220m; // mm
    public decimal BedHeight = 250m; // mm
    
    public int TravelSpeed = 50; // mm/s
    public int PrintSpeed = 50; // mm/s

    public int NumberShells = 1;

    
    /**
     * Following function can be used to update all setings at once
     */
    public void SetSlicerSettings(
        decimal layerHeight, 
        decimal nozzleDiameter, 
        int nozzleTemperature, 
        int bedTemperature,
        decimal filamentDiameter,
        decimal extrusionRate,
        int NShells)
    {
        this.LayerHeight = layerHeight;
        this.NozzleDiameter = nozzleDiameter;
        this.NozzleTemperature = nozzleTemperature;
        this.BedTemperature = bedTemperature;
        this.FilamentDiameter = filamentDiameter;
        this.ExtrusionRate = extrusionRate;
        this.NumberShells = NShells;
        
        // bed width, depth and height
        // support
        // nr of shells
        // ...
    }
}