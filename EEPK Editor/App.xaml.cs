using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace EEPK_Organiser
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
#if !DEBUG
            SaveExceptionLog(e.Exception.ToString());
            MessageBox.Show(String.Format("An unhandled exception was raised during execution of the application. \n\nDetails: {0}\n\nA log containing details about the error was saved at \"{1}\".", e.Exception.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
#endif
        }

        public void SaveExceptionLog(string ex)
        {
            try
            {
                File.WriteAllText(GeneralInfo.ERROR_LOG_PATH, ex);
            }
            catch
            {
            }
        }

    }
}
