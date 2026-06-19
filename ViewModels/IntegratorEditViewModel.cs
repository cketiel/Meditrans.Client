using Meditrans.Client.Models;
using System.Windows;
using System.Windows.Input;
using Meditrans.Client.Commands;

namespace Meditrans.Client.ViewModels
{
    public class IntegratorEditViewModel : BaseViewModel
    {
        public Integrator Integrator { get; set; }
        public bool IsNew { get; }

        private string _apiKeyDisplay;
        public string ApiKeyDisplay
        {
            get => _apiKeyDisplay;
            set => SetProperty(ref _apiKeyDisplay, value);
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand RegenerateKeyCommand { get; }

        public IntegratorEditViewModel(Integrator integrator)
        {
            Integrator = integrator;
            IsNew = integrator.Id == 0;

            ApiKeyDisplay = IsNew ? "Generated automatically on save" : Integrator.ApiKey;

            SaveCommand = new RelayCommandObject(Save);
            CancelCommand = new RelayCommandObject(Cancel);
            RegenerateKeyCommand = new RelayCommandObject(Regenerate, _ => !IsNew);
        }

        private void Regenerate(object obj)
        {
            Integrator.RegenerateApiKey = true;
            ApiKeyDisplay = "Will be regenerated on save...";
        }

        private void Save(object obj)
        {
            if (string.IsNullOrWhiteSpace(Integrator.Name))
            {
                MessageBox.Show("Name is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (obj is Window window) window.DialogResult = true;
        }

        private void Cancel(object obj)
        {
            if (obj is Window window) window.DialogResult = false;
        }
    }
}