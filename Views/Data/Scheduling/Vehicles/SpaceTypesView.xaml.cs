﻿using System;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Meditrans.Client.ViewModels;

namespace Meditrans.Client.Views.Data.Scheduling.Vehicles
{
    /// <summary>
    /// Interaction logic for SpaceTypesView.xaml
    /// </summary>
    public partial class SpaceTypesView : UserControl
    {
        public SpaceTypesView()
        {
            InitializeComponent();
            DataContext = new SpaceTypesViewModel();
        }
    }
}
