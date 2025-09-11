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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Meditrans.Client.Services;
using Meditrans.Client.ViewModels;
using Meditrans.Client.Models; 
using GMap.NET;

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
            this.DataContextChanged += ScheduleView_DataContextChanged;
          
            this.Unloaded += ScheduleView_Unloaded;

            try
            {              
                MapView.MapProvider = GMap.NET.MapProviders.GoogleMapProvider.Instance;
                GMap.NET.GMaps.Instance.Mode = GMap.NET.AccessMode.ServerAndCache;
                MapView.DragButton = MouseButton.Left;
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error initializing GMap.NET: " + ex.Message);
            }

            var viewModel = new SchedulesViewModel(new ScheduleService());
            SubscribeToViewModelEvents(viewModel); // Subscribe
            DataContext = viewModel;

           // DataContext = new SchedulesViewModel(new ScheduleService());
        }

        private void ScheduleView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Unsubscribe from old ViewModel if it changes
            if (e.OldValue is SchedulesViewModel oldVm)
            {
                oldVm.ZoomAndCenterRequest -= OnZoomAndCenterRequest;
            }

            // Subscribe to the new ViewModel
            if (e.NewValue is SchedulesViewModel newVm)
            {
                newVm.ZoomAndCenterRequest += OnZoomAndCenterRequest;
            }
        }

        // Alternative subscription method if you always create the VM in the constructor
        private void SubscribeToViewModelEvents(SchedulesViewModel viewModel)
        {
            if (viewModel != null)
            {
                viewModel.ZoomAndCenterRequest += OnZoomAndCenterRequest;
            }
        }

        // The event handler that calls the map method
        private void OnZoomAndCenterRequest(object sender, ZoomAndCenterEventArgs e)
        {
            if (e.BoundingBox != null)
            {
                MapView.SetZoomToFitRect(e.BoundingBox);
            }
        }

        private void ScheduleView_Unloaded(object sender, RoutedEventArgs e)
        {
            // Cleanup to prevent memory leaks
            if (this.DataContext is SchedulesViewModel vm)
            {
                vm.ZoomAndCenterRequest -= OnZoomAndCenterRequest;
            }
            this.DataContextChanged -= ScheduleView_DataContextChanged;
            this.Unloaded -= ScheduleView_Unloaded;
        }

        private void ScheduleMarker_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // We verify that we have all the necessary objects
            if (sender is FrameworkElement element &&
                element.DataContext is DTOs.ScheduleDto clickedSchedule &&
                this.DataContext is ViewModels.SchedulesViewModel viewModel)
            {
                

                if (viewModel.SelectedSchedule == clickedSchedule)
                {
                    // CASE 1: The bookmark that was already selected was clicked.
                    // We deselect it by assigning null.
                    viewModel.SelectedSchedule = null;
                }
                else
                {
                    // CASE 2: A different bookmark was clicked.
                    // We select it as before.
                    viewModel.SelectedSchedule = clickedSchedule;
                }

                // We mark the event as handled to prevent the click from moving the map.
                e.Handled = true;
            }
        }
    }
}
