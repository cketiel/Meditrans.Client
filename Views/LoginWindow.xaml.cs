using System.Windows;
using Meditrans.Client.Services;
using Meditrans.Client.Models;
using Meditrans.Client.Helpers;
using System.Windows.Media.Animation;
using MaterialDesignThemes.Wpf;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace Meditrans.Client.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            UsernameTextBox.Text = StorageHelper.LoadUsername();
            //this.AllowsTransparency = true;
            //this.WindowStyle = WindowStyle.None;
            //this.Background = Brushes.Transparent;
        }
        
        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var username = UsernameTextBox.Text;
            //var password = PasswordBox.Password;
            //string username = UsernameTextBox.Text;
            string password = PasswordBox.Visibility == Visibility.Visible ? PasswordBox.Password : PasswordVisibleBox.Text;

            // Validation
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                //ValidationText.Visibility = Visibility.Visible;
                MessageBox.Show("Login failed.", "Authentication", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

           // ValidationText.Visibility = Visibility.Collapsed;
            //LoginProgress.Visibility = Visibility.Visible;


            var authService = new AuthService();
            var result = await Task.Run(() => authService.LoginAsync(username, password));
            //var result = await authService.LoginAsync(username, password);

            //LoginProgress.Visibility = Visibility.Collapsed;

            if (result != null && result.IsSuccess)
            {
                SessionManager.Token = result.Token;
                SessionManager.Username = username;
                SessionManager.UserId = result.UserId;
                //SessionManager.Role = result.Role;

                StorageHelper.SaveUsername(username);

                // Run the fade out animation
                //StartFadeOutAnimation();

                var mainWindow = new MainWindow();
                mainWindow.WindowState = WindowState.Maximized;
                mainWindow.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show(result?.Message ?? "Login failed.", "Authentication", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            /*{
                MessageBox.Show("Login failed. Please check your credentials.");
            }*/
        }
        private void FadeInOnLoad(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(500))
                {
                    EasingFunction = new QuadraticEase(),
                    BeginTime = TimeSpan.FromMilliseconds(200)
                };
                element.BeginAnimation(OpacityProperty, fade);
            }
        }

        private void TogglePasswordVisibility_Checked(object sender, RoutedEventArgs e)
        {
            PasswordVisibleBox.Text = PasswordBox.Password;
            PasswordVisibleBox.Visibility = Visibility.Visible;
            PasswordBox.Visibility = Visibility.Collapsed;

            if (TogglePasswordVisibility.Content is PackIcon icon)
            {
                icon.Kind = PackIconKind.EyeOff;
            }
        }

        private void TogglePasswordVisibility_Unchecked(object sender, RoutedEventArgs e)
        {
            PasswordBox.Password = PasswordVisibleBox.Text;
            PasswordBox.Visibility = Visibility.Visible;
            PasswordVisibleBox.Visibility = Visibility.Collapsed;

            if (TogglePasswordVisibility.Content is PackIcon icon)
            {
                icon.Kind = PackIconKind.Eye;
            }
        }

        private void StartFadeOutAnimation()
        {
            var blurEffect = new BlurEffect
            {
                Radius = 0,
                KernelType = KernelType.Gaussian,
                RenderingBias = RenderingBias.Quality
            };
            this.Effect = blurEffect;

            // Crear la animación de desvanecimiento para la ventana de login
            var fadeOutAnimation = new DoubleAnimation
            {
                From = 1, // Comienza totalmente visible
                To = 0,   // Finaliza completamente invisible
                Duration = TimeSpan.FromSeconds(1), // Duración de la animación
                FillBehavior = FillBehavior.Stop
            };

            var blurAnimation = new DoubleAnimation
            {
                From = 0,
                To = 10,  // Cantidad de blur máximo
                Duration = TimeSpan.FromSeconds(0.3),
                AutoReverse = true  // El blur vuelve a 0 al final
            };

            var storyboard = new Storyboard();
            storyboard.Children.Add(fadeOutAnimation);
            storyboard.Children.Add(blurAnimation);

            Storyboard.SetTarget(fadeOutAnimation, this);
            Storyboard.SetTargetProperty(fadeOutAnimation, new PropertyPath(Window.OpacityProperty));

            Storyboard.SetTarget(blurAnimation, this);
            Storyboard.SetTargetProperty(blurAnimation, new PropertyPath("Effect.Radius"));

            // 4. Al completar, mostrar la nueva ventana con efecto similar
            storyboard.Completed += (sender, e) =>
            {
                ShowMainWindowWithEffect();
                this.Close();
            };

            storyboard.Begin();

            // Cuando la animación termine, ejecutar el método FadeOut_Completed
            /*fadeOutAnimation.Completed += FadeOut_Completed;

            // Iniciar la animación en la ventana de login
            this.BeginAnimation(Window.OpacityProperty, fadeOutAnimation);*/
        }

        private void ShowMainWindowWithEffect()
        {
            var mainWindow = new MainWindow
            {
                Opacity = 0,
                WindowState = WindowState.Maximized,
                //AllowsTransparency = true,
                //WindowStyle = WindowStyle.None,
                //Background = Brushes.Transparent,
                Effect = new BlurEffect { Radius = 10 }  // Comienza con blur
            };

            mainWindow.Show();

            // Animación de entrada (fade in + blur out)
            var fadeInAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.5)
            };

            var blurOutAnimation = new DoubleAnimation
            {
                From = 10,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.4)
            };

            var enterStoryboard = new Storyboard();
            enterStoryboard.Children.Add(fadeInAnimation);
            enterStoryboard.Children.Add(blurOutAnimation);

            Storyboard.SetTarget(fadeInAnimation, mainWindow);
            Storyboard.SetTargetProperty(fadeInAnimation, new PropertyPath(Window.OpacityProperty));

            Storyboard.SetTarget(blurOutAnimation, mainWindow);
            Storyboard.SetTargetProperty(blurOutAnimation, new PropertyPath("Effect.Radius"));

            enterStoryboard.Begin();
        }

        // Este evento se dispara cuando la animación de desvanecimiento termina
        private void FadeOut_Completed(object sender, EventArgs e)
        {
            // Create the MainWindow window
            /*var mainWindow = new MainWindow
            {
                Opacity = 0,
                WindowState = WindowState.Maximized,
                //AllowsTransparency = true,
                //WindowStyle = WindowStyle.None,
                //Background = Brushes.Transparent
            };*/
            var mainWindow = new MainWindow();
            mainWindow.Opacity = 0; // Initially invisible
            mainWindow.WindowState = WindowState.Maximized;
            mainWindow.Show();

            // Create the fading animation for MainWindow
            var fadeInAnimation = new DoubleAnimation
            {
                From = 0, // Starts invisible
                To = 1,   // It becomes completely visible
                Duration = TimeSpan.FromSeconds(1)//, // Animation duration
                //FillBehavior = FillBehavior.Stop
            };

            // Start fading animation on MainWindow window
            mainWindow.BeginAnimation(Window.OpacityProperty, fadeInAnimation);
            
            // Close the login window
            this.Close();

        }

        #region Animation
        private void ZoomInAnimation()
        {
            var scaleTransform = new ScaleTransform();
            this.RenderTransform = scaleTransform;

            var zoomInAnimation = new DoubleAnimation
            {
                From = 0,  // Empieza muy pequeña
                To = 1,    // Finaliza con el tamaño original
                Duration = TimeSpan.FromSeconds(1)
            };

            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, zoomInAnimation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, zoomInAnimation);
        }

        private void BounceInAnimation()
        {
            var bounceAnimation = new DoubleAnimation
            {
                From = this.Top + 50,  // Empieza un poco más abajo
                To = this.Top,         // Finaliza en la posición original
                Duration = TimeSpan.FromSeconds(0.5),
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(2)  // Rebote
            };

            this.BeginAnimation(Window.TopProperty, bounceAnimation);
        }

        private void SpinAnimation()
        {
            var rotateTransform = new RotateTransform();
            this.RenderTransform = rotateTransform;

            var spinAnimation = new DoubleAnimation
            {
                From = 0,       // Empieza sin rotación
                To = 360,       // Gira completamente
                Duration = TimeSpan.FromSeconds(1)
            };

            rotateTransform.BeginAnimation(RotateTransform.AngleProperty, spinAnimation);
        }

        private void FadeAndSlideInAnimation()
        {
            var slideInAnimation = new DoubleAnimation
            {
                From = this.Width,  // Desplazamiento desde la derecha
                To = 0,             // Hasta la posición original
                Duration = TimeSpan.FromSeconds(1)
            };

            var fadeInAnimation = new DoubleAnimation
            {
                From = 0,           // Comienza invisible
                To = 1,             // Finaliza visible
                Duration = TimeSpan.FromSeconds(1)
            };

            // Animación de desplazamiento
            this.BeginAnimation(Window.LeftProperty, slideInAnimation);
            // Animación de opacidad
            this.BeginAnimation(Window.OpacityProperty, fadeInAnimation);
        }

        private void SlideInAnimation()
        {
            // Establecemos la posición inicial fuera de la pantalla
            var slideInAnimation = new DoubleAnimation
            {
                From = this.Width,  // Empieza desde la derecha
                To = 0,             // Finaliza en su posición original
                Duration = TimeSpan.FromSeconds(1)
            };

            // Animamos la propiedad Left (Horizontal) de la ventana
            this.BeginAnimation(Window.LeftProperty, slideInAnimation);
        }
        #endregion

    }
}
