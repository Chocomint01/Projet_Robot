using ApplicationRobot.Models;
using Microsoft.Maps.MapControl.WPF;
using Plugin.BingMap;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WPFLocation = Microsoft.Maps.MapControl.WPF.Location;


namespace ApplicationRobot.Views
{
    public partial class LocalisationView : UserControl
    {
        private MapPolygon polygon;
        private List<WPFLocation> polylinePoints;
        private bool isFinished;

        public LocalisationView()
        {
            InitializeComponent();
            polylinePoints = new List<WPFLocation>();
            polygon = new MapPolygon
            {
                Stroke = new SolidColorBrush(Colors.Red),
                Fill = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
                StrokeThickness = 1
            };

            isFinished = true;
            btnFinish.IsEnabled = false;

            MapWithPolygon.Focus();

            if (AppSettings.CurrentUser != null)
            {
                Guid userId = Guid.Parse(AppSettings.CurrentUser.Id);
                LoadPolygonPointsFromDatabase(userId);
                UpdatePolygon();
                LoadMapCenterAndZoomFromDatabase(userId);
            }
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
            WPFLocation point = MapWithPolygon.ViewportPointToLocation(mousePosition);
            polylinePoints.Add(point);
            UpdatePolygon();
            UpdatePointsCoordinatesText();
        }

        private void UpdatePolygon()
        {
            polygon.Locations = new LocationCollection();
            foreach (WPFLocation point in polylinePoints)
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
            foreach (WPFLocation point in polylinePoints)
            {
                sb.AppendLine($"Point {pointIndex}: x = {point.Longitude}, y = {point.Latitude}");
                pointIndex++;
            }
            File.WriteAllText(fileName, sb.ToString());
        }
        private void InsertPolygonPointsToDatabase(Guid userId)
        {
            using (SqlConnection conn = new SqlConnection("Server=MSI\\LOCAL; Database=MVVMLoginDb; Integrated Security=true"))
            {
                conn.Open();

                for (int pointIndex = 0; pointIndex < polylinePoints.Count; pointIndex++)
                {
                    WPFLocation point = polylinePoints[pointIndex];
                    string query = "INSERT INTO Zone (UserId, PointIndex, Latitude, Longitude) VALUES (@UserId, @PointIndex, @Latitude, @Longitude)";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        cmd.Parameters.AddWithValue("@PointIndex", pointIndex);
                        cmd.Parameters.AddWithValue("@Latitude", point.Latitude);
                        cmd.Parameters.AddWithValue("@Longitude", point.Longitude);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }


        private void btnFinish_Click(object sender, RoutedEventArgs e)
        {
            isFinished = true;
            btnFinish.IsEnabled = false;
            btnDefineArea.IsEnabled = true;
            txtValidationMessage.Text = $"Vous avez validé le domaine avec {polylinePoints.Count} points utilisés.";

            if (AppSettings.CurrentUser != null)
            {
                Guid userId = Guid.Parse(AppSettings.CurrentUser.Id);
                InsertPolygonPointsToDatabase(userId);
                SaveMapCenterToDatabase(userId);
                SaveMapZoomLevelToDatabase(userId); // Ajout de cette ligne pour enregistrer le niveau de zoom
                SharedData.PolygonCoordinates = polylinePoints.ToList();
            }
            else
            {
                txtValidationMessage.Text = $"Vous n'êtes pas connecté";
            }
        }


        private void ClearPolygon()
        {
            polylinePoints.Clear();
            NewPolygonLayer.Children.Remove(polygon);
        }
        private void LoadPolygonPointsFromDatabase(Guid userId)
        {
            using (SqlConnection conn = new SqlConnection("Server=MSI\\LOCAL; Database=MVVMLoginDb; Integrated Security=true"))
            {
                conn.Open();

                string query = "SELECT PointIndex, Latitude, Longitude FROM Zone WHERE UserId = @UserId ORDER BY PointIndex";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int pointIndex = reader.GetInt32(0);
                            double latitude = reader.GetDouble(1);
                            double longitude = reader.GetDouble(2);
                            WPFLocation point = new WPFLocation(latitude, longitude);
                            polylinePoints.Add(point);
                        }
                    }
                }
            }
        }

        //bouton de suppression du polygone 
        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (AppSettings.CurrentUser != null)
            {
                Guid userId = Guid.Parse(AppSettings.CurrentUser.Id);
                DeletePolygonPointsFromDatabase(userId);
                RemovePolygonFromMap();
                ClearPolygon();
                txtValidationMessage.Text = "Le domaine a été supprimé.";
                SharedData.OnPolygonDeleted();

            }
            else
            {
                txtValidationMessage.Text = "Vous n'êtes pas connecté.";
            }
        }

        private void DeletePolygonPointsFromDatabase(Guid userId)
        {
            using (SqlConnection conn = new SqlConnection("Server=MSI\\LOCAL; Database=MVVMLoginDb; Integrated Security=true"))
            {
                conn.Open();
                string query = "DELETE FROM Zone WHERE UserId = @UserId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        private void RemovePolygonFromMap()
        {
            NewPolygonLayer.Children.Remove(polygon);
        }
        private void UpdatePointsCoordinatesText()
        {
            StringBuilder sb = new StringBuilder();
            int pointIndex = 1;

            foreach (WPFLocation point in polylinePoints)
            {
                sb.AppendLine($"Point {pointIndex}: x = {point.Longitude}, y = {point.Latitude}");
                pointIndex++;
            }

            txtPointsCoordinates.Text = sb.ToString();
        }
        //enregistrer la position de la carte 

        public void LoadMapCenterAndZoomFromDatabase(Guid userId)
        {
            using (SqlConnection conn = new SqlConnection("Server=MSI\\LOCAL; Database=MVVMLoginDb; Integrated Security=true"))
            {
                conn.Open();

                string query = "SELECT CenterLatitude, CenterLongitude, MapZoomLevel FROM [User] WHERE Id = @UserId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            double defaultLatitude = 48.8566;
                            double defaultLongitude = 2.3522;

                            double centerLatitude;
                            if (reader.IsDBNull(0))
                            {
                                centerLatitude = defaultLatitude;
                            }
                            else
                            {
                                centerLatitude = reader.GetDouble(0);
                            }

                            double centerLongitude;
                            if (reader.IsDBNull(1))
                            {
                                centerLongitude = defaultLongitude;
                            }
                            else
                            {
                                centerLongitude = reader.GetDouble(1);
                            }

                            // Vérifiez si la colonne MapZoomLevel est null dans la base de données
                            double zoomLevel;
                            if (reader.IsDBNull(2))
                            {
                                // Attribuez une valeur par défaut si la colonne MapZoomLevel est null
                                zoomLevel = 1.0; // Vous pouvez remplacer cette valeur par celle qui convient le mieux à votre application
                            }
                            else
                            {
                                zoomLevel = reader.GetDouble(2);
                            }

                            MapWithPolygon.Center = new WPFLocation(centerLatitude, centerLongitude);
                            MapWithPolygon.ZoomLevel = zoomLevel;
                        }
                    }
                }
            }
        }


        private void SaveMapCenterToDatabase(Guid userId)
        {
            using (SqlConnection conn = new SqlConnection("Server=MSI\\LOCAL; Database=MVVMLoginDb; Integrated Security=true"))
            {
                conn.Open();
                string query = "UPDATE [User] SET CenterLatitude = @CenterLatitude, CenterLongitude = @CenterLongitude WHERE Id = @UserId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@CenterLatitude", MapWithPolygon.Center.Latitude);
                    cmd.Parameters.AddWithValue("@CenterLongitude", MapWithPolygon.Center.Longitude);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void SaveMapZoomLevelToDatabase(Guid userId)
        {
            using (SqlConnection conn = new SqlConnection("Server=MSI\\LOCAL; Database=MVVMLoginDb; Integrated Security=true"))
            {
                conn.Open();
                string query = "UPDATE [User] SET MapZoomLevel = @ZoomLevel WHERE Id = @UserId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@ZoomLevel", MapWithPolygon.ZoomLevel);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void SetMapCenterFromUser(UserModel user)
        {
            if (user.CenterLatitude.HasValue && user.CenterLongitude.HasValue)
            {
                MapWithPolygon.Center = new WPFLocation(user.CenterLatitude.Value, user.CenterLongitude.Value);
            }
        }

        private void LocalisationView_LostFocus(object sender, RoutedEventArgs e)
        {
            if (AppSettings.CurrentUser != null)
            {
                Guid userId = Guid.Parse(AppSettings.CurrentUser.Id);
                SaveMapCenterToDatabase(userId);
                SaveMapZoomLevelToDatabase(userId);
            }
        }


    }
}
