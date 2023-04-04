using System;
using System.Collections.Generic;
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
using ApplicationRobot.Events;

namespace ApplicationRobot.Views
{
    /// <summary>
    /// Logique d'interaction pour HistoriqueView.xaml
    /// </summary>
    public partial class HistoriqueView : UserControl
    {
        public HistoriqueView()
        {
            InitializeComponent();

            // Créer une liste d'événements aléatoires
            List<Event> events = Generate(20);

            // Affecter la liste à la source de données du DataGrid
            DGridCustomer.ItemsSource = events;
        }

        private List<Event> Generate(int count)
        {
            List<Event> events = new List<Event>();
            Random random = new Random();

            // Générer 'count' événements aléatoires
            for (int i = 0; i < count; i++)
            {
                events.Add(new Event
                {
                    ID = i + 1,
                    Name = "Event " + (i + 1),
                    Date = DateTime.Now.AddDays(random.Next(-30, 30)).ToString("dd/MM/yyyy"),
                    Time = DateTime.Now.AddHours(random.Next(-12, 12)).ToString("HH:mm"),
                    Domain = "Domaine " + (i % 3 + 1),
                    Operation = "Nettoyage"
                });
            }

            return events;
        }
    }

    public class Event
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
        public string Domain { get; set; }
        public string Operation { get; set; }
    }
}
