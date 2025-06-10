using System;
using System.Collections.Generic;
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

namespace Meditrans.Client.Views.Data
{
    /// <summary>
    /// Lógica de interacción para EditCustomerView.xaml
    /// </summary>
    public partial class EditCustomerView : Window
    {
        public EditCustomerView()
        {
            InitializeComponent();
        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Al hacer clic en Guardar, establecemos el resultado del diálogo en 'true'
            // y la ventana se cerrará automáticamente porque IsCancel=true en el otro botón
            // se encarga del caso 'false'.
            this.DialogResult = true;
        }
    }
}
