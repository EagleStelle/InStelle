using Microsoft.UI.Xaml;

namespace InStelle
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();

            // Navigate to the MainMenuPage when the app starts
            MainFrame.Navigate(typeof(MainPage));
        }
    }
}
