// Meditrans.Client/ViewModels/AddEditGroupViewModel.cs
using Meditrans.Client.Models;
using Meditrans.Client.Commands;
using System.Windows.Input;
using System.ComponentModel; // Necesario para IDataErrorInfo
using System;
using System.Windows.Media; // Para System.Windows.Media.Color

namespace Meditrans.Client.ViewModels
{
    public class AddEditGroupViewModel : BaseViewModel, IDataErrorInfo
    {
        private VehicleGroup _currentGroup;
        public VehicleGroup CurrentGroup
        {
            get => _currentGroup;
            set
            {
                _currentGroup = value;
                OnPropertyChanged(nameof(CurrentGroup));
                // Actualiza las propiedades individuales para la validación y el ColorPicker
                OnPropertyChanged(nameof(Name));
                OnPropertyChanged(nameof(Description));
                OnPropertyChanged(nameof(SelectedColor));
            }
        }

        // Propiedad intermediaria para el ColorPicker
        private Color _selectedColor;
        public Color SelectedColor
        {
            get => _selectedColor;
            set
            {
                _selectedColor = value;
                OnPropertyChanged(nameof(SelectedColor));
                // Actualizar el string en el modelo
                if (CurrentGroup != null)
                {
                    CurrentGroup.Color = value.ToString(); // Convertir Color a string #AARRGGBB
                }
            }
        }

        // Traducciones (asumiendo que tienes un LocalizationService similar)
        public string WindowTitle => CurrentGroup?.Id == 0 ? Services.LocalizationService.Instance["AddGroupTitle"] : Services.LocalizationService.Instance["EditGroupTitle"];
        public string NameLabel => Services.LocalizationService.Instance["GroupNameText"];
        public string DescriptionLabel => Services.LocalizationService.Instance["Description"];
        public string ColorLabel => Services.LocalizationService.Instance["GroupColorText"];
        public string OkButtonText => Services.LocalizationService.Instance["OK"];
        public string CancelButtonText => Services.LocalizationService.Instance["Cancel"];


        public AddEditGroupViewModel(VehicleGroup groupToEdit = null)
        {
            CurrentGroup = groupToEdit ?? new VehicleGroup();
            if (!string.IsNullOrEmpty(CurrentGroup.Color))
            {
                try
                {
                    SelectedColor = (Color)ColorConverter.ConvertFromString(CurrentGroup.Color);
                }
                catch
                {
                    SelectedColor = Colors.Transparent; // o un color por defecto
                }
            }
            else
            {
                SelectedColor = Colors.Gray; // Color inicial por defecto para un nuevo grupo
            }
        }

        // IDataErrorInfo para validación simple
        public string Error => null; // No usado para validación a nivel de objeto

        public string this[string columnName]
        {
            get
            {
                string result = null;
                if (columnName == nameof(Name))
                {
                    if (string.IsNullOrWhiteSpace(CurrentGroup?.Name))
                        result = Services.LocalizationService.Instance["NameIsRequiredError"]; // Añadir esta clave a tu servicio de localización
                }
                // Puedes añadir más validaciones aquí para Description, Color, etc.
                return result;
            }
        }

        // Propiedades para bindear directamente en XAML si es necesario
        public string Name
        {
            get => CurrentGroup?.Name;
            set
            {
                if (CurrentGroup != null)
                {
                    CurrentGroup.Name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        public string Description
        {
            get => CurrentGroup?.Description;
            set
            {
                if (CurrentGroup != null)
                {
                    CurrentGroup.Description = value;
                    OnPropertyChanged(nameof(Description));
                }
            }
        }
    }
}