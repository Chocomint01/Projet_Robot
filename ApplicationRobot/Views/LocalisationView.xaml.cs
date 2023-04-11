using ApplicationRobot.Models;
using Microsoft.Maps.MapControl.WPF;
using Plugin.BingMap;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        private List<MapPolygon> allPolygons;



        public LocalisationView()
        {
            InitializeComponent();
            polylinePoints = new List<WPFLocation>();
            allPolygons = new List<MapPolygon>();
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
                LoadAndDisplaySelectedPolygon(userId);
            }
        }


        //---------------------------------------------------------
        //-------------------------Les fonction chargée de définir une nouvelle zone----------------------------------
        private void btnDefineArea_Click(object sender, RoutedEventArgs e)
        {
            isFinished = false;
            btnFinish.IsEnabled = true;
            btnDefineArea.IsEnabled = false;
            polygon = new MapPolygon
            {
                Stroke = new SolidColorBrush(Colors.Red),
                Fill = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
                StrokeThickness = 1
            };
            polylinePoints = new List<WPFLocation>();
            if (!NewPolygonLayer.Children.Contains(polygon))
            {
                NewPolygonLayer.Children.Add(polygon);
            }
            allPolygons.Add(polygon);
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
        private void InsertPolygonPointsToDatabase(Guid domaineId)
        {
            using (SqlConnection conn = new SqlConnection("Server=MSI\\LOCAL; Database=MVVMLoginDb; Integrated Security=true"))
            {
                conn.Open();
                string query = "INSERT INTO Zone (DomaineId, PointIndex, Latitude, Longitude) VALUES (@DomaineId, @PointIndex, @Latitude, @Longitude)";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    for (int i = 0; i < polylinePoints.Count; i++)
                    {
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@DomaineId", domaineId);
                        cmd.Parameters.AddWithValue("@PointIndex", i);
                        cmd.Parameters.AddWithValue("@Latitude", polylinePoints[i].Latitude);
                        cmd.Parameters.AddWithValue("@Longitude", polylinePoints[i].Longitude);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        //---------------------------------Fonction chargée de valiser et stocké la nouvelle zone------------------------------------------

        private void btnFinish_Click(object sender, RoutedEventArgs e)
        {
            isFinished = true;
            btnFinish.IsEnabled = false;
            btnDefineArea.IsEnabled = true;

            string domainName = PromptDomainName();
            if (string.IsNullOrEmpty(domainName))
            {
                txtValidationMessage.Text = $"Le nom du domaine n'a pas été fourni.";
                return;
            }

            if (AppSettings.CurrentUser != null)
            {
                Guid userId = Guid.Parse(AppSettings.CurrentUser.Id);
                Guid domaineId = InsertDomaineToDatabase(userId, domainName);
                InsertPolygonPointsToDatabase(domaineId);
                polygon.Tag = domaineId;
                SharedData.PolygonCoordinates = polylinePoints.ToList();
                txtValidationMessage.Text = $"Vous avez validé le domaine \"{domainName}\" avec {polylinePoints.Count} points utilisés.";
                InsertEventToDatabase(userId, "Création de polygone", DateTime.Now, domainName);
            }
            else
            {
                txtValidationMessage.Text = $"Vous n'êtes pas connecté";
            }
        }
        //---------------------------stocker dans l'historique les evenements----------------------------------------------------------
        private void InsertEventToDatabase(Guid userId, string eventName, DateTime eventDate, string domainName)
        {
            using (SqlConnection conn = new SqlConnection("Server=MSI\\LOCAL; Database=MVVMLoginDb; Integrated Security=true"))
            {
                conn.Open();
                string query = "INSERT INTO Historique (UserId, EventName, EventDate, DomainName) VALUES (@UserId, @EventName, @EventDate, @DomainName)";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@EventName", eventName);
                    cmd.Parameters.AddWithValue("@EventDate", eventDate);
                    cmd.Parameters.AddWithValue("@DomainName", domainName);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        private string GetDomainNameFromDatabase(Guid domaineId)
        {
            string domainName = "";
            using (SqlConnection conn = new SqlConnection("Server=MSI\\LOCAL; Database=MVVMLoginDb; Integrated Security=true"))
            {
                conn.Open();
                string query = "SELECT NomDomaine FROM Domaine WHERE Id = @DomaineId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@DomaineId", domaineId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            domainName = reader.GetString(0);
                        }
                    }
                }
            }
            return domainName;
        }


        //-----------------------------fonction charger d'affichage du polygone sur la carte--------------------------------------------------
        private void AddPolygonToMap(Guid domaineId, List<WPFLocation> polylinePoints)
        {
            MapPolygon polygon = new MapPolygon
            {
                Stroke = new SolidColorBrush(Colors.Red),
                Fill = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
                StrokeThickness = 1,
                Locations = new LocationCollection(),
                Visibility = Visibility.Collapsed,
                Tag = domaineId
            };

            foreach (WPFLocation point in polylinePoints)
            {
                polygon.Locations.Add(point);
            }

            NewPolygonLayer.Children.Add(polygon);
            allPolygons.Add(polygon);
        }
        private async void LoadPolygonPointsFromDatabase(Guid userId)
        {
            using (SqlConnection conn = new SqlConnection("Server=MSI\\LOCAL; Database=MVVMLoginDb; Integrated Security=true"))
            {
                await conn.OpenAsync();

                string query = @"SELECT d.Id, z.PointIndex, z.Latitude, z.Longitude, d.IsSelected
                         FROM Domaine d
                         INNER JOIN Zone z ON z.DomaineId = d.Id
                         WHERE d.UserId = @UserId
                         ORDER BY d.Id, z.PointIndex";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        Guid? lastDomaineId = null;
                        List<WPFLocation> locations = new List<WPFLocation>();
                        bool isSelected = false;

                        while (await reader.ReadAsync())
                        {
                            Guid domaineId = reader.GetGuid(0);
                            int pointIndex = reader.GetInt32(1);
                            double latitude = reader.GetDouble(2);
                            double longitude = reader.GetDouble(3);
                            isSelected = reader.GetBoolean(4);

                            if (lastDomaineId != null && domaineId != lastDomaineId)
                            {
                                AddPolygon(locations, (Guid)lastDomaineId, isSelected);
                                locations.Clear();
                            }

                            locations.Add(new WPFLocation(latitude, longitude));
                            lastDomaineId = domaineId;
                        }

                        if (lastDomaineId != null)
                        {
                            AddPolygon(locations, (Guid)lastDomaineId, isSelected);
                        }
                    }
                }
            }
        }
        private void AddPolygon(List<WPFLocation> locations, Guid domaineId, bool isSelected)
        {
            var locationCollection = new LocationCollection();
            foreach (var location in locations)
            {
                locationCollection.Add(location);
            }

            var poly = new MapPolygon
            {
                Stroke = new SolidColorBrush(Colors.Red),
                Fill = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
                StrokeThickness = 1,
                Locations = locationCollection,
                Tag = domaineId,
                Visibility = isSelected ? Visibility.Visible : Visibility.Collapsed
            };

            allPolygons.Add(poly);
            NewPolygonLayer.Children.Add(poly);
        }



        //------------------------------------bouton de suppression du polygone ----------------------------------------------------
        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (AppSettings.CurrentUser != null)
            {
                MessageBoxResult result = MessageBox.Show("Êtes-vous sûr de vouloir supprimer tous les domaines ?", "Confirmer la suppression", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    Guid userId = Guid.Parse(AppSettings.CurrentUser.Id);
                    List<Guid> allDomaineIds = GetAllDomaineIdsFromDatabase(userId);

                    foreach (Guid domaineId in allDomaineIds)
                    {
                        string domainName = GetDomainNameFromDatabase(domaineId); // Ajoutez cette ligne
                        DeletePolygonPointsFromDatabase(domaineId);
                        DeleteDomaineFromDatabase(domaineId);
                        InsertEventToDatabase(userId, "Suppression de polygone", DateTime.Now, domainName);
                    }

                    RemovePolygonFromMap();
                    ClearPolygon();
                    txtValidationMessage.Text = "Tous les domaines ont été supprimés.";
                    SharedData.OnPolygonDeleted();
                }
            }
            else
            {
                txtValidationMessage.Text = "Vous n'êtes pas connecté.";
            }
        }

        private void ClearPolygon()
        {
            polylinePoints.Clear();
            NewPolygonLayer.Children.Remove(polygon);
        }
        private void DeleteDomaineFromDatabase(Guid domaineId)
        {
            using (SqlConnection conn = new SqlConnection("Server=MSI\\LOCAL; Database=MVVMLoginDb; Integrated Security=true"))
            {
                conn.Open();
                string query = "DELETE FROM Domaine WHERE Id = @DomaineId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@DomaineId", domaineId);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        private void DeletePolygonPointsFromDatabase(Guid domaineId)
        {
            using (SqlConnection conn = new SqlConnection("Server=MSI\\LOCAL; Database=MVVMLoginDb; Integrated Security=true"))
            {
                conn.Open();
                string query = "DELETE FROM Zone WHERE DomaineId = @DomaineId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@DomaineId", domaineId);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        private void RemovePolygonFromMap()
        {
            NewPolygonLayer.Children.Remove(polygon);
        }
        //-------------------------------------fonction de gets information d'un polygone et leurs relation-----------------------------------
        private List<Guid> GetAllDomaineIdsFromDatabase(Guid userId)
        {
            List<Guid> allDomaineIds = new List<Guid>();

            using (SqlConnection conn = new SqlConnection("Server=MSI\\LOCAL; Database=MVVMLoginDb; Integrated Security=true"))
            {
                conn.Open();
                string query = "SELECT Id FROM Domaine WHERE UserId = @UserId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            allDomaineIds.Add(reader.GetGuid(0));
                        }
                    }
                }
            }

            return allDomaineIds;
        }
        private async Task<List<WPFLocation>> GetDomaineLocations(Guid domaineId)
        {
            List<WPFLocation> locations = new List<WPFLocation>();

            using (SqlConnection conn = new SqlConnection("Server=MSI\\LOCAL; Database=MVVMLoginDb; Integrated Security=true"))
            {
                await conn.OpenAsync();

                string query = "SELECT Latitude, Longitude FROM Zone WHERE DomaineId = @DomaineId ORDER BY PointIndex";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@DomaineId", domaineId);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            double latitude = reader.GetDouble(0);
                            double longitude = reader.GetDouble(1);

                            locations.Add(new WPFLocation(latitude, longitude));
                        }
                    }
                }
            }

            return locations;
        }

        //-----------------------------------Les fonctions charger de la selection d'un polygone par l'utilisateur dans le tableau-------------------------------------------

        private async void DomainesWindow_DomaineSelected(object sender, Guid domaineId)
        {
            await CenterMapOnDomaine(domaineId);
            DisplaySelectedDomaine(domaineId);
        }



        private void DisplaySelectedDomaine(Guid domaineId)
        {
            NewPolygonLayer.Children.Clear();

            foreach (var poly in allPolygons)
            {
                if (poly.Tag != null && (Guid)poly.Tag == domaineId)
                {
                    poly.Visibility = Visibility.Visible;
                    NewPolygonLayer.Children.Add(poly);

                    // Afficher le pointIndex à côté de chaque point du polygone
                    for (int i = 0; i < poly.Locations.Count; i++)
                    {
                        var pointLocation = poly.Locations[i];
                        var pointIndexTextBlock = new TextBlock
                        {
                            Text = (i + 1).ToString(), // Ajoutez 1 car l'indexation commence à 0
                            Foreground = new SolidColorBrush(Colors.Black),
                            FontWeight = FontWeights.Bold,
                            FontSize = 14
                        };

                        // Positionner le TextBlock à côté du point
                        MapLayer.SetPosition(pointIndexTextBlock, pointLocation);
                        MapLayer.SetPositionOffset(pointIndexTextBlock, new Point(-10, -10));
                        NewPolygonLayer.Children.Add(pointIndexTextBlock);
                    }
                }
                else
                {
                    poly.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void btn_selectionner_Click(object sender, RoutedEventArgs e)
        {
            if (AppSettings.CurrentUser != null)
            {
                Guid userId = Guid.Parse(AppSettings.CurrentUser.Id);
                DomainesWindow domainesWindow = new DomainesWindow(userId);
                domainesWindow.DomaineSelected += DomainesWindow_DomaineSelected;
                domainesWindow.ShowDialog();
            }
            else
            {
                MessageBox.Show("Vous n'êtes pas connecté.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        //-------------------------------------------Affichage d'information du polygone----------------------------------------------------------------
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

        //----------------------------Enregistrement de la position de la carte ------------------------------------------------------------
        private async void LoadAndDisplaySelectedPolygon(Guid userId)
        {
            using (SqlConnection conn = new SqlConnection("Server=MSI\\LOCAL; Database=MVVMLoginDb; Integrated Security=true"))
            {
                await conn.OpenAsync();

                string query = @"SELECT d.Id
                         FROM Domaine d
                         WHERE d.UserId = @UserId AND d.IsSelected = 1";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            Guid selectedDomaineId = reader.GetGuid(0);
                            DisplaySelectedDomaine(selectedDomaineId);
                            await CenterMapOnDomaine(selectedDomaineId);
                        }
                    }
                }
            }
        }
        private async Task CenterMapOnDomaine(Guid domaineId)
        {
            var locations = await GetDomaineLocations(domaineId);

            if (locations != null && locations.Any())
            {
                var center = new WPFLocation(
                    locations.Average(location => location.Latitude),
                    locations.Average(location => location.Longitude));

                MapWithPolygon.Center = center;

                // Trouver le polygone correspondant au domaineId
                var polygon = allPolygons.FirstOrDefault(p => p.Tag != null && (Guid)p.Tag == domaineId);

                if (polygon != null)
                {
                    double mapWidth = MapWithPolygon.ActualWidth;
                    double mapHeight = MapWithPolygon.ActualHeight;
                    double zoomLevel = CalculateZoomLevel(polygon, mapWidth, mapHeight);

                    MapWithPolygon.ZoomLevel = Math.Max(1, zoomLevel - 1); // Ajoutez un peu de marge en soustrayant 1
                }
            }
        }
        private double CalculateZoomLevel(MapPolygon polygon, double mapWidth, double mapHeight)
        {
            const double WORLD_WIDTH = 256; // La largeur du monde en coordonnées de la carte
            double minLatitude = 90, maxLatitude = -90, minLongitude = 180, maxLongitude = -180;

            foreach (var location in polygon.Locations)
            {
                minLatitude = Math.Min(minLatitude, location.Latitude);
                maxLatitude = Math.Max(maxLatitude, location.Latitude);
                minLongitude = Math.Min(minLongitude, location.Longitude);
                maxLongitude = Math.Max(maxLongitude, location.Longitude);
            }

            double latitudeRange = maxLatitude - minLatitude;
            double longitudeRange = maxLongitude - minLongitude;
            double latitudeZoom = Math.Log(mapHeight * 360 / (latitudeRange * WORLD_WIDTH)) / Math.Log(2);
            double longitudeZoom = Math.Log(mapWidth * 360 / (longitudeRange * WORLD_WIDTH)) / Math.Log(2);

            return Math.Min(latitudeZoom, longitudeZoom);
        }


        //--------------------------------------------------------------------------------------
        //all modification
        private string PromptDomainName()
        {
            string domainName = null;
            Window promptWindow = new Window
            {
                Width = 300,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Title = "Nom du domaine",
                Content = new StackPanel
                {
                    Margin = new Thickness(10)
                }
            };

            TextBox inputTextBox = new TextBox { Margin = new Thickness(0, 10, 0, 10) };
            Button okButton = new Button { Content = "OK", Width = 75, Height = 25, IsDefault = true };
            okButton.Click += (sender, e) => { domainName = inputTextBox.Text; promptWindow.Close(); };

            (promptWindow.Content as StackPanel).Children.Add(new TextBlock { Text = "Entrez le nom du domaine :" });
            (promptWindow.Content as StackPanel).Children.Add(inputTextBox);
            (promptWindow.Content as StackPanel).Children.Add(okButton);

            promptWindow.ShowDialog();

            return domainName;
        }
        private Guid InsertDomaineToDatabase(Guid userId, string domainName)
        {
            Guid domaineId = Guid.NewGuid();

            using (SqlConnection conn = new SqlConnection("Server=MSI\\LOCAL; Database=MVVMLoginDb; Integrated Security=true"))
            {
                conn.Open();
                string query = "INSERT INTO Domaine (Id, NomDomaine, UserId) VALUES (@DomaineId, @NomDomaine, @UserId)";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@DomaineId", domaineId);
                    cmd.Parameters.AddWithValue("@NomDomaine", domainName);
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.ExecuteNonQuery();
                }
            }

            return domaineId;
        }
       

    }
}
