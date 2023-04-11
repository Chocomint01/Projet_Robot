using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Maps.MapControl.WPF;
using ApplicationRobot.Models;


namespace ApplicationRobot.Views
{
    /// <summary>
    /// Logique d'interaction pour HomeView.xaml
    /// </summary>
    public partial class HomeView : UserControl
    {
        public HomeView()
        {
            InitializeComponent();
            myMap.Loaded += MyMap_Loaded;
            if (AppSettings.CurrentUser != null)
            {
                Guid userId = Guid.Parse(AppSettings.CurrentUser.Id);
                LoadMapCenterAndZoomFromDatabase(userId);
            }

            SharedData.PolygonDeleted += SharedData_PolygonDeleted;

        }



        private void Map_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void Map_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void Map_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void Map_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;
        }

        private void MyMap_Loaded(object sender, RoutedEventArgs e)
        {
            if (AppSettings.CurrentUser != null)
            {
                Guid userId = Guid.Parse(AppSettings.CurrentUser.Id);
                LoadMapCenterAndZoomFromDatabase(userId);
                DisplaySelectedPolygon(userId);
            }
        }

        //Restaurer la position du surveillance
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

                            double zoomLevel;
                            if (reader.IsDBNull(2))
                            {
                                zoomLevel = 1.0;
                            }
                            else
                            {
                                zoomLevel = reader.GetDouble(2);
                            }

                            myMap.Center = new Location(centerLatitude, centerLongitude);
                            myMap.ZoomLevel = zoomLevel;
                        }
                    }
                }
            }
        }
        private void DisplaySelectedPolygon(Guid userId)
        {
            using (SqlConnection conn = new SqlConnection("Server=MSI\\LOCAL; Database=MVVMLoginDb; Integrated Security=true"))
            {
                conn.Open();

                // Requête pour récupérer les points du polygone sélectionné (IsSelected = 1) lié à l'utilisateur
                string query = @"SELECT Zone.Latitude, Zone.Longitude
                         FROM Zone
                         INNER JOIN Domaine ON Zone.DomaineId = Domaine.Id
                         WHERE Domaine.UserId = @UserId AND Domaine.IsSelected = 1
                         ORDER BY Zone.PointIndex";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        LocationCollection locationCollection = new LocationCollection();

                        while (reader.Read())
                        {
                            double latitude = reader.GetDouble(0);
                            double longitude = reader.GetDouble(1);

                            locationCollection.Add(new Location(latitude, longitude));
                        }

                        if (locationCollection.Count > 0)
                        {
                            // Centrer la carte et adapter le zoom
                            var bounds = new LocationRect(locationCollection);
                            myMap.SetView(bounds);

                            // Afficher le polygone sélectionné
                            MapPolygon polygon = new MapPolygon
                            {
                                Locations = locationCollection,
                                Fill = new SolidColorBrush(Colors.Transparent),
                                Stroke = new SolidColorBrush(Colors.Red),
                                StrokeThickness = 2
                            };

                            myMap.Children.Clear();
                            myMap.Children.Add(polygon);
                        }
                    }
                }
            }
        }



        private void SharedData_PolygonDeleted(object sender, EventArgs e)
        {
            // Supprime tous les polygones existants de la carte
            myMap.Children.Clear();
        }


    }

}
