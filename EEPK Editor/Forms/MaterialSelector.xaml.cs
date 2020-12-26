using MahApps.Metro.Controls;
using System;
using System.ComponentModel;
using System.Windows;
using Xv2CoreLib.EMM;

namespace EEPK_Organiser.Forms
{
    /// <summary>
    /// Interaction logic for TextureSelector.xaml
    /// </summary>
    public partial class MaterialSelector : MetroWindow, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private EMM_File _emmFileValue = null;
        public EMM_File emmFile
        {
            get
            {
                return this._emmFileValue;
            }
            set
            {
                if (value != this._emmFileValue)
                {
                    this._emmFileValue = value;
                    NotifyPropertyChanged("emmFile");
                }
            }
        }
        public Material SelectedMaterial { get; private set; }

        public MaterialSelector(EMM_File _emmFile, object parent, Material initialSelection)
        {
            emmFile = _emmFile;
            InitializeComponent();
            DataContext = this;
            Owner = Application.Current.MainWindow;
            listBox_Materials.SelectedItem = initialSelection;
            listBox_Materials.ScrollIntoView(initialSelection);
        }

        private void Finish()
        {
            if(listBox_Materials.SelectedItem != null)
            {
                SelectedMaterial = listBox_Materials.SelectedItem as Material;
            }

            Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Finish();
        }
    }
}
