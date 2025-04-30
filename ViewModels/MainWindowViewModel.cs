using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Meditrans.Client.Commands;
using Meditrans.Client.Helpers;
using Meditrans.Client.Models;
using Meditrans.Client.Services;
using Meditrans.Client.Views;

namespace Meditrans.Client.ViewModels
{
    public class MainWindowViewModel : BaseViewModel
    {
        public string LoggedUserName => "Hello, User"; // Example

        public ICommand ChangeLanguageCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand SelectionChangedCommand { get; }

        #region Translation

        // Main menu
        public string MainMenuHome => LocalizationService.Instance["Home"];
        public string MainMenuData => LocalizationService.Instance["Data"];
        public string MainMenuSchedules => LocalizationService.Instance["Schedules"];
        public string MainMenuDispatch => LocalizationService.Instance["Dispatch"];
        public string MainMenuReports => LocalizationService.Instance["Reports"];
        public string MainMenuAdmin => LocalizationService.Instance["Admin"];


        public string Settings => LocalizationService.Instance["Settings"];
        public string MenuChangeLanguage => LocalizationService.Instance["MenuChangeLanguage"];
        public string MenuEnglishLanguage => LocalizationService.Instance["MenuEnglishLanguage"];
        public string MenuSpanishLanguage => LocalizationService.Instance["MenuSpanishLanguage"];
        public string MenuLogout => LocalizationService.Instance["MenuLogout"];


        private ObservableCollection<LanguageOption> _languages;
        public ObservableCollection<LanguageOption> Languages
        {
            get => _languages;
            set
            {
                _languages = value;
                OnPropertyChanged();
            }
        }

        private LanguageOption _selectedLanguage;
        public LanguageOption SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                _selectedLanguage = value;
                OnPropertyChanged();
                ChangeLanguage(value.LanguageCode);
            }
        }

        private Visibility _englishCheckVisibility = Visibility.Visible;
        public Visibility EnglishCheckVisibility
        {
            get => _englishCheckVisibility;
            set
            {
                _englishCheckVisibility = value;
                OnPropertyChanged();
            }
        }

        private Visibility _spanishCheckVisibility = Visibility.Collapsed;
        public Visibility SpanishCheckVisibility
        {
            get => _spanishCheckVisibility;
            set
            {
                _spanishCheckVisibility = value;
                OnPropertyChanged();
            }
        }




        #endregion
        public MainWindowViewModel()
        {
            ChangeLanguageCommand = new RelayCommand<string>(ChangeLanguage);
            LogoutCommand = new RelayCommand(Logout);

            Languages = new ObservableCollection<LanguageOption>
            {
                new LanguageOption { LanguageCode = "en", DisplayName = MenuEnglishLanguage, FlagEmoji = "🇺🇸" },
                new LanguageOption { LanguageCode = "es", DisplayName = MenuSpanishLanguage, FlagEmoji = "🇲🇽" },
                new LanguageOption { LanguageCode = "fr", DisplayName = "Français", FlagEmoji = "🇫🇷" },
            };

            // Select saved language
            var currentLang = Properties.Settings.Default.Language ?? "en";
            var match = Languages.FirstOrDefault(x => x.LanguageCode == currentLang) ?? Languages.First();
            match.IsSelected = true;
            SelectedLanguage = match;

            
            if (SelectedLanguage.LanguageCode.Equals("en")) 
            {
                EnglishCheckVisibility = SelectedLanguage.CheckVisibility;
                SpanishCheckVisibility = Visibility.Collapsed;
            }else if (SelectedLanguage.LanguageCode.Equals("es"))
            {
                SpanishCheckVisibility = SelectedLanguage.CheckVisibility;
                EnglishCheckVisibility = System.Windows.Visibility.Collapsed;
            }
        }

        private void ChangeLanguage(string languageCode)
        {
            if (LocalizationService.Instance.CurrentLanguage != languageCode)
            {
                LocalizationService.Instance.LoadLanguage(languageCode);

                foreach (var lang in Languages)
                {
                    lang.IsSelected = lang.LanguageCode == languageCode;
                    if (lang.LanguageCode.Equals("en"))
                    {
                        EnglishCheckVisibility = lang.CheckVisibility;
                    }else if (lang.LanguageCode.Equals("es"))
                    {
                        SpanishCheckVisibility = lang.CheckVisibility;
                    }
                }
                    
            }
            //LocalizationService.Instance.LoadLanguage(languageCode);
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
