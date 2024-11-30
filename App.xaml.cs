using Microsoft.UI.Xaml;

namespace InStelle
{
    public partial class App : Application
    {
        public static MainWindow? MainWindow { get; private set; } // Nullable, ensures proper initialization check

        public App()
        {
            this.InitializeComponent();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            if (MainWindow == null)
            {
                MainWindow = new MainWindow();
                MainWindow.Activate();
            }
        }
    }
}
