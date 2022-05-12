using MahApps.Metro.Controls;
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
using AudioCueEditor.Data;
using GalaSoft.MvvmLight.CommandWpf;

namespace AudioCueEditor.Forms
{
    /// <summary>
    /// Interaction logic for HcaEncryptionKeysEditor.xaml
    /// </summary>
    public partial class HcaEncryptionKeysEditor : MetroWindow
    {

        public HcaEncryptionKeys Keys { get; set; }
        public HcaEncryptionKey SelectedKey { get; set; }
        public ulong ThisAcbKey { get; set; }
        public string ThisAcbKeyText => $"Current ACB: {ThisAcbKey}";
        public bool HasThisKey => ThisAcbKey != 0;
        
        public HcaEncryptionKeysEditor(ulong thisAcbKey, Window parent)
        {
            Keys = HcaEncryptionKeysManager.Instance.EncryptionKeys;
            ThisAcbKey = thisAcbKey;
            Owner = parent;
            InitializeComponent();
        }

        public RelayCommand AddNewCommand => new RelayCommand(AddNew);
        private void AddNew()
        {
            HcaEncryptionKeysManager.Instance.AddKey(0);
        }

        public RelayCommand RemoveCommand => new RelayCommand(RemoveKey, CanRemoveKey);
        private void RemoveKey()
        {
            Keys.Keys.Remove(SelectedKey);
        }

        private bool CanRemoveKey()
        {
            return SelectedKey != null;
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            HcaEncryptionKeysManager.Instance.Save();
        }

        private void Key_Add(object sender, RoutedEventArgs e)
        {
            HcaEncryptionKeysManager.Instance.AddKey(ThisAcbKey);
        }

        private void Key_Copy(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(ThisAcbKey.ToString());
        }
    }
}
