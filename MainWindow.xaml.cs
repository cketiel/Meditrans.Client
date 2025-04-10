using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Meditrans.Client.Views;

namespace Meditrans.Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            OpenHomeView(null, null); // Load HomeView by default
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Set to full screen
            this.WindowState = WindowState.Maximized;
            //this.WindowStyle = WindowStyle.None;
            //this.Topmost = true; // To keep the window always in front
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.F11)  // Use F11 to toggle mode
            {
                if (this.WindowState == WindowState.Maximized)
                {
                    this.WindowState = WindowState.Normal;
                    this.WindowStyle = WindowStyle.SingleBorderWindow;
                }
                else
                {
                    this.WindowState = WindowState.Maximized;
                    this.WindowStyle = WindowStyle.None;
                }
            }
        }

        private void OpenHomeView(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new HomeView();
        }

        private void OpenAdminView(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new AdminView(); 
        }
    }
}