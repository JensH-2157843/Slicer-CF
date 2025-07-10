using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Clipper2Lib;
using HelixToolkit.Wpf;
using FillRule = System.Windows.Media.FillRule;

namespace WpfApp1;

public class STLLoader
{
    public STLLoader()
    {
        // Do nothing yet
    }
    
    /**
     * Loads the STL file at given filePath into 3D model. 
     */
    public GeometryModel3D? LoadStl(string filePath)
    {
        try
        {
            var stlReader = new StLReader();
            Model3DGroup group = stlReader.Read(filePath);
            GeometryModel3D? geomModel = FindLargestModel(group);
            if (geomModel == null || geomModel.Geometry == null)
            {
                MessageBox.Show("Empty STL file");
                return null;
            }
            
            MeshGeometry3D mesh = geomModel?.Geometry as MeshGeometry3D;
            if (mesh == null)
            {
                MessageBox.Show("Empty STL file");
                return null;
            }

            // Center the model in the 3D viewport
            var transformGroup = new Transform3DGroup();
            var centerTranslation = getCenterGeometryTranslation(geomModel);
            transformGroup.Children.Add(centerTranslation);
            
            // // Check if it exceeds bed dimensions, and auto scale to fit
            // if (ExceedsBedDimensions(modelWidth, modelDepth, modelHeight))
            // {
            //     // Get scale factor to fit in bed
            //     // Calculate the scale factor to fit the model on the print bed
            //     double scaleToFitWidth = bedWidth / modelWidth;
            //     double scaleToFitDepth = bedDepth / modelDepth;
            //     double scaleToFit = Math.Min(scaleToFitWidth, scaleToFitDepth);
            //     
            //     // Scale the model to fit the print bed
            //     var scaleTransform = new ScaleTransform3D(scaleToFit, scaleToFit, scaleToFit);
            //     transformGroup.Children.Add(scaleTransform);
            // }
            
            // Apply the transformations to the geometry model
            geomModel.Transform = transformGroup;

            return geomModel;
        }
        catch (Exception err)
        {
            MessageBox.Show(
                $"Error loading STL file: {err.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
                );
        }
        return null;
    }
    
    
    private TranslateTransform3D getCenterGeometryTranslation(GeometryModel3D model)
    {
        // Find the bounds of the model
        // double minX = model.Bounds.X;
        // double minY = model.Bounds.Y;
        // double minZ = model.Bounds.Z;
        //
        // double maxX = model.Bounds.X + model.Bounds.SizeX;
        // double maxY = model.Bounds.Y + model.Bounds.SizeY;
        // double maxZ = model.Bounds.Z + model.Bounds.SizeZ;
        //
        // double modelWidth = maxX - minX;
        // double modelDepth = maxY - minY;
        // double modelHeight = maxZ - minZ;
        //     
        // // Translate model so that it is centered on the bed
        // TranslateTransform3D translateTransform = new TranslateTransform3D(-minX - modelWidth / 2, -minY - modelDepth / 2, -minZ);
        //
        // return translateTransform;
        
        var offsetX = - model.Bounds.X - model.Bounds.SizeX/ 2;
        var offsetY = - model.Bounds.Y - model.Bounds.SizeY / 2;
        var offsetZ = - model.Bounds.Z;
        var translation = new TranslateTransform3D(offsetX, offsetY, offsetZ);
        
        return translation;
    }
    
    private TranslateTransform3D getCenterMeshTranslation(MeshGeometry3D mesh)
    {
        // Find the bounds of the model
        double minX = mesh.Positions.Min(p => p.X);
        double minY = mesh.Positions.Min(p => p.Y);
        double minZ = mesh.Positions.Min(p => p.Z);

        double maxX = mesh.Positions.Max(p => p.X);
        double maxY = mesh.Positions.Max(p => p.Y);
        double maxZ = mesh.Positions.Max(p => p.Z);

        double modelWidth = maxX - minX;
        double modelDepth = maxY - minY;
        double modelHeight = maxZ - minZ;
            
        // Translate model so that it is centered on the bed
        TranslateTransform3D translateTransform = new TranslateTransform3D(-minX - modelWidth / 2, -minY - modelDepth / 2, -minZ);
        
        return translateTransform;
    }
    
    /**
    * Start code from Blackboard to extract the largest model from the group
    */
    public static GeometryModel3D? FindLargestModel(Model3DGroup group) {
        if (group.Children.Count == 1)
            return group.Children[0] as GeometryModel3D;
        int maxCount = int.MinValue;
        GeometryModel3D maxModel = null;
        foreach (GeometryModel3D model in group.Children) {
            int count = ((MeshGeometry3D)model.Geometry).Positions.Count;
            if (maxCount < count) {
                maxCount = count;
                maxModel = model;
            }
        }
        return maxModel;
    }
    
}