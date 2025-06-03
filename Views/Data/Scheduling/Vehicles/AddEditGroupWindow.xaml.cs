// Meditrans.Client/Views/Data/Scheduling/Vehicles/AddEditGroupWindow.xaml.cs
using Meditrans.Client.ViewModels;
using System.Windows;
using System.Linq;
using System.Windows.Input;
using System.Windows.Controls; // Para validar

namespace Meditrans.Client.Views.Data.Scheduling.Vehicles
{
    public partial class AddEditGroupWindow : Window
    {
        public AddEditGroupWindow(AddEditGroupViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            // Forzar actualización de bindings por si el foco está en un TextBox
            // y el usuario presiona Enter (IsDefault="True")
            var focusedElement = FocusManager.GetFocusedElement(this);
            if (focusedElement is FrameworkElement fe)
            {
                var binding = fe.GetBindingExpression(TextBox.TextProperty); // O la propiedad relevante
                binding?.UpdateSource();
            }

            var vm = DataContext as AddEditGroupViewModel;
            if (vm != null)
            {
                // Comprobar errores de validación (IDataErrorInfo)
                // Simple check for Name, more complex validation could be done
                if (string.IsNullOrWhiteSpace(vm.CurrentGroup.Name))
                {
                    // La validación de IDataErrorInfo debería mostrarse en el TextBox
                    // Podemos mostrar un mensaje adicional si es necesario
                    MessageBox.Show(Services.LocalizationService.Instance["NameIsRequiredError"],
                                    Services.LocalizationService.Instance["ValidationErrorTitle"], // Añadir esta clave
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}