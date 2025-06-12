using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Meditrans.Client.ViewModels;

namespace Meditrans.Client.Views.Data.Scheduling
{
    /// <summary>
    /// Lógica de interacción para VehicleRouteEditorWindow.xaml
    /// </summary>
    public partial class VehicleRouteEditorWindow : Window
    {
        public VehicleRouteEditorWindow()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;

            // Suscribirse a los eventos del ViewModel para cerrar la ventana
            /*DataContextChanged += (s, e) =>
            {
                if (e.NewValue is VehicleRouteEditorViewModel vm)
                {
                    vm.RequestClose += (sender, dialogResult) =>
                    {
                        this.DialogResult = dialogResult;
                        this.Close();
                    };
                }
            };*/
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is VehicleRouteEditorViewModel oldVm)
            {
                oldVm.RequestClose -= OnRequestClose;
            }
            if (e.NewValue is VehicleRouteEditorViewModel newVm)
            {
                newVm.RequestClose += OnRequestClose;
            }
        }

        private void OnRequestClose(object sender, bool? dialogResult)
        {
            this.DialogResult = dialogResult;
            this.Close();
        }
    }

    // Convertidor para cambiar el título de la ventana
    public class IdToTitleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var baseTitle = parameter as string ?? "Editor";
            if (value is int id && id > 0)
            {
                return $"Editar {baseTitle} (ID: {id})";
            }
            return $"Nuevo {baseTitle}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
