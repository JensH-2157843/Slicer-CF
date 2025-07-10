using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WpfApp1;
using HelixToolkit.Wpf;
using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Media.Media3D;
using Clipper2Lib;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
///

public partial class MainWindow : Window
{
    private ModelVisual3D? _printBed;
    private ModelVisual3D? _slicingPlane;
    private ModelVisual3D? _visualModel;
    
    private Slicer? _slicer;
    private STLLoader? _stlLoader;
    private GCodeConverter? _gcodeConverter;
    private Gcodecoverter? _gcodeConverter2;
    private double _slicingPlaneHeight;
    private int _selectedSlicingLayer = 0;
    private double _scaleFactor = 1.0;
    private double _nozzleSize = 0.4;

    private Dictionary<int, PathsD> _slicesDictionary = new Dictionary<int, PathsD>();
    
    public MainWindow()
    {
        InitializeComponent();
        InitializePrintBed();
        InitializeSlicingPlane();
        InitializeSlicer();
        
        // MyComboBox.SelectedItem = "0.4 mm";
    }

    /**
     * Initialize the slicer instance
     */
    private void InitializeSlicer()
    {
        // Use default slicer settings
        SlicerSettings slicerSettings = new SlicerSettings();
        
        this._slicer = new Slicer(slicerSettings);
        this._stlLoader = new STLLoader();
        this._gcodeConverter = new GCodeConverter(slicerSettings);
        this._gcodeConverter2 = new Gcodecoverter();
    }

    /**
     * Initialize the print bed as a fixed square at height Z=0
     */
    private void InitializePrintBed()
    {
        var bedMesh = new MeshBuilder();
        double bedSize = 220; // Bed size (220x220 units)

        // Add a flat square at Z=0
        var points = new Point3DCollection();
        points.Add(new Point3D(-bedSize / 2, -bedSize / 2, -1));
        points.Add(new Point3D(bedSize / 2, -bedSize / 2, -1));
        points.Add(new Point3D(bedSize / 2, -bedSize / 2, -1));
        points.Add(new Point3D(-bedSize / 2, -bedSize / 2, -1));
        
        bedMesh.AddPolygon(points);

        // Create a semi-transparent material for the bed
        var bedMaterial = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(128, 200, 200, 200)));

        _printBed = new ModelVisual3D
        {
            Content = new GeometryModel3D
            {
                Geometry = bedMesh.ToMesh(),
                Material = bedMaterial,
                BackMaterial = bedMaterial,
            }
        };

        HelixViewport.Children.Add(_printBed);
    }
    
    /**
    * Initialize a slicing plane at the current slicingPlaneHeight
    */
    private void InitializeSlicingPlane()
    {
        var planeMesh = new MeshBuilder();
        double planeSize = 220; // Same size as the print bed

        // Add a flat square at the initial slicingPlaneHeight
        var points = new Point3DCollection();
        points.Add(new Point3D(-planeSize / 2, -planeSize / 2, _slicingPlaneHeight));
        points.Add(new Point3D(planeSize / 2, -planeSize / 2, _slicingPlaneHeight));
        points.Add(new Point3D(planeSize / 2, planeSize / 2, _slicingPlaneHeight));
        points.Add(new Point3D(-planeSize / 2, planeSize / 2, _slicingPlaneHeight));
        
        planeMesh.AddPolygon(points);

        // Create a semi-transparent material for the slicing plane
        var planeMaterial = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(128, 255, 0, 0)));

        _slicingPlane = new ModelVisual3D
        {
            Content = new GeometryModel3D
            {
                Geometry = planeMesh.ToMesh(),
                Material = planeMaterial,
                BackMaterial = planeMaterial
            }
        };

        HelixViewport.Children.Add(_slicingPlane);
    }


    /**
     * TODO: Update slicer settings
     */
    private void UpdateSlicerSettings(SlicerSettings updatedSettings)
    {
        // Update in _slicer
        _slicer.UpdateSlicerSettings(updatedSettings);
        
        // Update in GCodeconverter
        _gcodeConverter.UpdateSlicerSettings(updatedSettings); 
    }


    /**
     * Handles the button click for loading an STL file
     */
    private void OnClickLoadSTL(object sender, RoutedEventArgs e)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog();
        openFileDialog.Filter = "STL Files (*.stl)|*.stl";
        openFileDialog.Title = "Load STL file";

        if (openFileDialog.ShowDialog() == true) {
            // Reset slice canvas
            SliceCanvas.Children.Clear();
            
            var file = openFileDialog.FileName;
            GeometryModel3D? geomModel = _stlLoader?.LoadStl(file);

            if (geomModel == null)
            {
                MessageBox.Show("STL file could not be loaded");
            }
            
            // If stl is already loaded, remove old one from view 
            if (_visualModel != null)
            {
                HelixViewport.Children.Remove(_visualModel);
                //  Reset slicing canvas
                SliceCanvas.Children.Clear();
                DisableSlicingLayersSlider();
                // Reset scale to 1.0
                ScaleSlider.Value = 1.0;
                _scaleFactor = 1.0;
            }
             
            // Create new 3D object to place in viewport
            _visualModel = new ModelVisual3D();
            _visualModel.Children.Clear();
            _visualModel.Content = geomModel;
             
            // Add the model to the helix viewport
            HelixViewport.Children.Add(_visualModel);
            
            HelixViewport.RotateGesture = new MouseGesture(MouseAction.LeftClick);
        }
    }
    
    private void OnClickSlice(object sender, RoutedEventArgs e)
    {
        // Check if an STL model is loaded
        if (_visualModel == null || _visualModel.Content == null)
        {
            MessageBox.Show("Please load an STL file before slicing.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        // Disable buttons and show loading spinner modal
        // DisableUI();
        // LoadingModal.Visibility = Visibility.Visible;
    
        try
        {
            Console.WriteLine("Slicing started");
            
            /*var geometryModel = visualModel.Content as GeometryModel3D;
            if (geometryModel == null)
            {
                // Nothing to slice
                return;
            }
            */
            
            var paths = _slicer?.SliceAndGeneratePaths(_slicingPlaneHeight, _visualModel, _scaleFactor);

            if (paths == null || paths.Count == 0)
            {
                // Nothing to slice
                Console.WriteLine("Nothing to slice");
                // Empty the canvas
                SliceCanvas.Children.Clear();
                return;
            }
            else
            {
                Console.WriteLine("Slicing finished");
                VisualizeSlicingPlane(paths);

                _gcodeConverter?.ExportGCodeForSingleSlice(paths, _selectedSlicingLayer);
                
                // TODO: Take working code of gcodeConverter2 and merge into gcodeConverter
                // gcodeConverter2.Gcode(paths);
            }
        }
        catch (Exception err)
        {
            MessageBox.Show($"An error occurred during slicing: {err.Message}", 
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        
        // // Hide modal and enable buttons again
        // LoadingModal.Visibility = Visibility.Collapsed;
        // EnableUI();
    }

    /**
     * Used for testing. Not needed in final application.
     */
    private void OnClickExportSlice(object sender, RoutedEventArgs e)
    {
        _slicesDictionary.TryGetValue(_selectedSlicingLayer, out var slice);

        if (slice == null || slice.Count == 0)
        {
            MessageBox.Show("Nothing to export.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        _gcodeConverter?.ExportGCodeForSingleSlice(slice, _selectedSlicingLayer);
    }

    private void OnClickExportAll(object sender, RoutedEventArgs e)
    {
        _gcodeConverter?.ExportGCode(_slicesDictionary);
    }
    
    private void UpdateSlicingLayersSlider(Dictionary<int, PathsD> slicesDictionary, int selectedSlicingLayer = 0)
    {
        // _selectedSlicingLayer
        
        var numberOfLayers = slicesDictionary.Count;
        SlicingPlaneSlider.Minimum = 0;
        SlicingPlaneSlider.Maximum = numberOfLayers-1;
        SlicingPlaneSlider.Interval = 1;
        SlicingPlaneSlider.TickFrequency = 1;
        SlicingPlaneSlider.Value = selectedSlicingLayer;
        SlicingPlaneSlider.IsSnapToTickEnabled = true;
        
        // Select layer (first by default)
        SlicingPlaneSlider.Value = selectedSlicingLayer;
        _selectedSlicingLayer = selectedSlicingLayer;
        
        // Make visible
        EnableSlicingLayersSlider();
        
    }

    private void DisableSlicingLayersSlider()
    {
        SlicingPlaneSlider.IsEnabled = false;
        Console.WriteLine("Disabling Slicing Layers Slider");
    }

    private void EnableSlicingLayersSlider()
    {
        SlicingPlaneSlider.IsEnabled = true;
        Console.WriteLine("Enabling Slicing Layers Slider");
    }

    private void VisualiseSliceAtSelectedLayer()
    {
        // Get slice at _selectedSlicingLayer
        var sliceRetrieved = _slicesDictionary.TryGetValue(_selectedSlicingLayer, out var slice);

        if (!sliceRetrieved || slice == null)
        {
            MessageBox.Show("Object is not touching the bed. Please realign before continuing.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
            
        // Visualise slice at 0.0
        VisualizeSlicingPlane(slice);
    }
    
    private void OnClickSliceModel(object sender, RoutedEventArgs e)
    {
        // Reset slicesDictionary
        _slicesDictionary.Clear();
        
        // Check if an STL model is loaded
        if (_visualModel == null || _visualModel.Content == null)
        {
            MessageBox.Show("Please load an STL file before slicing.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        try
        {
            var allSlices = _slicer?.SliceModel(_visualModel, _scaleFactor);
            if (allSlices == null) return; // nothing to slice
            _slicesDictionary = allSlices;
            
            Console.Write(_slicesDictionary.Count);

            // Reset selected layer to 0
            _selectedSlicingLayer = 0;
            
            UpdateSlicingLayersSlider(_slicesDictionary, _selectedSlicingLayer);
            UpdateSlicingPlanePosition();
            VisualiseSliceAtSelectedLayer();
            EnableSlicingLayersSlider();

        }
        catch (Exception err)
        {
            MessageBox.Show($"An error occurred during slicing: {err.Message}", 
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        
        // // Hide modal and enable buttons again
        // LoadingModal.Visibility = Visibility.Collapsed;
        // EnableUI();
    }
    
    /*private void DisableUi()
    {
        foreach (var element in LogicalTreeHelper.GetChildren(this))
        {
            if (element is Button button)
            {
                button.IsEnabled = false;
            }
        }
        
        SlicingPlaneSlider.IsEnabled = false;
    }
    
    private void EnableUi()
    {
        foreach (var element in LogicalTreeHelper.GetChildren(this))
        {
            if (element is Button button)
            {
                button.IsEnabled = true;
            }
        }

        SlicingPlaneSlider.IsEnabled = true;
    }*/

    private void OnSlicingPlaneHeightChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _slicingPlaneHeight = e.NewValue;
        SliderValueTextBlock.Text = $"{_slicingPlaneHeight:F2} mm";
        
        // Check if an STL file has been loaded
        if (_visualModel != null)
        {
            // var paths = slicer.SliceAndGeneratePaths(slicingPlaneHeight, geometryModel);
            // VisualizeSlicingPlane(paths);
            UpdateSlicingPlanePosition();
        }
    }

    private void OnSlicingLayerChange(object sender, RoutedEventArgs e)
    {
        try
        {
            var selLayer = (int)SlicingPlaneSlider.Value;
            _selectedSlicingLayer = selLayer;
            
            Console.WriteLine($"Selected Slicing Layer: {_selectedSlicingLayer}");
        
            UpdateSlicingLayersSlider(_slicesDictionary, _selectedSlicingLayer);
            UpdateSlicingPlanePosition();
            VisualiseSliceAtSelectedLayer();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in OnSlicingLayerChange: {ex.Message}");
        }
    }
    
    /**
     * Move the slicing plane to the new slicingPlaneHeight
     */
    private void UpdateSlicingPlanePosition()
    {
        if (_slicingPlane != null && _slicer != null)
        {
            _slicingPlaneHeight = (_selectedSlicingLayer + 1) * _slicer.Settings().LayerHeight;

            if (_slicingPlane == null)
            {
                return;
            }
            
            // Update the plane's position using a transform
            _slicingPlane.Transform = new TranslateTransform3D(0, 0, _slicingPlaneHeight);
        }
    }
    
    private void VisualizeSlicingPlane(PathsD paths)
    {
        // Clear current canvas
        SliceCanvas.Children.Clear();

        if (paths.Count == 0)
        {
            Console.WriteLine("No paths to visualize.");
            return;
        }

        // Show the paths on the canvas
        foreach (var path in paths)
        {
            var polygon = new Polyline
            {
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };

            foreach (var point in path)
            {
                polygon.Points.Add(new Point(ScaleToCanvasX(point.x), ScaleToCanvasY(point.y)));
            }

            // Get polygon center
            // var polyCenter = new Point(polygon.Points.Average(p => p.X), polygon.Points.Average(p => p.Y));
            // //
            // // // Flip polygon so it is displayed accurately to the way it will be printed
            // // var flipTransform = new ScaleTransform(1, -1, polyCenter.X, polyCenter.Y);
            // var rotateTransform = new RotateTransform(180, polyCenter.X, polyCenter.Y);
            //
            // var transformGroup = new TransformGroup();
            // // transformGroup.Children.Add(flipTransform);
            // transformGroup.Children.Add(rotateTransform);
            // //
            // polygon.RenderTransform = transformGroup;
            // //
            // // polygon.RenderTransform = new RotateTransform(180, polyCenter.X, polyCenter.Y);
            
            SliceCanvas.Children.Add(polygon);
        }
    }
    
    private double ScaleToCanvasX(double x)
    {
        var geometryModel = _visualModel?.Content as GeometryModel3D;
        if (geometryModel == null) return x;
        
        // Calculate the scale factor to fit the canvas while maintaining aspect ratio
        double scaleX = SliceCanvas.Width / geometryModel.Bounds.SizeX;
        double scaleY = SliceCanvas.Height / geometryModel.Bounds.SizeY;
        
        double factor = Math.Min(scaleX, scaleY);
        // double paddingOffsetFactor = 0.9;
        
        return (x * factor) / _scaleFactor;
        // return ((x * factor) / _scaleFactor)*paddingOffsetFactor;
        // return x * 2 // scale to canvas for relative size to print bed
    }

    private double ScaleToCanvasY(double y)
    {
        var geometryModel = _visualModel?.Content as GeometryModel3D;
        if (geometryModel == null) return y;

        double scaleX = SliceCanvas.Width / geometryModel.Bounds.SizeX;
        double scaleY = SliceCanvas.Height / geometryModel.Bounds.SizeY;
        
        double factor = Math.Min(scaleX, scaleY);
        // double paddingOffsetFactor = 0.9;
        
        return (y * factor) / _scaleFactor;

        // return ((y * factor) / _scaleFactor)*paddingOffsetFactor;
        // return x * 2 // scale to canvas for relative size to print bed
    }

    private void MyComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if(((ComboBox) sender).SelectedItem.ToString() == null) return;
        _nozzleSize = Convert.ToDouble(((ComboBox) sender).SelectedItem.ToString()!.Substring(38,3));
    }
    
    private void OnScaleSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_visualModel != null)
        {
            double scaleValue = e.NewValue;
            ScaleValueTextBlock.Text = $"Scale: {scaleValue:F1}";

            this._scaleFactor = scaleValue;

            // Update the scale of the STL model in the helix viewport
            ScaleModel(scaleValue);
        }
    }
    
    private void ScaleModel(double scale)
    {
        // Reset slicing canvas
        SliceCanvas.Children.Clear();
        SliceCanvas.Children.RemoveRange(0,(SliceCanvas.Children.Count - 1));
        
        // Disable slicing slider
        DisableSlicingLayersSlider();
        
        if (_visualModel == null)
        {
            MessageBox.Show("Cannot scale because no stl file has been loaded yet.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        
        Console.WriteLine("Scaling model with factor "+scale);
        
        var transformGroup = new Transform3DGroup();
        var scaleTransform = new ScaleTransform3D(scale, scale, scale);
        
        // Add scaling
        transformGroup.Children.Add(scaleTransform);
        
        /*
        // Center the model
        var centerTranslation = getCenterGeometryTranslation(visualModel.Content as GeometryModel3D);
        */

        // Preserve the existing translation to keep the model centered on the print bed
        var existingTransformations = _visualModel.Transform;

        if (existingTransformations is Transform3DGroup existingGroup)
        {
            foreach (var transform in existingGroup.Children)
            {
                if (transform is not ScaleTransform3D)
                {
                    transformGroup.Children.Add(transform);
                }
            }
        }
        
        // Apply the transformations to the STL model (3D model shown on the left)
        _visualModel.Transform = transformGroup;
        // Apply the transformations to the Geometry as well
        // visualModel.Content.Transform = transformGroup;
        
        if (ExceedsBedDimensions(_visualModel))
        {
            // TODO: Show warning on screen
            Console.Write("WARNING! The model has exceeded the bed dimensions. DO NOT PRINT.");
        }
        
        // 
        
    }

    private bool ExceedsBedDimensions(ModelVisual3D model)
    {
        // Check if the width of the model is bigger than the bed (220)
        if (model.Content.Bounds.SizeX > _slicer?.Settings().BedWidth)
        {
            Console.WriteLine("WARNING! The model has exceeded the bed dimensions.");
            return true;
        }

        // Check if the depth of the model is bigger than the bed (220)
        if (model.Content.Bounds.SizeY > _slicer?.Settings().BedDepth)
        {
            Console.WriteLine("WARNING! The model has exceeded the bed dimensions.");
            return true;
        }

        // Check if the height of the model is bigger than the bed (250)
        if (model.Content.Bounds.SizeZ > _slicer?.Settings().BedHeight)
        {
            Console.WriteLine("WARNING! The model has exceeded the bed dimensions.");
            return true;
        }

        return false;
    }

    /**
     * Open the slicer settings window
     */
    private void OnClickEditSettings(object sender, RoutedEventArgs e)
    {
        
        if (_slicer == null)
        {
            MessageBox.Show("Slicer is not initialized.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        SettingsWindow settingsWindow = new SettingsWindow(_slicer.Settings());
        
        // If settings saved
        if (settingsWindow.ShowDialog() == true)
        {
            // Get updated settings and apply them to the slicer
            var updatedSettings = settingsWindow.UpdatedSettings;
            
            // Update slicer settings wherever necessary
            UpdateSlicerSettings(updatedSettings);
        }
    }
}