using MahApps.Metro.Controls;
using System;
using System.ComponentModel;
using System.Windows;
using Xv2CoreLib.EMB_CLASS;

namespace EEPK_Organiser.Forms
{
    /// <summary>
    /// Interaction logic for TextureSelector.xaml
    /// </summary>
    public partial class TextureSelector : MetroWindow, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private EMB_File _embFileValue = null;
        public EMB_File embFile
        {
            get
            {
                return this._embFileValue;
            }
            set
            {
                if (value != this._embFileValue)
                {
                    this._embFileValue = value;
                    NotifyPropertyChanged("embFile");
                }
            }
        }
        public EmbEntry SelectedTexture { get; private set; }

        public TextureSelector(EMB_File _embFile, object parent, EmbEntry initialSelection)
        {
            embFile = _embFile;
            InitializeComponent();
            DataContext = this;
            Owner = Application.Current.MainWindow;
            listBox_Textures.SelectedItem = initialSelection;
            listBox_Textures.ScrollIntoView(initialSelection);
        }

        private void Finish()
        {
            if(listBox_Textures.SelectedIndex != -1)
            {
                SelectedTexture = listBox_Textures.SelectedItem as EmbEntry;
            }

            Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Finish();
        }
    }
}
