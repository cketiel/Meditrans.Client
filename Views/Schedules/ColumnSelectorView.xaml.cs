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
using Meditrans.Client.ViewModels;

namespace Meditrans.Client.Views.Schedules
{
    /// <summary>
    /// Lógica de interacción para ColumnSelectorView.xaml
    /// </summary>
    public partial class ColumnSelectorView : Window
    {
        public ColumnSelectorView()
        {
            InitializeComponent();
            //DataContext = new ScheduleColumnSelectorViewModel();
        }
    }
}
