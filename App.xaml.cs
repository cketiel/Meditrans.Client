using System.Configuration;
using System.Data;
using System.Windows;
using Meditrans.Client.Views;
using Microsoft.Extensions.Configuration;

namespace Meditrans.Client
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IConfiguration Configuration { get; private set; }
        protected void Application_Startup(object sender, StartupEventArgs e)
        {
            var login = new LoginWindow();
            login.ResizeMode = ResizeMode.NoResize;
            login.WindowState = WindowState.Normal;
            login.Topmost = true;
            login.Show();

            // Load configuration from appsettings.json
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            Configuration = builder.Build();

            //base.OnStartup(e);
        }

        /*protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            var login = new LoginWindow();
            login.Show();
        }*/

    }

}
