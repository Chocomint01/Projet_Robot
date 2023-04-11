using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Globalization;
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

namespace ApplicationRobot.Views
{
    /// <summary>
    /// Logique d'interaction pour HistoriqueView.xaml
    /// </summary>
    public partial class HistoriqueView : UserControl
    {
        private List<EventRecord> eventsList = new List<EventRecord>();
        public ObservableCollection<EventRecord> EventRecords { get; set; }

        public HistoriqueView() : this(Guid.Empty)
        {

        }
        public HistoriqueView(Guid userId)
        {
            InitializeComponent();
            EventRecords = new ObservableCollection<EventRecord>();
            DGridCustomer.ItemsSource = EventRecords;
            LoadEvents();
        }
        private void LoadEvents()
        {
            using (SqlConnection conn = new SqlConnection("Server=MSI\\LOCAL; Database=MVVMLoginDb; Integrated Security=true"))
            {
                conn.Open();

                string query = "SELECT H.Id, H.EventName, H.EventDate, H.DomainName FROM Historique H";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        List<EventRecord> tempEventRecords = new List<EventRecord>();

                        while (reader.Read())
                        {
                            tempEventRecords.Add(new EventRecord
                            {
                                ID = reader.GetGuid(0),
                                Name = reader.GetString(1),
                                Date = reader.GetDateTime(2).ToString("d", CultureInfo.CurrentCulture),
                                Time = reader.GetDateTime(2).ToString("t", CultureInfo.CurrentCulture),
                                Domain = reader.GetString(3),
                            });
                        }

                        // Trier les événements par date et heure décroissantes
                        var sortedEventRecords = tempEventRecords.OrderByDescending(er => er.Date).ThenByDescending(er => er.Time);

                        // Ajouter les événements triés à la collection observable
                        foreach (var eventRecord in sortedEventRecords)
                        {
                            EventRecords.Add(eventRecord);
                        }
                    }
                }
            }
        }
        //charger de supprimer un evenement 
        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var eventRecord = (EventRecord)button.DataContext;

            MessageBoxResult result = MessageBox.Show("Voulez-vous supprimer cet événement ?", "Confirmation de suppression", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                DeleteEvent(eventRecord.ID);
                EventRecords.Remove(eventRecord);
            }
        }

        private void DeleteEvent(Guid id)
        {
            using (SqlConnection conn = new SqlConnection("Server=MSI\\LOCAL; Database=MVVMLoginDb; Integrated Security=true"))
            {
                conn.Open();

                string query = "DELETE FROM Historique WHERE Id = @Id";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        //fonction charger pour la recherche 
        private void TBoxSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = TBoxSearch.Text;
            FilterEvents(searchText);
        }

        private void FilterEvents(string searchText)
        {
            if (string.IsNullOrEmpty(searchText))
            {
                DGridCustomer.ItemsSource = EventRecords;
            }
            else
            {
                var filteredEvents = EventRecords.Where(er => er.Domain.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0);
                DGridCustomer.ItemsSource = filteredEvents;
            }
        }
        //ajoute d'un evenement par l'utilisateur :
        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
        }







    }

    public class EventRecord
    {
        public Guid ID { get; set; }
        public string Name { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
        public string Domain { get; set; }
        public string Operation { get; set; }
    }

}
