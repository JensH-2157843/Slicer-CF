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
    public double LayerHeight = 0.20; // mm
    public double NozzleDiameter = 0.4; // mm
    
    public int NozzleTemperature = 200; // degrees celcius
    public int BedTemperature = 35; // degrees celcius

    public double FilamentDiameter = 1.75; // mm
    public double ExtrusionRate = 0.05;

    public double BedWidth = 220; // mm
    public double BedDepth = 220; // mm
    public double BedHeight = 250; // mm
    
    public int TravelSpeed = 50; // mm/s
    public int PrintSpeed = 50; // mm/s

    
    /**
     * Following function can be used to update all setings at once
     */
    public void SetSlicerSettings(
        double layerHeight, 
        double nozzleDiameter, 
        int nozzleTemperature, 
        int bedTemperature,
        double filamentDiameter,
        double extrusionRate)
    {
        this.LayerHeight = layerHeight;
        this.NozzleDiameter = nozzleDiameter;
        this.NozzleTemperature = nozzleTemperature;
        this.BedTemperature = bedTemperature;
        this.FilamentDiameter = filamentDiameter;
        this.ExtrusionRate = extrusionRate;
        
        // bed width, depth and height
        // support
        // nr of shells
        // ...
    }
}