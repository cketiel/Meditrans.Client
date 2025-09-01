using System.Windows.Controls;

using Meditrans.Client.Services;
using Meditrans.Client.ViewModels;

namespace Meditrans.Client.Views.Admin.Billing
{
    /// <summary>
    /// Lógica de interacción para BillingItemsView.xaml
    /// </summary>
    public partial class BillingItemsView : UserControl
    {
        public BillingItemsView()
        {
            InitializeComponent();

            var billingItemService = new BillingItemService();
            var unitService = new UnitService(); 
            var viewModel = new BillingItemsViewModel(billingItemService, unitService);
            DataContext = viewModel;

            // Load data when the control is ready to display
            Loaded += async (sender, e) => {
                if (viewModel.LoadBillingItemsCommand.CanExecute(null))
                {
                    await viewModel.LoadBillingItemsCommand.ExecuteAsync(null);
                }
            };
        }
    }
}
