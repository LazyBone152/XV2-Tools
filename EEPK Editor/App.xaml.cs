using MahApps.Metro.Controls.Dialogs;
using System;
using System.IO;
using System.Windows;
using Xv2CoreLib.Resource.App;

namespace EEPK_Organiser
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
#if !DEBUG
            SaveExceptionLog(e.Exception.ToString());
            MessageBox.Show(String.Format("Something went wrong. \n\nDetails: {0}\n\nA log containing details about the error was saved at \"{1}\".", e.Exception.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
#endif
        }

        public void SaveExceptionLog(string ex)
        {
            try
            {
                File.WriteAllText(SettingsManager.Instance.GetErrorLogPath(), ex);
            }
            catch
            {
            }
        }

    }
}
