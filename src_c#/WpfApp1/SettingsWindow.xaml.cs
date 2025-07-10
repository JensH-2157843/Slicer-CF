using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WpfApp1;

public partial class SettingsWindow : Window
{
    private SlicerSettings _settings;
    public SlicerSettings UpdatedSettings { get; private set; }

    public SettingsWindow(SlicerSettings settings)
    {
        InitializeComponent();
        _settings = settings;

        InitializeTextValues();
        
        // Create copy of the settings so that changes don't directly modify the main settings until save
        UpdatedSettings = new SlicerSettings
        {
            LayerHeight = settings.LayerHeight,
            NozzleTemperature = settings.NozzleTemperature,
            BedTemperature = settings.BedTemperature,
            FilamentDiameter = settings.FilamentDiameter,
            NozzleDiameter = settings.NozzleDiameter
        };
        // DataContext = UpdatedSettings;
        // Binding didnt work properly so removed this
    }

    private void InitializeTextValues()
    {
        layerHeight.Text = Formatter.FormatDouble(_settings.LayerHeight);
        extruderTemp.Text = Formatter.FormatInt(_settings.NozzleTemperature);
        bedTemp.Text = Formatter.FormatInt(_settings.BedTemperature);
        filamentDiam.Text = Formatter.FormatDouble(_settings.FilamentDiameter);
        nozzleDiam.Text = Formatter.FormatDouble(_settings.NozzleDiameter);
    }

    private void UpdateAllValues()
    {
        UpdatedSettings.LayerHeight = Formatter.FormatString(layerHeight.Text);
        UpdatedSettings.NozzleTemperature = Formatter.FormatSringToInt(extruderTemp.Text);
        UpdatedSettings.BedTemperature = Formatter.FormatSringToInt(bedTemp.Text);
        UpdatedSettings.FilamentDiameter = Formatter.FormatString(filamentDiam.Text);
        UpdatedSettings.NozzleDiameter = Formatter.FormatString(nozzleDiam.Text);
    }
    
    private void OnSaveSettingsClick(object sender, RoutedEventArgs e)
    {
        try
        {
            UpdateAllValues();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
            return;
        }
        
        
        // print updated settings
        Console.WriteLine("New layerheight: " + UpdatedSettings.LayerHeight);
        
        DialogResult = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
    
    private void NumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        TextBox textBox = sender as TextBox;

        // Regex to allow only digits and a single decimal point
        Regex regex = new Regex("[^0-9.]"); // Matches anything that is not a digit or dot
        if (regex.IsMatch(e.Text))
        {
            e.Handled = true; // Block non-numeric and non-dot characters
            return;
        }

        // Allow only one dot
        if (e.Text == "." && textBox.Text.Contains("."))
        {
            e.Handled = true; // Block if there's already a dot (only one allowed)
        }
    }
    
}