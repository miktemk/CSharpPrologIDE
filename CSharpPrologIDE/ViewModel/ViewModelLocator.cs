using CSharpPrologIDE.Services;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.Practices.ServiceLocation;
using Miktemk.Wpf.Services;

namespace CSharpPrologIDE.ViewModel
{
    public class ViewModelLocator
    {
        static ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

            // see: http://stackoverflow.com/questions/17594058/mvvm-light-there-is-already-a-factory-registered-for-inavigationservice
            if (!ViewModelBase.IsInDesignModeStatic)
            {
                SimpleIoc.Default.Register<FileDialogsServiceWin32, FileDialogsServiceWin32>();
                SimpleIoc.Default.Register<MyAppStateService, MyAppStateService>();

                SimpleIoc.Default.Register<MainViewModel>();
            }
        }

        public MainViewModel Main => ServiceLocator.Current.GetInstance<MainViewModel>();

        public static void Cleanup() { }
    }
}