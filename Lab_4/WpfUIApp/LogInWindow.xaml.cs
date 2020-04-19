using System.Windows;
using Shared.Models;

namespace WpfUIApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        

        private async void LogIn_Click(object sender, RoutedEventArgs e)
        {
            var response = await App.Client.AuthenticateAsync(new AuthenticationCredentials
            {
                UserName = UserNameBox.Text,
                Password = PasswordBox.Password
            });

            if (response.Success)
            {
                var w = new FileTransferWindow();
                MessageBox.Show("Logged", "Info");
                Hide();
                w.Show();
            }
            else
            {
                MessageBox.Show(response.Error, "Info");
            }
        }
    }
}
