using ApplicationRobot.Models;
using System;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;



namespace ApplicationRobot.Views
{
    public partial class DomainesWindow : Window
    {
        private Guid _userId;
        private Guid? _lastSelectedDomaineId;
        public event EventHandler<Guid> DomaineSelected;


        public DomainesWindow(Guid userId)
        {
            _userId = userId;
            InitializeComponent();
            LoadDomainesData();
        }
        private void btnQuitter_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void btnLocaliser_Click(object sender, RoutedEventArgs e)
        {
            if (DomainesDataGrid.SelectedItem != null)
            {
                DomaineData selectedDomaine = (DomaineData)DomainesDataGrid.SelectedItem;

                // Mettre à jour l'état IsVisible pour tous les éléments dans DomainesDataGrid.ItemsSource
                ObservableCollection<DomaineData> domaines = (ObservableCollection<DomaineData>)DomainesDataGrid.ItemsSource;
                foreach (DomaineData domaine in domaines)
                {
                    domaine.IsVisible = (domaine.Id == selectedDomaine.Id);
                }

                _lastSelectedDomaineId = selectedDomaine.Id;
                AppSettings.LastSelectedDomaineId = selectedDomaine.Id; // Enregistrez le dernier DomaineId sélectionné dans AppSettings.
                DomaineSelected?.Invoke(this, selectedDomaine.Id);

                // Actualiser les éléments de DomainesDataGrid.
                DomainesDataGrid.ItemsSource = null;
                DomainesDataGrid.ItemsSource = domaines;

                // Mettre à jour la colonne IsSelected dans la base de données
                using (SqlConnection conn = new SqlConnection("Server=MSI\\LOCAL; Database=MVVMLoginDb; Integrated Security=true"))
                {
                    conn.Open();

                    // Mettre la colonne IsSelected de tous les domaines à 0
                    string updateAllToUnselectedQuery = "UPDATE Domaine SET IsSelected = 0 WHERE UserId = @UserId";
                    using (SqlCommand cmd = new SqlCommand(updateAllToUnselectedQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", _userId);
                        cmd.ExecuteNonQuery();
                    }

                    // Mettre la colonne IsSelected du domaine sélectionné à 1
                    string updateSelectedDomaineQuery = "UPDATE Domaine SET IsSelected = 1 WHERE Id = @DomaineId";
                    using (SqlCommand cmd = new SqlCommand(updateSelectedDomaineQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@DomaineId", selectedDomaine.Id);
                        cmd.ExecuteNonQuery();
                    }
                }
                // Utilisez selectedDomaine.NomDomaine au lieu de selectedDomaine
                InsertEventToDatabase(_userId, "Localisation de domaine", DateTime.Now, selectedDomaine.NomDomaine);
                this.Close();
            }
        }






        private void UpdateIsSelectedInDatabase(Guid domaineId, bool isSelected)
        {
            using (SqlConnection conn = new SqlConnection("Server=MSI\\LOCAL; Database=MVVMLoginDb; Integrated Security=true"))
            {
                conn.Open();
                string query = "UPDATE Domaine SET IsSelected = @IsSelected WHERE Id = @DomaineId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@DomaineId", domaineId);
                    cmd.Parameters.AddWithValue("@IsSelected", isSelected);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        private void LoadDomainesData()
        {
            ObservableCollection<DomaineData> domainesData = new ObservableCollection<DomaineData>();

            using (SqlConnection conn = new SqlConnection("Server=MSI\\LOCAL; Database=MVVMLoginDb; Integrated Security=true"))
            {
                conn.Open();

                string query = "SELECT D.Id, D.NomDomaine, COUNT(Z.PointIndex) AS NombrePoints, D.IsSelected FROM Domaine AS D " +
               "INNER JOIN Zone AS Z ON D.Id = Z.DomaineId " +
               "WHERE D.UserId = @UserId GROUP BY D.Id, D.NomDomaine, D.IsSelected";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", _userId);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Guid id = reader.GetGuid(0);
                            string nomDomaine = reader.GetString(1);
                            int nombrePoints = reader.GetInt32(2);
                            bool isSelected = reader.GetBoolean(3);

                            domainesData.Add(new DomaineData
                            {
                                Id = id,
                                NomDomaine = nomDomaine,
                                NombrePoints = nombrePoints,
                                IsVisible = isSelected, // Utilisez isSelected pour la propriété IsVisible
                                IsSelected = isSelected // Initialisez la propriété IsSelected avec la valeur récupérée de la base de données
                            });
                        }
                    }
                }
            }

            DomainesDataGrid.ItemsSource = domainesData;
        }

        private void btnSupprimer_Click(object sender, RoutedEventArgs e)
        {
            if (DomainesDataGrid.SelectedItem != null)
            {
                DomaineData selectedDomaine = (DomaineData)DomainesDataGrid.SelectedItem;
                Guid selectedDomaineId = selectedDomaine.Id;
                string selectedDomaineName = selectedDomaine.NomDomaine;

                DeleteSelectedDomaine(selectedDomaineId);
                LoadDomainesData();

                // Ajoutez cette ligne après avoir supprimé le domaine sélectionné
                InsertEventToDatabase(_userId, "Suppression de domaine", DateTime.Now, selectedDomaineName);
            }
        }

        private void DeleteSelectedDomaine(Guid domaineId)
        {
            using (SqlConnection conn = new SqlConnection("Server=MSI\\LOCAL; Database=MVVMLoginDb; Integrated Security=true"))
            {
                conn.Open();

                string deleteZoneQuery = "DELETE FROM Zone WHERE DomaineId = @DomaineId";
                using (SqlCommand cmd = new SqlCommand(deleteZoneQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@DomaineId", domaineId);
                    cmd.ExecuteNonQuery();
                }

                string deleteDomaineQuery = "DELETE FROM Domaine WHERE Id = @DomaineId";
                using (SqlCommand cmd = new SqlCommand(deleteDomaineQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@DomaineId", domaineId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void btnSupprimerTout_Click(object sender, RoutedEventArgs e)
        {
            if (DomainesDataGrid.Items.Count == 0)
            {
                MessageBox.Show("Il n'y a aucun domaine à supprimer.", "Supprimer tous les domaines", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            MessageBoxResult result = MessageBox.Show("Êtes-vous sûr de vouloir supprimer tous les domaines ?", "Supprimer tous les domaines", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                DeleteAllDomaines();
                LoadDomainesData();
            }
        }
        private void DeleteAllDomaines()
        {
            using (SqlConnection conn = new SqlConnection("Server=MSI\\LOCAL; Database=MVVMLoginDb; Integrated Security=true"))
            {
                conn.Open();

                string deleteZoneQuery = "DELETE Z FROM Zone AS Z INNER JOIN Domaine AS D ON Z.DomaineId = D.Id WHERE D.UserId = @UserId";
                using (SqlCommand cmd = new SqlCommand(deleteZoneQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", _userId);
                    cmd.ExecuteNonQuery();
                }

                string deleteDomaineQuery = "DELETE FROM Domaine WHERE UserId = @UserId";
                using (SqlCommand cmd = new SqlCommand(deleteDomaineQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", _userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        //enregistrement dans historique

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


    }



    public class DomaineData
    {
        public Guid Id { get; set; }
        public string NomDomaine { get; set; }
        public int NombrePoints { get; set; }
        public bool IsVisible { get; set; }
        public bool IsSelected { get; set; }
    }
    public class IsVisibleToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isSelected = (bool)value;
            return isSelected ? "Afficher" : "Masquer";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


}

