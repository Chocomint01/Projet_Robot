using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ApplicationRobot.Models;
using ApplicationRobot.Views;

namespace ApplicationRobot
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // Ajoutez cet événement à votre classe App
        public static event EventHandler ShowLoginView;
        private LoginView _loginView;


        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Ajoutez cet événement au début de la méthode ApplicationStart
            ShowLoginView += OnShowLoginView;

            // Affichez la vue de connexion au démarrage de l'application
            ShowLogin();
        }



        public void ShowLogin()
        {
            ShowLoginView?.Invoke(this, EventArgs.Empty);
        }

        private void OnShowLoginView(object sender, EventArgs e)
        {
            if (_loginView == null || !_loginView.IsLoaded)
            {
                _loginView = new LoginView();
                _loginView.IsVisibleChanged += (s, ev) =>
                {
                    if (_loginView.IsVisible == false && _loginView.IsLoaded)
                    {
                        var mainView = new MainView();
                        mainView.Show();
                        //_loginView.Close(); // Ne fermez pas la fenêtre LoginView ici
                        //_loginView = null;

                        // Chargement de la position de la carte et du niveau de zoom
                    }
                };
            }

            _loginView.Visibility = Visibility.Visible; // Assurez-vous que la vue LoginView est visible
            _loginView.Show();
        }


        // N'oubliez pas de vous désabonner de l'événement lorsque l'application se termine
        protected override void OnExit(ExitEventArgs e)
        {
            ShowLoginView -= OnShowLoginView;
            base.OnExit(e);
        }
    }
}
