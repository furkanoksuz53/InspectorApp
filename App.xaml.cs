using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using System.Windows;

namespace InspectorApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static App()
        {
            var splashScreenViewModel = new DXSplashScreenViewModel() { Title = "InspectorApp" };
            SplashScreenManager.Create(() => new SplashScreen1(), splashScreenViewModel).ShowOnStartup();
        }
    }
}
