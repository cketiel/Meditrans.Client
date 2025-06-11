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
            // By clicking Save, we set the dialog result to 'true'
            // and the window will close automatically because IsCancel=true on the other button takes care of the 'false' case.          
            this.DialogResult = true;
        }
    }
}
