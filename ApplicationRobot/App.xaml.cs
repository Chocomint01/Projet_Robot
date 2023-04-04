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
        protected void ApplicationStart(object sender, StartupEventArgs e)
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
    }
}
