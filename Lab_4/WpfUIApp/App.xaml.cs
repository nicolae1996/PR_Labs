using System.Windows;
using System.Windows.Navigation;
using Client.Helpers;

namespace WpfUIApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Client
        /// </summary>
        private static readonly AsynchronousClient _singleton;
        public static AsynchronousClient Client = _singleton ??= new AsynchronousClient();


        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            await Client.StartClientAsync();
        }
    }
}
