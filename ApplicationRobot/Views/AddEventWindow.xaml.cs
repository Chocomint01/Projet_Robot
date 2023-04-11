using System;
using System.Windows;
using System.Windows.Controls;

namespace ApplicationRobot.Views
{
    public partial class AddEventWindow : Window
    {
        public string EventName { get; private set; }
        public string Domain { get; private set; }

        public AddEventWindow()
        {
            InitializeComponent();
            LoadDomains();
        }

        private void LoadDomains()
        {
            // Remplacez cette méthode par le chargement des domaines depuis la base de données
            // et ajoutez-les au ComboBox. Ici, j'ajoute des domaines de test.
            DomainComboBox.Items.Add("Domaine 1");
            DomainComboBox.Items.Add("Domaine 2");
            DomainComboBox.Items.Add("Domaine 3");
            DomainComboBox.SelectedIndex = 0;
        }

        private void AddEventButton_Click(object sender, RoutedEventArgs e)
        {
            EventName = EventNameTextBox.Text;
            Domain = (DomainComboBox.SelectedItem as ComboBoxItem).Content.ToString();

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
