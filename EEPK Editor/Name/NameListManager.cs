using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Xv2CoreLib.EEPK;
using Xv2CoreLib.EffectContainer;
using Xv2CoreLib.Resource;
using Xv2CoreLib.Resource.App;

namespace EEPK_Organiser.NameList
{
    public class NameListManager : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private ObservableCollection<NameListFile> _loadedNameLists = null;
        public ObservableCollection<NameListFile> LoadedNameLists
        {
            get
            {
                return this._loadedNameLists;
            }
            set
            {
                if (value != this._loadedNameLists)
                {
                    this._loadedNameLists = value;
                    NotifyPropertyChanged("LoadedNameLists");
                }
            }
        }

        public string NameListDir { get { return SettingsManager.Instance.GetAbsPathInAppFolder("namelist"); } }

        public NameListManager()
        {
            LoadedNameLists = new ObservableCollection<NameListFile>();
            LoadNameLists();

            if(LoadedNameLists == null)
            {
                LoadedNameLists = new ObservableCollection<NameListFile>();
            }
        }

        private void LoadNameLists()
        {
            if (Directory.Exists(NameListDir))
            {
                string[] files = Directory.GetFiles(NameListDir);

                foreach (var file in files)
                {
                    try
                    {
                        LoadedNameLists.Add(new NameListFile() { Name = Path.GetFileNameWithoutExtension(file), path = file });
                    }
                    catch
                    {

                    }
                }
            }
        }

        public void ApplyNameList(IList<Effect> effects, NameList _namelist)
        {

            foreach(var effect in effects)
            {
                effect.NameList = _namelist.GetName(effect.IndexNum);
            }
        }

        public void ClearNameList(IList<Effect> effects)
        {
            foreach (var effect in effects)
            {
                effect.NameList = null;
            }
        }

        public void SaveNameList(IList<Effect> effects)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Title = "Save Name List";
            saveDialog.Filter = "XML file | *.xml";
            saveDialog.InitialDirectory = Path.GetFullPath(NameListDir);
            saveDialog.AddExtension = true;
            saveDialog.ShowDialog();

            if (!String.IsNullOrWhiteSpace(saveDialog.FileName))
            {
                string selectedDir = Path.GetDirectoryName(saveDialog.FileName);

                //Create the Name List
                NameList nameList = new NameList();
                nameList.Names = new List<NameListEntry>();

                foreach(var effect in effects)
                {
                    if(effect.NameList != null)
                    {
                        nameList.Names.Add(new NameListEntry()
                        {
                            EffectID = effect.IndexNum,
                            Description = effect.NameList
                        });
                    }
                }

                //Save Name List
                nameList.Save(saveDialog.FileName);

                //Add Name List to current list
                if(selectedDir == Path.GetFullPath(NameListDir))
                {
                    NameListFile nameListFile = new NameListFile();
                    nameListFile.Name = Path.GetFileNameWithoutExtension(saveDialog.FileName);
                    nameListFile.path = saveDialog.FileName;
                    nameListFile.File = nameList;
                    LoadedNameLists.Add(nameListFile);
                }

                MessageBox.Show("The name list was saved.", "Save Name List", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("The entered path was invalid.", "Save Name List", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        public void EepkLoaded(EffectContainerFile effectContainerFile)
        {
            if (effectContainerFile == null) return;

            NameList nameListToUse = GetNameListForFile(effectContainerFile.Name);

            if(nameListToUse != null)
            {
                ApplyNameList(effectContainerFile.Effects, nameListToUse);
            }
        }

        private NameList GetNameListForFile(string name)
        {
            foreach(var nameList in LoadedNameLists)
            {
                if(nameList.Name == name)
                {
                    return nameList.GetNameList();
                }
            }

            return null;
        }

    }
}
