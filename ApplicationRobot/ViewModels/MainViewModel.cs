using FontAwesome.Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ApplicationRobot.Models;
using ApplicationRobot.Repositories;
using ApplicationRobot.Views;

namespace ApplicationRobot.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        //Fields
        private UserAccountModel _currentUserAccount;
        private ViewModelBase _currentChildView;
        private string _caption;
        private IconChar _icon;

        private IUserRepository userRepository;

        public UserAccountModel CurrentUserAccount
        {
            get
            {
                return _currentUserAccount;
            }

            set
            {
                _currentUserAccount = value;
                OnPropertyChanged(nameof(CurrentUserAccount));
            }
        }

        public ViewModelBase CurrentChildView 
        {
            get
            {
                return _currentChildView;
            }
            set
            {
                _currentChildView = value;
                OnPropertyChanged(nameof(CurrentChildView));
            }
        }
        public string Caption
        {
            get
            {
                return _caption;
            }
            set
            {
                _caption = value;
                OnPropertyChanged(nameof(Caption));
            }
        }
        public IconChar Icon
        {
            get
            {
                return _icon;
            }
            set
            {
                _icon = value;
                OnPropertyChanged(nameof(Icon));
            }
        }

        public ICommand ShowHomeViewCommand { get; }
        public ICommand ShowLocalisationViewCommand { get; }
        public ICommand ShowHistoriqueViewCommand { get; }
        public ICommand ShowInformationViewCommand { get; }
        public ICommand ShowSettingViewCommand { get; }

        public MainViewModel()
        {
            userRepository = new UserRepository();
            CurrentUserAccount = new UserAccountModel();


            ShowHomeViewCommand = new ViewModelCommand(ExecuteShowHomeViewCommand);
            ShowLocalisationViewCommand = new ViewModelCommand(ExecuteShowLocalisationCommand);
            ShowHistoriqueViewCommand = new ViewModelCommand(ExecuteShowHistoriqueViewCommand);
            ShowInformationViewCommand = new ViewModelCommand(ExecuteShowInformationViewCommand);
            ShowSettingViewCommand = new ViewModelCommand(ExecuteShowSettingViewCommand);

            ExecuteShowHomeViewCommand(null);

            LoadCurrentUserData();
        }

        private void ExecuteShowLocalisationCommand(object obj)
        {
            CurrentChildView = new LocalisationViewModel();
            Caption = "Localisation";
            Icon = IconChar.UserGroup;
        }

        private void ExecuteShowHomeViewCommand(object obj)
        {
            CurrentChildView = new HomeViewModel();
            Caption = "Tableau De Bord";
            Icon = IconChar.Home;
        }
        
        private void ExecuteShowHistoriqueViewCommand(object obj)
        {
            CurrentChildView = new HistoriqueViewModel();
            Caption = "Historique des évenements";
            Icon = IconChar.Location;
        }

        private void ExecuteShowInformationViewCommand(object obj)
        {
            CurrentChildView = new InformationViewModel();
            Caption = "Information sur votre robot";
            Icon = IconChar.CircleInfo;
        }

        private void ExecuteShowSettingViewCommand(object obj)
        {
            CurrentChildView = new SettingViewModel();
            Caption = "Parramétrage de votre Robot";
            Icon = IconChar.Gear;
        }

        private void LoadCurrentUserData()
        {
            var user = userRepository.GetByUsername(Thread.CurrentPrincipal.Identity.Name);
            if (user != null)
            {
                CurrentUserAccount.Username = user.Username;
                CurrentUserAccount.DisplayName = $"{user.Name} {user.LastName}";
                CurrentUserAccount.ProfilePicture = null;               
            }
            else
            {
                CurrentUserAccount.DisplayName="Invalid user, not logged in";
                //Hide child views.
            }
        }
    }
}
