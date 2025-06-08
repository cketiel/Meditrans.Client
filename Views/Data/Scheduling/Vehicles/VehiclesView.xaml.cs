
using System.Windows.Controls;

using Meditrans.Client.ViewModels;

namespace Meditrans.Client.Views.Data.Scheduling.Vehicles
{
    /// <summary>
    /// Lógica de interacción para VehiclesView.xaml
    /// </summary>
    public partial class VehiclesView : UserControl
    {
        public VehiclesView()
        {
            InitializeComponent();
            DataContext = new VehiclesViewModel();
        }
    }
}
