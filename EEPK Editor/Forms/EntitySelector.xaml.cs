using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System;
using MahApps.Metro.Controls;

namespace EEPK_Organiser.Forms
{
    /// <summary>
    /// Interaction logic for EntitySelector.xaml
    /// </summary>
    public partial class EntitySelector : MetroWindow
    {
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

        public EntitySelector(LoadFromGameHelper _gameInterface, EntityType _entityType, object parent)
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
            Owner = Application.Current.MainWindow;

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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SelectedEntity = listBox.SelectedItem as GameEntity;
            Close();
        }

        private void ListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if(e.LeftButton == MouseButtonState.Pressed && (listBox.SelectedItem as GameEntity) != null)
            {
                SelectedEntity = listBox.SelectedItem as GameEntity;
                Close();
            }
        }

        private void ListBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.Enter) && (listBox.SelectedItem as GameEntity) != null)
            {
                e.Handled = true;
                SelectedEntity = listBox.SelectedItem as GameEntity;
                Close();
            }
        }
    }
}
