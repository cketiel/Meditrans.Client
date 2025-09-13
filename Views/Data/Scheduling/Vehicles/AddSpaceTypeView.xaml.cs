﻿using Meditrans.Client.ViewModels;
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

namespace Meditrans.Client.Views.Data.Scheduling.Vehicles
{
    /// <summary>
    /// Lógica de interacción para AddSpaceTypeView.xaml
    /// </summary>
    public partial class AddSpaceTypeView : Window
    {
        public AddSpaceTypeView(AddSpaceTypeViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            viewModel.RequestClose += (s, dialogResult) => {
                this.DialogResult = dialogResult;
                this.Close();
            };
        }
    }
}
