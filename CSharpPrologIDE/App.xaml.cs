using System.Windows;
using CSharpPrologIDE.Code;
using GalaSoft.MvvmLight.Threading;

namespace CSharpPrologIDE
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static App()
        {
            DispatcherHelper.Initialize();
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args.Length > 0)
                Current.Resources.Add(Constants.Resources.Arg1Key, e.Args[0]);
        }
    }
}
