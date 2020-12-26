using MahApps.Metro.Controls;
using System;
using System.Threading.Tasks;
using System.Windows;
using Xv2CoreLib.EffectContainer;
using Xv2CoreLib.Resource;

namespace EEPK_Organiser.Forms
{
    /// <summary>
    /// Interaction logic for ProgressBarFileLoad.xaml
    /// </summary>
    public partial class ProgressBarFileLoad : MetroWindow
    {

        public EffectContainerFile effectContainerFile { get; set; }
        public string FilePath { get; set; }
        private Xv2FileIO fileIO = null;
        private bool onlyFromCpk = false;
        public Exception exception = null;

        public ProgressBarFileLoad(string eepkFilePath, object parent, Xv2FileIO _fileIO, bool _onlyFromCpk = false)
        {
            fileIO = _fileIO;
            onlyFromCpk = _onlyFromCpk;
            FilePath = eepkFilePath;
            InitializeComponent();
            Owner = Application.Current.MainWindow;
        }

        private void Load()
        {
            if(fileIO == null)
            {
                //Load files directly
                if(System.IO.Path.GetExtension(FilePath) == ".eepk")
                {
                    effectContainerFile = EffectContainerFile.Load(FilePath);
                }
                else if (System.IO.Path.GetExtension(FilePath) == EffectContainerFile.ZipExtension)
                {
                    effectContainerFile = EffectContainerFile.LoadVfx2(FilePath);
                }
            }
            else
            {
                //Load from game
                effectContainerFile = EffectContainerFile.Load(FilePath, fileIO, onlyFromCpk);
            }
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            LoadFileAsync();
        }

        private async Task LoadFileAsync()
        {
            try
            {
                await Task.Run(new Action(Load));
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            finally
            {
                Close();
            }
        }
    }
}
