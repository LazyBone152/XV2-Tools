using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System;
using MahApps.Metro.Controls;
using System.ComponentModel;
using System.Windows.Data;
using GalaSoft.MvvmLight.CommandWpf;

namespace EEPK_Organiser.Forms
{
    /// <summary>
    /// Interaction logic for EntitySelector.xaml
    /// </summary>
    public partial class EntitySelector : MetroWindow, INotifyPropertyChanged
    {
        #region NotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        public enum EntityType
        {
            Character,
            SuperSkill,
            UltimateSkill,
            EvasiveSkill,
            BlastSkill,
            AwokenSkill,
            Stage,
            Demo,
            CMN
        }

        public ObservableCollection<GameEntity> Entities { get; set; }

        public GameEntity SelectedEntity { get; set; }
        public bool OnlyLoadFromCPK { get; set; }

        public EntitySelector(LoadFromGameHelper _gameInterface, EntityType _entityType, Window parent)
        {
            switch (_entityType)
            {
                case EntityType.Character:
                    Entities = _gameInterface.characters;
                    break;
                case EntityType.SuperSkill:
                    Entities = _gameInterface.superSkills;
                    break;
                case EntityType.UltimateSkill:
                    Entities = _gameInterface.ultimateSkills;
                    break;
                case EntityType.EvasiveSkill:
                    Entities = _gameInterface.evasiveSkills;
                    break;
                case EntityType.BlastSkill:
                    Entities = _gameInterface.blastSkills;
                    break;
                case EntityType.AwokenSkill:
                    Entities = _gameInterface.awokenSkills;
                    break;
                case EntityType.Demo:
                    Entities = _gameInterface.demo;
                    break;
                case EntityType.CMN:
                    Entities = _gameInterface.cmn;
                    break;
                default:
                    throw new Exception(String.Format("EntitySelector: unknown _entityType ({0})", _entityType));
            }

            InitializeComponent();
            DataContext = this;
            Owner = parent;

            switch (_entityType)
            {
                case EntityType.Character:
                    Title = "Select Character";
                    break;
                case EntityType.SuperSkill:
                    Title = "Select Super Skill";
                    break;
                case EntityType.UltimateSkill:
                    Title = "Select Ultimate Skill";
                    break;
                case EntityType.EvasiveSkill:
                    Title = "Select Evasive Skill";
                    break;
                case EntityType.BlastSkill:
                    Title = "Select Blast Skill";
                    break;
                case EntityType.AwokenSkill:
                    Title = "Select Awoken Skill";
                    break;
                case EntityType.Demo:
                    Title = "Select Demo (Cutscene)";
                    break;
                case EntityType.CMN:
                    Title = "Select CMN";
                    break;
            }


        }

        public RelayCommand SelectItemCommand => new RelayCommand(SelectItem, () => listBox.SelectedItem != null);
        private void SelectItem()
        {
            SelectedEntity = listBox.SelectedItem as GameEntity;
            Close();
        }


        #region Search
        private string _searchFilter = null;
        public string SearchFilter
        {
            get => _searchFilter;
            set
            {
                _searchFilter = value;
                RefreshSearchResults();
                NotifyPropertyChanged(nameof(SearchFilter));
            }
        }

        private ListCollectionView _filterList = null;
        public ListCollectionView FilterList
        {
            get
            {
                if (_filterList == null && Entities != null)
                {
                    _filterList = new ListCollectionView(Entities);
                    _filterList.Filter = new Predicate<object>(SearchFilterCheck);
                }
                return _filterList;
            }
            set
            {
                if (value != _filterList)
                {
                    _filterList = value;
                    NotifyPropertyChanged(nameof(FilterList));
                }
            }
        }

        public bool SearchFilterCheck(object material)
        {
            if (string.IsNullOrWhiteSpace(SearchFilter)) return true;
            GameEntity item = material as GameEntity;
            string searchParam = SearchFilter.ToLower();

            if (item != null)
            {
                if (item.Name != null)
                {
                    if (item.Name.ToLower().Contains(searchParam)) return true;
                }

                int num;
                if (int.TryParse(searchParam, out num))
                {
                    if (item.ID == num) return true;
                }
            }

            return false;
        }

        private void RefreshSearchResults()
        {
            if (_filterList == null)
                _filterList = new ListCollectionView(Entities);

            _filterList.Filter = new Predicate<object>(SearchFilterCheck);
            NotifyPropertyChanged(nameof(FilterList));
        }

        public RelayCommand ClearSearchCommand => new RelayCommand(ClearSearch);
        private void ClearSearch()
        {
            SearchFilter = string.Empty;
        }
        
        #endregion
    }
}
