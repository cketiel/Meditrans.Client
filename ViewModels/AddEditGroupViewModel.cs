// Meditrans.Client/ViewModels/AddEditGroupViewModel.cs
using Meditrans.Client.Models;
using Meditrans.Client.Commands;
using System.Windows.Input;
using System.ComponentModel;
using System;
using System.Windows.Media;
using System.Diagnostics; // Para Debug.WriteLine

namespace Meditrans.Client.ViewModels
{
    public class AddEditGroupViewModel : BaseViewModel, IDataErrorInfo
    {
        private VehicleGroup _currentGroup;
        public VehicleGroup CurrentGroup
        {
            get => _currentGroup;
            private set // Es mejor que solo se establezca en el constructor
            {
                _currentGroup = value;
                // Notificar cambios en propiedades dependientes si es necesario,
                // como Name, Description, y la representación string del color.
                OnPropertyChanged(nameof(Name));
                OnPropertyChanged(nameof(Description));
                OnPropertyChanged(nameof(CurrentGroupColorString)); // Para el TextBlock de depuración
            }
        }

        private Color _selectedMediaColor;
        public Color SelectedMediaColor // Renombrado para claridad (es Media.Color)
        {
            get => _selectedMediaColor;
            set
            {
                if (_selectedMediaColor != value)
                {
                    _selectedMediaColor = value;
                    OnPropertyChanged(nameof(SelectedMediaColor)); // Notifica al ColorPicker

                    // Actualizar la propiedad string en el modelo CurrentGroup
                    if (CurrentGroup != null)
                    {
                        string newColorString = _selectedMediaColor.ToString(); // e.g., #AARRGGBB

                        // Solo actualiza CurrentGroup.Color si realmente ha cambiado
                        // para evitar notificaciones innecesarias si el string ya era el mismo.
                        if (CurrentGroup.Color != newColorString)
                        {
                            CurrentGroup.Color = newColorString;
                            Debug.WriteLine($"ColorPicker changed. New MediaColor: {_selectedMediaColor}, Updated CurrentGroup.Color: {CurrentGroup.Color}");
                            // Notificar que la propiedad string ha cambiado, para el TextBlock de depuración
                            OnPropertyChanged(nameof(CurrentGroupColorString));
                        }
                    }
                }
            }
        }

        // Propiedad para mostrar el string del color actual del CurrentGroup en el popup (para depuración)
        public string CurrentGroupColorString => CurrentGroup?.Color;


        // Traducciones...
        public string WindowTitle => (CurrentGroup?.Id ?? 0) == 0 ? Services.LocalizationService.Instance["AddGroupTitle"] : Services.LocalizationService.Instance["EditGroupTitle"];
        public string NameLabel => Services.LocalizationService.Instance["GroupNameText"];
        public string DescriptionLabel => Services.LocalizationService.Instance["Description"];
        public string ColorLabel => Services.LocalizationService.Instance["GroupColorText"];
        public string OkButtonText => Services.LocalizationService.Instance["OK"];
        public string CancelButtonText => Services.LocalizationService.Instance["Cancel"];

        public AddEditGroupViewModel(VehicleGroup groupToEdit = null)
        {
            CurrentGroup = groupToEdit ?? new VehicleGroup(); // Se establece CurrentGroup primero

            // Ahora inicializa SelectedMediaColor BASADO en CurrentGroup.Color
            // o establece un valor por defecto si CurrentGroup.Color es nulo/inválido
            // Y ASEGÚRATE de que CurrentGroup.Color también refleje ese defecto.
            InitializeColorProperties();
        }

        private void InitializeColorProperties()
        {
            Color initialMediaColor;
            if (CurrentGroup != null && !string.IsNullOrEmpty(CurrentGroup.Color))
            {
                try
                {
                    initialMediaColor = (Color)ColorConverter.ConvertFromString(CurrentGroup.Color);
                    Debug.WriteLine($"Initializing. CurrentGroup.Color: '{CurrentGroup.Color}', Parsed to MediaColor: {initialMediaColor}");
                }
                catch (FormatException ex)
                {
                    Debug.WriteLine($"Error converting initial CurrentGroup.Color '{CurrentGroup.Color}' to MediaColor: {ex.Message}. Defaulting to Gray.");
                    initialMediaColor = Colors.Gray;
                    CurrentGroup.Color = initialMediaColor.ToString(); // Corregir CurrentGroup.Color si era inválido
                }
            }
            else // Nuevo grupo o color no establecido
            {
                initialMediaColor = Colors.Gray; // Color inicial por defecto para un nuevo grupo
                if (CurrentGroup != null)
                {
                    CurrentGroup.Color = initialMediaColor.ToString(); // Asegurar que el nuevo grupo tenga este color string
                }
                Debug.WriteLine($"Initializing. CurrentGroup.Color was null/empty. Defaulted to MediaColor: {initialMediaColor}, CurrentGroup.Color set to: {CurrentGroup?.Color}");
            }

            // Establece la variable de respaldo directamente para evitar que el setter se ejecute AHORA y
            // potencialmente sobreescriba CurrentGroup.Color si la lógica del setter fuera diferente.
            _selectedMediaColor = initialMediaColor;

            // Notifica a la UI para que el ColorPicker muestre este color inicial.
            OnPropertyChanged(nameof(SelectedMediaColor));
            // Y también notifica al TextBlock de depuración.
            OnPropertyChanged(nameof(CurrentGroupColorString));
        }


        // IDataErrorInfo...
        public string Error => null;
        public string this[string columnName]
        {
            get
            {
                string result = null;
                if (columnName == nameof(Name))
                {
                    if (string.IsNullOrWhiteSpace(CurrentGroup?.Name))
                        result = Services.LocalizationService.Instance["NameIsRequiredError"];
                }
                return result;
            }
        }

        // Propiedades Name y Description (se actualizan directamente en CurrentGroup)
        public string Name
        {
            get => CurrentGroup?.Name;
            set
            {
                if (CurrentGroup != null && CurrentGroup.Name != value)
                {
                    CurrentGroup.Name = value;
                    OnPropertyChanged(nameof(Name)); // Necesario para la validación y actualización de la UI
                }
            }
        }

        public string Description
        {
            get => CurrentGroup?.Description;
            set
            {
                if (CurrentGroup != null && CurrentGroup.Description != value)
                {
                    CurrentGroup.Description = value;
                    OnPropertyChanged(nameof(Description));
                }
            }
        }
    }
}