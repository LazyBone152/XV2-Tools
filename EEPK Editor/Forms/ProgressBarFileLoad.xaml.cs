using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Xv2CoreLib.EffectContainer;
using Xv2CoreLib.Resource;

namespace EEPK_Organiser.Forms
{
    /// <summary>
    /// Interaction logic for ProgressBarFileLoad.xaml
    /// </summary>
    public partial class ProgressBarFileLoad : Window
    {

        public EffectContainerFile effectContainerFile { get; set; }
        public string FilePath { get; set; }
        private Xv2FileIO fileIO = null;
        private bool onlyFromCpk = false;
        public Exception exception = null;

        public ProgressBarFileLoad(string eepkFilePath, Window parent, Xv2FileIO _fileIO = null, bool _onlyFromCpk = false)
        {
            fileIO = _fileIO;
            onlyFromCpk = _onlyFromCpk;
            FilePath = eepkFilePath;
            InitializeComponent();
            Owner = parent;
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
