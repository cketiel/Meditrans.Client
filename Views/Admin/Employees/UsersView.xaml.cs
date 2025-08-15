﻿
using System.Windows.Controls;


namespace Meditrans.Client.Views.Admin.Employees
{
    /// <summary>
    /// Lógica de interacción para UsersView.xaml
    /// </summary>
    public partial class UsersView : UserControl
    {
        public UsersView()
        {
            InitializeComponent();
            DataContext = new ViewModels.UsersViewModel();
        }
    }
}
