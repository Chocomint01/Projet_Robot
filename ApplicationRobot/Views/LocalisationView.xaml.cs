using Microsoft.Maps.MapControl.WPF;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ApplicationRobot.Views
{
    public partial class LocalisationView : UserControl
    {
        private MapPolygon polygon;
        private List<Location> polylinePoints;
        private bool isFinished;


        public LocalisationView()
        {
            InitializeComponent();

            polylinePoints = new List<Location>();
            polygon = new MapPolygon
            {
                Stroke = new SolidColorBrush(Colors.Red),
                Fill = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
                StrokeThickness = 1
            };

            isFinished = true;
            btnFinish.IsEnabled = false;

            MapWithPolygon.Focus();
        }
        private void btnDefineArea_Click(object sender, RoutedEventArgs e)
        {
            isFinished = false;
            btnFinish.IsEnabled = true;
            btnDefineArea.IsEnabled = false;
        }

        private void MapWithPolygon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!isFinished)
            {
                e.Handled = true;
                AddPointToPolyline(e.GetPosition(MapWithPolygon));
            }
        }

        private void MapWithPolygon_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Z && !isFinished)
            {
                AddPointToPolyline(Mouse.GetPosition(MapWithPolygon));
            }
        }

        private void AddPointToPolyline(Point mousePosition)
        {
            Location point = MapWithPolygon.ViewportPointToLocation(mousePosition);
            polylinePoints.Add(point);
            UpdatePolygon();
            UpdatePointsCoordinatesText();
        }

        private void UpdatePolygon()
        {
            polygon.Locations = new LocationCollection();
            foreach (Location point in polylinePoints)
            {
                polygon.Locations.Add(point);
            }

            if (!NewPolygonLayer.Children.Contains(polygon))
            {
                NewPolygonLayer.Children.Add(polygon);
            }
        }
        private void MapWithPolygon_Loaded(object sender, RoutedEventArgs e)
        {
            MapWithPolygon.Focus();
        }
        private void SaveCoordinatesToFile()
        {
            string fileName = "coordinates.txt";
            StringBuilder sb = new StringBuilder();
            int pointIndex = 1;
            foreach (Location point in polylinePoints)
            {
                sb.AppendLine($"Point {pointIndex}: x = {point.Longitude}, y = {point.Latitude}");
                pointIndex++;
            }
            File.WriteAllText(fileName, sb.ToString());
        }
        private void btnFinish_Click(object sender, RoutedEventArgs e)
        {
            isFinished = true;
            btnFinish.IsEnabled = false;
            btnDefineArea.IsEnabled = true;
            txtValidationMessage.Text = $"Vous avez validé le domaine avec {polylinePoints.Count} points utilisés.";
        }
        private void UpdatePointsCoordinatesText()
        {
            StringBuilder sb = new StringBuilder();
            int pointIndex = 1;

            foreach (Location point in polylinePoints)
            {
                sb.AppendLine($"Point {pointIndex}: x = {point.Longitude}, y = {point.Latitude}");
                pointIndex++;
            }

            txtPointsCoordinates.Text = sb.ToString();
        }
    }
}
