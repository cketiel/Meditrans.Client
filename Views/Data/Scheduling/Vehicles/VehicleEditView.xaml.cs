﻿using System.Windows;
using Meditrans.Client.ViewModels;

namespace Meditrans.Client.Views.Data.Scheduling.Vehicles
{
    public partial class VehicleEditView : Window
    {
        public VehicleEditView()
        {
            InitializeComponent();

            // It is executed after the DataContext has already been assigned from the main ViewModel.          
            this.DataContextChanged += (sender, e) =>
            {
                if (e.NewValue is VehicleEditViewModel vm)
                {
                    vm.OnSave += () =>
                    {
                        this.DialogResult = true;
                        this.Close();
                    };
                    vm.OnCancel += () =>
                    {
                        this.DialogResult = false;
                        this.Close();
                    };
                }
            };
        }
    }
}