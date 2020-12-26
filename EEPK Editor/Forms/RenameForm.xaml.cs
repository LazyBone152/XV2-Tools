using MahApps.Metro.Controls;
using System;
using System.Windows;
using System.Windows.Input;
using Xv2CoreLib.EffectContainer;
using Xv2CoreLib.EMB_CLASS;
using Xv2CoreLib.EMM;

namespace EEPK_Organiser.Forms
{
    /// <summary>
    /// Interaction logic for RenameForm.xaml
    /// </summary>
    public partial class RenameForm : MetroWindow
    {
        public enum Mode
        {
            Asset,
            Texture,
            Material
        }
        private Mode CurrentMode { get; set; }

        private string OriginalName = null;
        public bool WasNameChanged { get; private set; }
        public string NameValue { get; set; }
        private string Extension { get; set; }
        private int LengthLimit = -1;

        //ContainerObjects
        private AssetContainerTool assetContainer = null;
        private EMB_File EmbFile = null;
        private EMM_File EmmFile = null;

        public RenameForm(string originalName, string extension, string _windowTitle, object containerObject, Window parent = null, Mode _mode = Mode.Asset, int _stringSize = -1)
        {
            switch (_mode)
            {
                case Mode.Asset:
                    assetContainer = containerObject as AssetContainerTool;
                    break;
                case Mode.Texture:
                    EmbFile = containerObject as EMB_File;
                    break;
                case Mode.Material:
                    EmmFile = containerObject as EMM_File;
                    break;
            }

            LengthLimit = _stringSize;
            NameValue = originalName;
            Extension = extension;
            InitializeComponent();
            Title = _windowTitle;
            DataContext = this;
            Owner = parent;
            textBox.Focus();
            OriginalName = NameValue + Extension;
            CurrentMode = _mode;
        }

        private void Button_OK_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(NameValue))
            {
                MessageBox.Show("The name cannot be empty or whitespace.", "Invalid Name", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (NameValue.Length > LengthLimit && LengthLimit != -1)
            {
                MessageBox.Show(String.Format("The name cannot exceed {0} characters.", LengthLimit), "Invalid Name", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (NameValue + Extension == OriginalName)
            {
                WasNameChanged = false;
                Close();
                return;
            }

            if (CurrentMode == Mode.Asset)
            {
                if (assetContainer.NameUsed(NameValue + Extension))
                {
                    MessageBox.Show(String.Format("The name \"{0}{1}\" is already in use.", NameValue, Extension), "Rename", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            else if (CurrentMode == Mode.Texture)
            {
                if (EmbFile.NameUsed(NameValue + Extension))
                {
                    MessageBox.Show(String.Format("The name \"{0}{1}\" is already in use.", NameValue, Extension), "Rename", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            else if (CurrentMode == Mode.Material)
            {
                if (EmmFile.NameUsed(NameValue))
                {
                    MessageBox.Show(String.Format("The name \"{0}{1}\" is already in use.", NameValue, Extension), "Rename", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            WasNameChanged = true;
            Close();
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            WasNameChanged = false;
            Close();
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Button_OK_Click(null, null);
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                Button_OK_Click(null, null);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            textBox.CaretIndex = textBox.Text.Length;
        }
    }
}
