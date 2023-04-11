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
using System.Windows.Shapes;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;
using BCrypt.Net;

namespace ApplicationRobot.Views
{
    /// <summary>
    /// Logique d'interaction pour RegisterWindow.xaml
    /// </summary>
    public partial class RegisterWindow : Window
    {
        public RegisterWindow()
        {
            InitializeComponent();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private async void btnIsc_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUser1.Text) ||
                string.IsNullOrWhiteSpace(txtPass1.Text) ||
                string.IsNullOrWhiteSpace(txtname.Text) ||
                string.IsNullOrWhiteSpace(txtprenom.Text))
            {
                ErrorMessage.Visibility = Visibility.Visible;
                ErrorMessage.Text = "Tous les champs doivent être remplis";
                return;
            }

            if (txtPass1.Text.Length < 5)
            {
                ErrorMessage.Visibility = Visibility.Visible;
                ErrorMessage.Text = "Le mot de passe doit comporter\nau moins 5 caractères";
                return;
            }

            using (SqlConnection con = new SqlConnection("Server=MSI\\LOCAL; Database=MVVMLoginDb; Integrated Security=true"))
            {
                con.Open();

                // Vérifier si le nom d'utilisateur est déjà utilisé
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM [User] WHERE Username = @Username", con))
                {
                    cmd.Parameters.AddWithValue("@Username", txtUser1.Text);
                    int count = (int)cmd.ExecuteScalar();

                    if (count > 0)
                    {
                        ErrorMessage.Visibility = Visibility.Visible;
                        ErrorMessage.Text = "Le nom d'utilisateur est déjà utilisé";
                        return;
                    }
                }

                // Hasher le mot de passe avant de l'insérer dans la base de données
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(txtPass1.Text);

                using (SqlCommand cmd = new SqlCommand("INSERT INTO [User] (Username, Password, Name, LastName) VALUES (@Username, @Password, @Name, @LastName)", con))
                {
                    cmd.Parameters.AddWithValue("@Username", txtUser1.Text);
                    cmd.Parameters.AddWithValue("@Password", hashedPassword);
                    cmd.Parameters.AddWithValue("@Name", txtname.Text);
                    cmd.Parameters.AddWithValue("@LastName", txtprenom.Text);
                    cmd.ExecuteNonQuery();
                }
            }

            SuccessMessage.Visibility = Visibility.Visible;
            SuccessMessage.Text = "Inscription réussie !";

            await Task.Delay(3000); // Attendre 3 secondes

            this.Close();
        }
        private void txtname_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
