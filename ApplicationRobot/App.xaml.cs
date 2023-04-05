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

        protected void ApplicationStart(object sender, StartupEventArgs e)
        {
            // Ajoutez cet événement au début de la méthode ApplicationStart
            ShowLoginView += OnShowLoginView;

            // Affichez la vue de connexion au démarrage de l'application
            ShowLoginView?.Invoke(this, EventArgs.Empty);
        }

        private void OnShowLoginView(object sender, EventArgs e)
        {
            var loginView = new LoginView();
            loginView.Show();
            loginView.IsVisibleChanged += (s, ev) =>
            {
                if (loginView.IsVisible == false && loginView.IsLoaded)
                {
                    var mainView = new MainView();
                    mainView.Show();
                    loginView.Close();

                    // Chargement de la position de la carte et du niveau de zoom
                    if (AppSettings.CurrentUser != null)
                    {
                        var localisationView = mainView.FindName("LocalisationView") as LocalisationView;
                        if (localisationView != null)
                        {
                            localisationView.LoadMapCenterAndZoomFromDatabase(Guid.Parse(AppSettings.CurrentUser.Id));
                        }
                    }
                }
            };
        }

        // N'oubliez pas de vous désabonner de l'événement lorsque l'application se termine
        protected override void OnExit(ExitEventArgs e)
        {
            ShowLoginView -= OnShowLoginView;
            base.OnExit(e);
        }
    }
}
