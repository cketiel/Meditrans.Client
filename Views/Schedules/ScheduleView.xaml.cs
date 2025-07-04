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
using Meditrans.Client.Services;
using Meditrans.Client.ViewModels;

namespace Meditrans.Client.Views.Schedules
{
    /// <summary>
    /// Lógica de interacción para ScheduleView.xaml
    /// </summary>
    public partial class ScheduleView : UserControl
    {
        public ScheduleView()
        {
            InitializeComponent();
            DataContext = new SchedulesViewModel(new ScheduleService());
        }
    }
}
