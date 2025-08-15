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
using Meditrans.Client.Models;
using Meditrans.Client.ViewModels;

namespace Meditrans.Client.Views.Admin.Employees
{
    public partial class AddEditUserView : Window
    {
        public AddEditUserView(User user, List<Role> availableRoles)
        {
            InitializeComponent();
            DataContext = new AddEditUserViewModel(user, availableRoles);
        }

        // Event handler to update the ViewModel's Password property
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is AddEditUserViewModel viewModel)
            {
                viewModel.Password = ((PasswordBox)sender).Password;
            }
        }
    }
}
