using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace AudioCueEditor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static MetroDialogSettings DefaultDialogSettings = new MetroDialogSettings() { AnimateHide = false, AnimateShow = false, DialogTitleFontSize = 16, DialogMessageFontSize = 12 };

        public static string ExePath { get { return System.Reflection.Assembly.GetEntryAssembly().Location; } }
        public static string ErrorLogPath { get { return $"{Path.GetDirectoryName(ExePath)}/ace_log.txt"; } }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
#if !DEBUG
            MessageBox.Show(String.Format("An unhandled exception was raised during execution of the application. \n\nDetails: {0}\n\nA log containing details about the error was saved at \"{1}\".", e.Exception.Message, ErrorLogPath), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            SaveExceptionLog(e.Exception.ToString());
            e.Handled = true;
#endif
        }

        public void SaveExceptionLog(string ex)
        {
            try
            {
                File.WriteAllText(ErrorLogPath, ex);
            }
            catch
            {
            }
        }

    }
}
