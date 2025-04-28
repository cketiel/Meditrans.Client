using System.Windows;
using System.Windows.Input;
using Meditrans.Client.Commands;
using Meditrans.Client.Helpers;
using Meditrans.Client.Services;
using Meditrans.Client.Views;

namespace Meditrans.Client.ViewModels
{
    public class MainWindowViewModel : BaseViewModel
    {
        public string LoggedUserName => "Hello, User"; // Example

        public ICommand ChangeLanguageCommand { get; }
        public ICommand LogoutCommand { get; }

        public string MenuLogout => LocalizationService.Instance["Logout"];
        public string MenuChangeLanguage => LocalizationService.Instance["ChangeLanguage"];
        public MainWindowViewModel()
        {
            ChangeLanguageCommand = new RelayCommand<string>(ChangeLanguage);
            LogoutCommand = new RelayCommand(Logout);
        }

        private void ChangeLanguage(string languageCode)
        {
            LocalizationService.Instance.LoadLanguage(languageCode);
        }

        private void Logout()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var loginWindow = new LoginWindow();
                loginWindow.Show();

                foreach (Window window in Application.Current.Windows)
                {
                    if (window is MainWindow)
                    {
                        window.Close();
                        break;
                    }
                }
            });
        }
    }
}
