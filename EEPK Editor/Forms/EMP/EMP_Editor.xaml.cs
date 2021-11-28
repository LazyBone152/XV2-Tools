using EEPK_Organiser.Forms.Recolor;
using EEPK_Organiser.Misc;
using EEPK_Organiser.View;
using GalaSoft.MvvmLight.CommandWpf;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Xv2CoreLib.EMB_CLASS;
using Xv2CoreLib.EMM;
using Xv2CoreLib.EMP;
using Xv2CoreLib.Resource;
using Xv2CoreLib.Resource.App;
using Xv2CoreLib.Resource.UndoRedo;

namespace EEPK_Organiser.Forms.EMP
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class EMP_Editor : MetroWindow, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private EMP_File _empFileValue = null;
        public EMP_File empFile
        {
            get
            {
                return this._empFileValue;
            }
            set
            {
                if (value != this._empFileValue)
                {
                    this._empFileValue = value;
                    NotifyPropertyChanged("empFile");
                }
            }
        }


        private Xv2CoreLib.EMB_CLASS.EMB_File textureContainer { get; set; }
        private EMM_File materialFile { get; set; }
        private EepkEditor mainWindow = null;

        public EMP_Editor(EMP_File _empFile, string empName, Xv2CoreLib.EMB_CLASS.EMB_File _textureContainer, EMM_File _materialFile, EepkEditor _mainWindow)
        {
            if (_empFile == null)
                throw new InvalidDataException("_empFile was null. Cannot open EMP Editor.");

            mainWindow = _mainWindow;
            materialFile = _materialFile;
            textureContainer = _textureContainer;
            empFile = _empFile;
            InitializeComponent();
            InputValidationRegister();
            DataContext = this;

            //Set window name
            Title = String.Format("EMP Editor ({0})", empName);

            //Hidding tabs
            CollapseAll();

        }

        private void InputValidationRegister()
        {
            //General
            general_Name.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Str_Size32);

            //Texture Part
            textBox_R.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_RgbInt);
            textBox_g.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_RgbInt);
            textBox_b.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_RgbInt);
            textBox_a.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_RgbInt);
            texturePart_I_00.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_UInt8);
            texturePart_I_01.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_UInt8);
            texturePart_I_02.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_UInt8);
            texturePart_I_03.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_UInt8);
            texturePart_F_05.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            texturePart_F_100.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            texturePart_F_104.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            texturePart_F_108.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            texturePart_F_24.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            texturePart_F_28.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            texturePart_F_32.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            texturePart_F_36.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            texturePart_F_40.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            texturePart_F_44.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            texturePart_F_96.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            texturePart_I_08.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Int32);
            texturePart_I_12.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Int32);

            verticalDistibution_F_00.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            verticalDistibution_F_04.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            verticalDistibution_F_08.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            verticalDistibution_F_12.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            verticalDistibution_F_16.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            verticalDistibution_F_20.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            verticalDistibution_F_24.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            verticalDistibution_F_28.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            sphericalDistibution_F_00.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            sphericalDistibution_F_04.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            sphericalDistibution_F_08.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            spherical_F_12.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            shapeAreaDistibution_F_00.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            shapeAreaDistibution_F_04.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            shapeAreaDistibution_F_08.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            shapeAreaDistibution_F_12.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            shapeAreaDistibution_F_16.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            shapeAreaDistibution_F_20.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            shapeAreaDistibution_F_24.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            shapeAreaDistibution_F_28.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            shapeAreaDistibution_F_32.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            shapeAreaDistibution_F_36.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            shapeAreaDistibution_F_44.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            shapePerimeterDistibution_F_00.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            shapePerimeterDistibution_F_00.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            shapePerimeterDistibution_F_00.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            shapePerimeterDistibution_F_00.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            shapePerimeterDistibution_F_00.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            shapePerimeterDistibution_F_00.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            shapePerimeterDistibution_F_00.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            shapeAreaDistibution_F_28.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            shapePerimeterDistibution_F_00.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            shapePerimeterDistibution_F_00.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            shapePerimeterDistibution_F_00.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            autoOriented_F_00.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            autoOriented_F_04.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            autoOriented_F_08.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            autoOriented_F_12.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            default_F_00.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            default_F_04.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            default_F_08.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            default_F_12.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            default_F_16.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            default_F_20.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            default_F_24.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            default_F_28.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            coneExtrude_I_00.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Int32);
            coneExtrude_I_02.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Int32);
            coneExtrude_I_04.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Int32);
            coneExtrude_I_08.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Int32);
            coneExtrude_I_10.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Int32);
            mesh_F_00.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            mesh_F_04.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            mesh_F_08.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            mesh_F_12.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            mesh_F_16.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            mesh_F_20.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            mesh_F_24.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            mesh_F_28.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            mesh_I_32.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Int32);
            mesh_I_40.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Int32);
            mesh_I_44.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Int32);
            shapeDraw_F_00.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            shapeDraw_F_04.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            shapeDraw_F_08.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            shapeDraw_F_12.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            shapeDraw_I_24.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Int32);
            shapeDraw_I_26.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Int32);
            shapeDraw_I_28.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Int32);
            shapeDraw_I_30.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Int32);
            animType0_I_00.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Int32);
            animType0_I_03.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Int32);
            animType0_F_04.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            animType1_Header_I_00.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Int32);
            animType1_Header_I_01.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Int32);
            animType1_I_08.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Int32);
            animType1_I_03.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Int32);
            animType1_F_04.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);

            general_F_128.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            general_F_132.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            general_I_136.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Int32);
            general_MaxParticlesPerFrame.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Int32);
            general_MaxParticlesPerFrameAddRandom.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Int32);
            general_NewDelay.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Int32);
            general_NewDelayAddRandom.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Int32);
            general_ParticleCount.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Int32);
            general_ParticleLifetime.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Int32);
            general_ParticleLifetimeAddRandom.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Int32);
            general_StartTime.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Int32);
            general_StartTimeAddRandom.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Int32);
            general_Transform_W.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            general_Transform_W1.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            general_Transform_W_AddRandom.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            general_Transform_W_AddRandom1.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            general_Transform_X.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            general_Transform_X1.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            general_Transform_X_AddRandom.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            general_Transform_X_AddRandom1.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            general_Transform_Y.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            general_Transform_Y1.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            general_Transform_Y_AddRandom.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            general_Transform_Y_AddRandom1.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            general_Transform_Z.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            general_Transform_Z1.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            general_Transform_Z_AddRandom.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            general_Transform_Z_AddRandom1.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);

            texture_I_02.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Int32);
            texture_I_04.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Int32);
            texture_I_05.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Int32);
            texture_I_08.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Int32);
            texture_I_09.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Int32);
            textureType0_ScrollX.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            textureType0_ScrollY.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            textureType0_ScaleX.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            textureType0_ScaleY.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            textureType1_ScrollSpeedU.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);
            textureType1_ScrollSpeedV.TextChanged += new TextChangedEventHandler(Misc.InputValidation.InputValidator_Float);

        }

        //Tab Control
        private void CollapseAll()
        {
            Tab_AutoOriented.Visibility = Visibility.Collapsed;
            Tab_ConeExtrude.Visibility = Visibility.Collapsed;
            Tab_Default.Visibility = Visibility.Collapsed;
            Tab_Mesh.Visibility = Visibility.Collapsed;
            Tab_ShapeAreaDistribution.Visibility = Visibility.Collapsed;
            Tab_ShapeDraw.Visibility = Visibility.Collapsed;
            Tab_ShapePerimeterDistribution.Visibility = Visibility.Collapsed;
            Tab_SphericalDistribution.Visibility = Visibility.Collapsed;
            Tab_VerticalDistribution.Visibility = Visibility.Collapsed;
            tab_TexturePart.Visibility = Visibility.Collapsed;

            grid_AutoOriented.IsEnabled = false;
            grid_ConeExtrude.IsEnabled = false;
            grid_Default.IsEnabled = false;
            grid_Mesh.IsEnabled = false;
            grid_ShapeAreaDistribution.IsEnabled = false;
            grid_ShapeDraw.IsEnabled = false;
            grid_ShapePerimeterDistribution.IsEnabled = false;
            grid_SphericalDistribution.IsEnabled = false;
            grid_VerticalDistribution.IsEnabled = false;
            grid_TexturePart.IsEnabled = false;


            try
            {
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void ComponentType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CollapseAll();

            ComboBox _comboBox = (ComboBox)sender;

            if (_comboBox.SelectedItem is ParticleEffect.ComponentType)
            {
                switch (_comboBox.SelectedItem)
                {
                    case ParticleEffect.ComponentType.AutoOriented_VisibleOnSpeed:
                        tab_TexturePart.Visibility = Visibility.Visible;
                        grid_TexturePart.IsEnabled = true;
                        break;
                    case ParticleEffect.ComponentType.AutoOriented:
                        Tab_AutoOriented.Visibility = Visibility.Visible;
                        tab_TexturePart.Visibility = Visibility.Visible;
                        grid_AutoOriented.IsEnabled = true;
                        grid_TexturePart.IsEnabled = true;
                        break;
                    case ParticleEffect.ComponentType.ConeExtrude:
                        Tab_ConeExtrude.Visibility = Visibility.Visible;
                        tab_TexturePart.Visibility = Visibility.Visible;
                        grid_TexturePart.IsEnabled = true;
                        grid_ConeExtrude.IsEnabled = true;
                        break;
                    case ParticleEffect.ComponentType.Default:
                        Tab_Default.Visibility = Visibility.Visible;
                        tab_TexturePart.Visibility = Visibility.Visible;
                        grid_TexturePart.IsEnabled = true;
                        grid_Default.IsEnabled = true;
                        break;
                    case ParticleEffect.ComponentType.Mesh:
                        Tab_Mesh.Visibility = Visibility.Visible;
                        tab_TexturePart.Visibility = Visibility.Visible;
                        grid_TexturePart.IsEnabled = true;
                        grid_Mesh.IsEnabled = true;
                        break;
                    case ParticleEffect.ComponentType.ShapeAreaDistribution:
                        Tab_ShapeAreaDistribution.Visibility = Visibility.Visible;
                        grid_ShapeAreaDistribution.IsEnabled = true;
                        break;
                    case ParticleEffect.ComponentType.ShapeDraw:
                        Tab_ShapeDraw.Visibility = Visibility.Visible;
                        tab_TexturePart.Visibility = Visibility.Visible;
                        grid_TexturePart.IsEnabled = true;
                        grid_ShapeDraw.IsEnabled = true;
                        break;
                    case ParticleEffect.ComponentType.ShapePerimeterDistribution:
                        Tab_ShapePerimeterDistribution.Visibility = Visibility.Visible;
                        grid_ShapePerimeterDistribution.IsEnabled = true;
                        break;
                    case ParticleEffect.ComponentType.SphericalDistribution:
                        Tab_SphericalDistribution.Visibility = Visibility.Visible;
                        grid_SphericalDistribution.IsEnabled = true;
                        break;
                    case ParticleEffect.ComponentType.VerticalDistribution:
                        Tab_VerticalDistribution.Visibility = Visibility.Visible;
                        grid_VerticalDistribution.IsEnabled = true;
                        break;
                }
            }

            //If the currently selected tab is not visible, then change it to general (tab 0)
            if (tabControl_ParticleEffect.SelectedItem as TabItem != null)
            {
                if ((tabControl_ParticleEffect.SelectedItem as TabItem).Visibility == Visibility.Collapsed)
                {
                    tabControl_ParticleEffect.SelectedIndex = 0;
                }
            }
        }

        //Undo/Redo 
        public RelayCommand UndoCommand => new RelayCommand(Undo, UndoManager.Instance.CanUndo);
        private void Undo()
        {
            UndoManager.Instance.Undo();
            RefreshItems();
        }

        public RelayCommand RedoCommand => new RelayCommand(Redo, UndoManager.Instance.CanRedo);
        private void Redo()
        {
            UndoManager.Instance.Redo();
            RefreshItems();
        }

        private void RefreshItems()
        {
            empTree.Items.Refresh();
        }

        //ParticleEffect (general)
        public RelayCommand AddParticleEffect_Command => new RelayCommand(AddParticleEffect);
        private void AddParticleEffect()
        {
            if (empFile == null) return;

            try
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();
                empFile.AddNew(-1, undos);
                UndoManager.Instance.AddUndo(new CompositeUndo(undos, "New Particle Effect"));
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        public RelayCommand AddParticleEffectAsChild_Command => new RelayCommand(AddParticleEffectAsChild, IsParticleSelected);
        private void AddParticleEffectAsChild()
        {
            if (empFile == null) return;

            try
            {
                ParticleEffect _particleEffect = empTree.SelectedItem as ParticleEffect;
                if (_particleEffect == null) return;

                List<IUndoRedo> undos = new List<IUndoRedo>();
                _particleEffect.AddNew(-1, undos);
                UndoManager.Instance.AddUndo(new CompositeUndo(undos, "Add Child Particle Effect"));

                RefreshItems();
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public RelayCommand RemoveParticleEffect_Command => new RelayCommand(RemoveParticleEffect, IsParticleSelected);
        private void RemoveParticleEffect()
        {
            if (empFile == null) return;
            ParticleEffect _particleEffect = empTree.SelectedItem as ParticleEffect;
            if (_particleEffect == null) return;

            List<IUndoRedo> undos = new List<IUndoRedo>();
            empFile.RemoveParticleEffect(_particleEffect, undos);

            UndoManager.Instance.AddCompositeUndo(undos, "Remove Particle Effect");

            RefreshItems();
            empTree.Focus();

        }

        private void TreeViewItem_MouseRightButtonDown(object sender, MouseEventArgs e)
        {
            TreeViewItem item = sender as TreeViewItem;
            if (item != null)
            {
                item.Focus();
                e.Handled = true;
            }
        }

        private bool IsParticleSelected()
        {
            return empTree.SelectedItem is ParticleEffect;
        }

        //TreeView Context Menu
        public RelayCommand AddNewAbove_Command => new RelayCommand(AddNewAbove, IsParticleSelected);
        private void AddNewAbove()
        {
            ParticleEffect _particleEffect = empTree.SelectedItem as ParticleEffect;
            if (_particleEffect == null) return;
            AsyncObservableCollection<ParticleEffect> parentList = empFile.GetParentList(_particleEffect);

            if (parentList != null)
            {
                var newEffect = ParticleEffect.GetNew();

                int index = parentList.IndexOf(_particleEffect);
                UndoManager.Instance.AddUndo(new UndoableListInsert<ParticleEffect>(parentList, index, newEffect));
                parentList.Insert(index, newEffect);
            }

        }

        public RelayCommand AddNewBelow_Command => new RelayCommand(AddNewBelow, IsParticleSelected);
        private void AddNewBelow()
        {
            ParticleEffect _particleEffect = empTree.SelectedItem as ParticleEffect;
            if (_particleEffect == null) return;
            AsyncObservableCollection<ParticleEffect> parentList = empFile.GetParentList(_particleEffect);

            if (parentList != null)
            {
                var newEffect = ParticleEffect.GetNew();

                int index = parentList.IndexOf(_particleEffect);
                UndoManager.Instance.AddUndo(new UndoableListInsert<ParticleEffect>(parentList, index + 1, newEffect));
                parentList.Insert(index + 1, newEffect);
            }
        }

        public RelayCommand MoveUp_Command => new RelayCommand(MoveUp, IsParticleSelected);
        private void MoveUp()
        {
            ParticleEffect _particleEffect = empTree.SelectedItem as ParticleEffect;
            if (_particleEffect == null) return;

            try
            {
                AsyncObservableCollection<ParticleEffect> parentList = empFile.GetParentList(_particleEffect);

                if (parentList != null)
                {
                    int oldIndex = parentList.IndexOf(_particleEffect);
                    if (oldIndex == 0) return;
                    UndoManager.Instance.AddUndo(new UndoableListMove<ParticleEffect>(parentList, oldIndex, oldIndex - 1));
                    parentList.Move(oldIndex, oldIndex - 1);
                }
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public RelayCommand MoveDown_Command => new RelayCommand(MoveDown, IsParticleSelected);
        private void MoveDown()
        {
            ParticleEffect _particleEffect = empTree.SelectedItem as ParticleEffect;

            try
            {
                if (_particleEffect == null) return;
                AsyncObservableCollection<ParticleEffect> parentList = empFile.GetParentList(_particleEffect);

                if (parentList != null)
                {
                    int oldIndex = parentList.IndexOf(_particleEffect);
                    if (oldIndex >= parentList.Count - 1) return;
                    UndoManager.Instance.AddUndo(new UndoableListMove<ParticleEffect>(parentList, oldIndex, oldIndex + 1));
                    parentList.Move(oldIndex, oldIndex + 1);
                }
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public RelayCommand Copy_Command => new RelayCommand(Copy, IsParticleSelected);
        private void Copy()
        {
            ParticleEffect _particleEffect = empTree.SelectedItem as ParticleEffect;
            if (_particleEffect == null) return;

            try
            {
                textureContainer.SaveDdsImages();
                Clipboard.SetData(ClipboardDataTypes.EmpParticleEffect, _particleEffect);
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public RelayCommand Paste_Command => new RelayCommand(Paste, CanPasteParticleEffect);
        private void Paste()
        {
            try
            {
                ParticleEffect particleEffect = (ParticleEffect)Clipboard.GetData(ClipboardDataTypes.EmpParticleEffect);

                if (particleEffect == null) return;
                List<IUndoRedo> undos = new List<IUndoRedo>();

                //Import texture entries
                ParticleEffect newParticleEffect = particleEffect;

                ImportTextureEntries(newParticleEffect, undos);
                ImportMaterialEntries(newParticleEffect, undos);

                //Paste particleEffect
                ParticleEffect _particleEffect = empTree.SelectedItem as ParticleEffect;

                //If selectedParticleEffect is null, we paste it at the root level
                if (_particleEffect == null)
                {
                    undos.Add(new UndoableListAdd<ParticleEffect>(empFile.ParticleEffects, newParticleEffect));
                    empFile.ParticleEffects.Add(newParticleEffect);
                }
                else
                {
                    //Else we place it at the level of the selected particleEffect
                    AsyncObservableCollection<ParticleEffect> parentList = empFile.GetParentList(_particleEffect);

                    if (parentList != null)
                    {
                        undos.Add(new UndoableListAdd<ParticleEffect>(parentList, newParticleEffect));
                        parentList.Add(newParticleEffect);

                        RefreshItems();
                    }
                }

                UndoManager.Instance.AddCompositeUndo(undos, "Paste Particle Effect");

            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            }
        }

        public RelayCommand PasteChild_Command => new RelayCommand(PasteChild, CanPasteParticleEffectAndIsSelected);
        private void PasteChild()
        {
            ParticleEffect _particleEffect = empTree.SelectedItem as ParticleEffect;
            if (_particleEffect == null) return;
            List<IUndoRedo> undos = new List<IUndoRedo>();

            try
            {
                ParticleEffect particleEffect = (ParticleEffect)Clipboard.GetData(ClipboardDataTypes.EmpParticleEffect);

                if (particleEffect == null) return;
                ParticleEffect newParticleEffect = particleEffect.Clone();

                ImportTextureEntries(newParticleEffect, undos);
                ImportMaterialEntries(newParticleEffect, undos);

                undos.Add(new UndoableListAdd<ParticleEffect>(_particleEffect.ChildParticleEffects, newParticleEffect));
                _particleEffect.ChildParticleEffects.Add(newParticleEffect);

                RefreshItems();

                UndoManager.Instance.AddCompositeUndo(undos, "Paste Particle Effect");
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        public RelayCommand PasteValues_Command => new RelayCommand(PasteValues, CanPasteParticleEffectAndIsSelected);
        private void PasteValues()
        {
            ParticleEffect selectedParticleEffect = empTree.SelectedItem as ParticleEffect;
            if (selectedParticleEffect == null) return;
            List<IUndoRedo> undos = new List<IUndoRedo>();

            try
            {
                ParticleEffect copiedParticleEffect = (ParticleEffect)Clipboard.GetData(ClipboardDataTypes.EmpParticleEffect);
                if (copiedParticleEffect == null) return;

                //Ensure the materials and textures exist
                ImportTextureEntries(copiedParticleEffect, undos);
                ImportMaterialEntries(copiedParticleEffect, undos);

                selectedParticleEffect.CopyValues(copiedParticleEffect, undos);

                UndoManager.Instance.AddCompositeUndo(undos, "Paste Values");
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImportTextureEntries(ParticleEffect particleEffect, List<IUndoRedo> undos)
        {
            //Add referenced texture entries if they are not found

            foreach (var texture in particleEffect.Type_Texture.TextureEntryRef)
            {
                var newTex = empFile.GetTexture(texture.TextureRef);

                if (newTex == null)
                {
                    undos.Add(new UndoableListAdd<EmbEntry>(textureContainer.Entry, texture.TextureRef.TextureRef));
                    texture.TextureRef.TextureRef = textureContainer.Add(texture.TextureRef.TextureRef);

                    //Add the texture
                    texture.TextureRef.EntryIndex = empFile.NextTextureId();
                    undos.Add(new UndoableListAdd<EMP_TextureDefinition>(empFile.Textures, texture.TextureRef));
                    empFile.Textures.Add(texture.TextureRef);
                }
                else
                {
                    //An identical texture was found, so set that as the ref
                    texture.TextureRef = newTex;
                }
            }

            //Recursively call this method again if this ParticleEffect has children
            if (particleEffect.ChildParticleEffects != null)
            {
                foreach (var child in particleEffect.ChildParticleEffects)
                {
                    ImportTextureEntries(child, undos);
                }
            }
        }

        private void ImportMaterialEntries(ParticleEffect particleEffect, List<IUndoRedo> undos)
        {
            //Add referenced material entries if they are not found
            if (particleEffect.Type_Texture.MaterialRef != null)
            {

                if (!materialFile.Materials.Contains(particleEffect.Type_Texture.MaterialRef))
                {
                    var result = materialFile.Compare(particleEffect.Type_Texture.MaterialRef);

                    if (result == null)
                    {
                        //Material didn't exist so we have to add it
                        particleEffect.Type_Texture.MaterialRef.Str_00 = materialFile.GetUnusedName(particleEffect.Type_Texture.MaterialRef.Str_00);
                        undos.Add(new UndoableListAdd<Material>(materialFile.Materials, particleEffect.Type_Texture.MaterialRef));
                        materialFile.Materials.Add(particleEffect.Type_Texture.MaterialRef);
                    }
                    else if (result != particleEffect.Type_Texture.MaterialRef)
                    {
                        //A identical material already existed but it was a different instance.
                        //Change the referenced material to this.
                        particleEffect.Type_Texture.MaterialRef = result;
                    }
                }
            }

            //Recursively call this method again if this ParticleEffect has children
            if (particleEffect.ChildParticleEffects != null)
            {
                foreach (var child in particleEffect.ChildParticleEffects)
                {
                    ImportMaterialEntries(child, undos);
                }
            }
        }

        public RelayCommand HueAdjustment_Command => new RelayCommand(HueAdjustment, IsParticleSelected);
        private void HueAdjustment()
        {
#if !DEBUG
            try
#endif
            {
                ParticleEffect _particleEffect = empTree.SelectedItem as ParticleEffect;
                if (_particleEffect == null) return;

                Forms.RecolorAll recolor = new Forms.RecolorAll(_particleEffect, this);

                if (recolor.Initialize())
                    recolor.ShowDialog();
            }
#if !DEBUG
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
#endif
        }

        public RelayCommand HueSet_Command => new RelayCommand(HueSet, IsParticleSelected);
        private void HueSet()
        {
#if !DEBUG
            try
#endif
            {
                ParticleEffect _particleEffect = empTree.SelectedItem as ParticleEffect;
                if (_particleEffect == null) return;

                RecolorAll_HueSet recolor = new RecolorAll_HueSet(_particleEffect, this);

                if (recolor.Initialize())
                    recolor.ShowDialog();
            }
#if !DEBUG
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
#endif
        }

        public RelayCommand HideAll_Command => new RelayCommand(HideAll);
        private void HideAll()
        {
            foreach(var particleEffect in empFile.ParticleEffects)
            {
                SetHideStatus(particleEffect, true);
            }
        }

        public RelayCommand HideAllSelected_Command => new RelayCommand(HideAllSelected, IsParticleSelected);
        private void HideAllSelected()
        {
            SetHideStatus(empTree.SelectedItem as ParticleEffect, true);
        }

        public RelayCommand ShowAll_Command => new RelayCommand(ShowAll);
        private void ShowAll()
        {
            foreach (var particleEffect in empFile.ParticleEffects)
            {
                SetHideStatus(particleEffect, false);
            }
        }

        public RelayCommand ShowAllSelected_Command => new RelayCommand(ShowAllSelected, IsParticleSelected);
        private void ShowAllSelected()
        {
            SetHideStatus(empTree.SelectedItem as ParticleEffect, false);
        }


        private bool CanPasteParticleEffect()
        {
            return Clipboard.ContainsData(ClipboardDataTypes.EmpParticleEffect);
        }

        private bool CanPasteParticleEffectAndIsSelected()
        {
            return IsParticleSelected() && CanPasteParticleEffect();
        }

        //Animation Tab (Type 0)
        private void AddNewAnimationType0_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ParticleEffect _particleEffect = empTree.SelectedItem as ParticleEffect;
                if (_particleEffect == null) return;
                var newAnimation = Type0.GetNew();
                _particleEffect.Type_0.Add(newAnimation);
                comboBox_Type0.SelectedItem = newAnimation;

                UndoManager.Instance.AddUndo(new UndoableListAdd<Type0>(_particleEffect.Type_0, newAnimation, "New Animation"));
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemoveAnimationType0_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ParticleEffect _particleEffect = empTree.SelectedItem as ParticleEffect;
                Type0 _type0 = comboBox_Type0.SelectedItem as Type0;
                if (_particleEffect == null) return;
                if (_type0 == null) return;

                List<IUndoRedo> undos = new List<IUndoRedo>();

                undos.Add(new UndoableListRemove<Type0>(_particleEffect.Type_0, _type0));
                _particleEffect.Type_0.Remove(_type0);

                if (_particleEffect.Type_0.Count > 0)
                {
                    comboBox_Type0.SelectedIndex = 0;
                }

                UndoManager.Instance.AddCompositeUndo(undos, "Remove Animation");
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }



        public RelayCommand AddType0KeyframeCommand => new RelayCommand(AddType0Keyframe, CanAddType0Keyframe);
        private void AddType0Keyframe()
        {
            Type0 type0 = comboBox_Type0.SelectedItem as Type0;

            if (type0 != null)
            {
                Type0_Keyframe keyframe = new Type0_Keyframe();
                UndoManager.Instance.AddUndo(new UndoableListAdd<Type0_Keyframe>(type0.Keyframes, keyframe, "Add Keyframe"));
                type0.Keyframes.Add(keyframe);
            }
        }

        public RelayCommand RemoveType0KeyframeCommand => new RelayCommand(RemoveType0Keyframe, CanRemoveType0Keyframe);
        private void RemoveType0Keyframe()
        {
            Type0 type0 = comboBox_Type0.SelectedItem as Type0;
            Type0_Keyframe keyframe = dataGrid_type0_Keyframes.SelectedItem as Type0_Keyframe;

            if (keyframe != null && type0 != null)
            {
                if (type0.Keyframes.Contains(keyframe))
                {
                    UndoManager.Instance.AddUndo(new UndoableListRemove<Type0_Keyframe>(type0.Keyframes, keyframe, "Remove Keyframe"));
                    type0.Keyframes.Remove(keyframe);
                }
            }
        }

        private bool CanAddType0Keyframe()
        {
            return (comboBox_Type0.SelectedItem is Type0);
        }

        private bool CanRemoveType0Keyframe()
        {
            return (dataGrid_type0_Keyframes.SelectedItem is Type0_Keyframe) && (comboBox_Type0.SelectedItem is Type0);
        }

        //Animation Tab (Type 1)
        private void AddType1Header_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ParticleEffect _particleEffect = empTree.SelectedItem as ParticleEffect;
                if (_particleEffect == null) return;

                var newHeader = Type1_Header.GetNew();
                _particleEffect.Type_1.Add(newHeader);
                comboBox_Type1_Header.SelectedItem = newHeader;

                UndoManager.Instance.AddUndo(new UndoableListAdd<Type1_Header>(_particleEffect.Type_1, newHeader, "New Anim Group"));
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemoveType1Header_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ParticleEffect _particleEffect = empTree.SelectedItem as ParticleEffect;
                Type1_Header _type1_Header = comboBox_Type1_Header.SelectedItem as Type1_Header;
                if (_particleEffect == null) return;
                if (_type1_Header == null) return;

                UndoManager.Instance.AddUndo(new UndoableListRemove<Type1_Header>(_particleEffect.Type_1, _type1_Header, "Remove Anim Group"));
                _particleEffect.Type_1.Remove(_type1_Header);

                if (_particleEffect.Type_1.Count > 0)
                {
                    comboBox_Type1_Header.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void comboBox_Type1_Header_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (empTree.SelectedItem != null)
                {
                    ParticleEffect particleEffect = empTree.SelectedItem as ParticleEffect;

                    if (particleEffect.Type_1.Count > 0 && comboBox_Type1_Header.SelectedIndex != -1)
                    {
                        if (particleEffect.Type_1[0].Entries.Count > 0 && comboBox_Type1.SelectedIndex == -1)
                        {
                            comboBox_Type1.SelectedIndex = 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddNewAnimationType1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ParticleEffect _particleEffect = empTree.SelectedItem as ParticleEffect;
                var _type1_header = comboBox_Type1_Header.SelectedItem as Type1_Header;
                if (_particleEffect == null) return;
                if (_type1_header == null) return;

                var newAnimation = Type0.GetNew();
                _type1_header.Entries.Add(newAnimation);
                comboBox_Type1.SelectedItem = newAnimation;

                UndoManager.Instance.AddUndo(new UndoableListAdd<Type0>(_type1_header.Entries, newAnimation, "New Animation"));
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemoveAnimationType1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ParticleEffect _particleEffect = empTree.SelectedItem as ParticleEffect;
                var _type1_header = comboBox_Type1_Header.SelectedItem as Type1_Header;
                Type0 _type0 = comboBox_Type1.SelectedItem as Type0;
                if (_particleEffect == null) return;
                if (_type0 == null) return;
                if (_type1_header == null) return;

                UndoManager.Instance.AddUndo(new UndoableListRemove<Type0>(_type1_header.Entries, _type0, "Remove Animation"));
                _type1_header.Entries.Remove(_type0);


                if (_particleEffect.Type_1.Count > 0)
                {
                    comboBox_Type1.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }


        public RelayCommand AddType1KeyframeCommand => new RelayCommand(AddType1Keyframe, CanAddType1Keyframe);
        private void AddType1Keyframe()
        {
            Type0 type0 = comboBox_Type1.SelectedItem as Type0;

            if (type0 != null)
            {
                Type0_Keyframe keyframe = new Type0_Keyframe();
                UndoManager.Instance.AddUndo(new UndoableListAdd<Type0_Keyframe>(type0.Keyframes, keyframe, "Add Keyframe"));
                type0.Keyframes.Add(keyframe);
            }
        }

        public RelayCommand RemoveType1KeyframeCommand => new RelayCommand(RemoveType1Keyframe, CanRemoveType1Keyframe);
        private void RemoveType1Keyframe()
        {
            Type0 type0 = comboBox_Type1.SelectedItem as Type0;
            Type0_Keyframe keyframe = dataGrid_type1_Keyframes.SelectedItem as Type0_Keyframe;

            if (keyframe != null && type0 != null)
            {
                if (type0.Keyframes.Contains(keyframe))
                {
                    UndoManager.Instance.AddUndo(new UndoableListRemove<Type0_Keyframe>(type0.Keyframes, keyframe, "Remove Keyframe"));
                    type0.Keyframes.Remove(keyframe);
                }
            }
        }

        private bool CanAddType1Keyframe()
        {
            return (comboBox_Type1.SelectedItem is Type0);
        }

        private bool CanRemoveType1Keyframe()
        {
            return (dataGrid_type1_Keyframes.SelectedItem is Type0_Keyframe) && (comboBox_Type1.SelectedItem is Type0);
        }

        //Struct3 (Cone Extrude)
        public RelayCommand AddConeExtrude_Command => new RelayCommand(AddConeExtrudeEntry);
        private void AddConeExtrudeEntry()
        {
            if (empFile == null) return;

            if(empTree.SelectedItem is ParticleEffect particleEffect)
            {
                Struct3_Entries entry = new Struct3_Entries();
                particleEffect.Type_Struct3.FloatList.Add(entry);
                UndoManager.Instance.AddUndo(new UndoableListAdd<Struct3_Entries>(particleEffect.Type_Struct3.FloatList, entry, "Add ConeExtrude Entry"));
            }
        }

        public RelayCommand RemoveConeExtrude_Command => new RelayCommand(RemoveConeExtrudeEntry);
        private void RemoveConeExtrudeEntry()
        {
            if (empFile == null) return;

            if (dataGrid_coneExtrude.SelectedItem is Struct3_Entries selectedEntry && empTree.SelectedItem is ParticleEffect particleEffect)
            {
                UndoManager.Instance.AddUndo(new UndoableListRemove<Struct3_Entries>(particleEffect.Type_Struct3.FloatList, selectedEntry, "Remove ConeExtrude Entry"));
                particleEffect.Type_Struct3.FloatList.Remove(selectedEntry);
            }
        }

        //Shape Draw
        public RelayCommand AddShapeDraw_Command => new RelayCommand(AddShapeDrawEntry);
        private void AddShapeDrawEntry()
        {
            if (empFile == null) return;

            if (empTree.SelectedItem is ParticleEffect particleEffect)
            {
                Struct5_Entries entry = new Struct5_Entries();
                particleEffect.Type_Struct5.FloatList.Add(entry);
                UndoManager.Instance.AddUndo(new UndoableListAdd<Struct5_Entries>(particleEffect.Type_Struct5.FloatList, entry, "Add ShapeDraw Entry"));
            }
        }

        public RelayCommand RemoveShapeDraw_Command => new RelayCommand(RemoveShapeDrawEntry);
        private void RemoveShapeDrawEntry()
        {
            if (empFile == null) return;

            if (dataGrid_ShapeDraw.SelectedItem is Struct5_Entries selectedEntry && empTree.SelectedItem is ParticleEffect particleEffect)
            {
                UndoManager.Instance.AddUndo(new UndoableListRemove<Struct5_Entries>(particleEffect.Type_Struct5.FloatList, selectedEntry, "Remove ShapeDraw Entry"));
                particleEffect.Type_Struct5.FloatList.Remove(selectedEntry);
            }
        }

        //Other
        private void empTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            try
            {
                if (empTree.SelectedItem != null)
                {
                    ParticleEffect particleEffect = empTree.SelectedItem as ParticleEffect;

                    if (particleEffect.Type_0.Count > 0 && comboBox_Type0.SelectedIndex == -1)
                    {
                        comboBox_Type0.SelectedIndex = 0;
                    }
                    if (particleEffect.Type_1.Count > 0 && comboBox_Type1_Header.SelectedIndex == -1)
                    {
                        comboBox_Type1_Header.SelectedIndex = 0;

                        if (particleEffect.Type_1[0].Entries.Count > 0 && comboBox_Type1.SelectedIndex == -1)
                        {
                            comboBox_Type1.SelectedIndex = 0;
                        }
                    }

                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            //Reset selected items for ComboBoxes

        }

        private void ColorCanvas_Color1_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            //System.Windows.MessageBox.Show(String.Format("R = {0}, G = {1}, B = {2}, A = {3}", ColorCanvas_Color1.R.ToString(), ColorCanvas_Color1.G.ToString(), ColorCanvas_Color1.B.ToString(), ColorCanvas_Color1.A.ToString()));
        }



        //Texture Tab
        public RelayCommand TextureRemove_Command => new RelayCommand(TextureRemove, IsTextureSelected);
        private void TextureRemove()
        {
            try
            {
                if (listBox_Textures.SelectedItem == null || comboBox_TextureType.SelectedIndex == -1) return;

                List<EMP_TextureDefinition> selectedTextures = listBox_Textures.SelectedItems.Cast<EMP_TextureDefinition>().ToList();
                if (selectedTextures == null) return;
                List<IUndoRedo> undos = new List<IUndoRedo>();

                List<TexturePart> textureInstances = new List<TexturePart>();

                foreach (var texture in selectedTextures)
                {
                    textureInstances.AddRange(empFile.GetTexturePartsThatUseEmbEntryRef(texture));
                }

                if (MessageBox.Show(String.Format("The selected texture(s) will be deleted and all references to them on {0} Particle Effects in this EMP will be removed.\n\nContinue?", textureInstances.Count, selectedTextures.Count), "Delete", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    foreach (var texture in selectedTextures)
                    {
                        empFile.RemoveTextureReferences(texture, undos);
                        undos.Add(new UndoableListRemove<EMP_TextureDefinition>(empFile.Textures, texture));
                        empFile.Textures.Remove(texture);
                    }

                    UndoManager.Instance.AddCompositeUndo(undos, selectedTextures.Count > 1 ? "Delete Textures (EMP)" : "Delete Texture (EMP)");
                }
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        public RelayCommand TextureAdd_Command => new RelayCommand(TextureAdd);
        private void TextureAdd()
        {
            try
            {
                if (empFile == null) return;
                if (empFile.Textures == null) empFile.Textures = AsyncObservableCollection<EMP_TextureDefinition>.Create();

                var newTexture = EMP_TextureDefinition.GetNew();
                newTexture.EntryIndex = empFile.NextTextureId();

                empFile.Textures.Add(newTexture);
                listBox_Textures.SelectedItem = newTexture;
                listBox_Textures.ScrollIntoView(newTexture);

                UndoManager.Instance.AddUndo(new UndoableListAdd<EMP_TextureDefinition>(empFile.Textures, newTexture, "Add Texture (EMP)"));
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public RelayCommand TextureTab_ChangeTexture_Command => new RelayCommand(TextureTab_ChangeTexture, IsTextureSelected);
        private void TextureTab_ChangeTexture()
        {
            try
            {
                var _selectedTextureEntry = listBox_Textures.SelectedItem as Xv2CoreLib.EMP.EMP_TextureDefinition;
                if (_selectedTextureEntry == null) return;

                var previousTextureRef = _selectedTextureEntry.TextureRef;

                var textureSelector = new TextureSelector(textureContainer, this, previousTextureRef);
                textureSelector.ShowDialog();

                if (textureSelector.SelectedTexture != null)
                {
                    List<IUndoRedo> undos = new List<IUndoRedo>();
                    undos.Add(new UndoableProperty<EMP_TextureDefinition>(nameof(EMP_TextureDefinition.TextureRef), _selectedTextureEntry, _selectedTextureEntry.TextureRef, textureSelector.SelectedTexture));
                    _selectedTextureEntry.TextureRef = textureSelector.SelectedTexture;

                    //Get number of other Texture Entries that use the previous texture ref, and ask if user wants to change those as well
                    if (previousTextureRef != null)
                    {
                        var textureEntriesThatUsedSameRef = empFile.GetTextureEntriesThatUseRef(previousTextureRef);

                        if (textureEntriesThatUsedSameRef.Count > 0)
                        {
                            if (MessageBox.Show(String.Format("Do you also want to change the {0} other texture entries in this EMP that uses \"{1}\" to use \"{2}\"?.", textureEntriesThatUsedSameRef.Count, previousTextureRef.Name, textureSelector.SelectedTexture.Name), "Change Texture", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                            {
                                foreach (var texture in textureEntriesThatUsedSameRef)
                                {
                                    undos.Add(new UndoableProperty<EMP_TextureDefinition>(nameof(EMP_TextureDefinition.TextureRef), texture, texture.TextureRef, textureSelector.SelectedTexture));
                                    texture.TextureRef = textureSelector.SelectedTexture;
                                }
                            }
                        }
                    }

                    UndoManager.Instance.AddCompositeUndo(undos, "Change Texture (EMP)");
                }

            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public RelayCommand TextureTab_GoToTexture_Command => new RelayCommand(TextureTab_GoToTexture, IsTextureSelected);
        private void TextureTab_GoToTexture()
        {
            try
            {
                var texture = listBox_Textures.SelectedItem as EMP_TextureDefinition;

                if (texture != null)
                {
                    if (texture.TextureRef != null)
                    {
                        EmbEditForm embForm = mainWindow.PBIND_OpenTextureViewer();
                        embForm.listBox_Textures.SelectedItem = texture.TextureRef;
                        embForm.listBox_Textures.ScrollIntoView(texture.TextureRef);
                    }
                }
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public RelayCommand TextureTab_RemoveTexture_Command => new RelayCommand(TextureTab_RemoveTexture, IsTextureSelected);
        private void TextureTab_RemoveTexture()
        {
            try
            {
                var texture = listBox_Textures.SelectedItem as EMP_TextureDefinition;

                if (texture != null)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<EMP_TextureDefinition>(nameof(EMP_TextureDefinition.TextureRef), texture, texture.TextureRef, null, "Remove Texture (EMP)"));
                    texture.TextureRef = null;
                }
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool IsTextureSelected()
        {
            return listBox_Textures.SelectedItem is EMP_TextureDefinition;
        }

        //Material
        private void Material_ChangeMaterial_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var particleEffect = empTree.SelectedItem as ParticleEffect;
                if (particleEffect == null) return;

                var previousMaterialRef = particleEffect.Type_Texture.MaterialRef;

                var materialSelector = new MaterialSelector(materialFile, this, previousMaterialRef);
                materialSelector.ShowDialog();

                if (materialSelector.SelectedMaterial != null)
                {
                    List<IUndoRedo> undos = new List<IUndoRedo>();
                    undos.Add(new UndoableProperty<TexturePart>(nameof(TexturePart.MaterialRef), particleEffect.Type_Texture, particleEffect.Type_Texture.MaterialRef, materialSelector.SelectedMaterial));
                    particleEffect.Type_Texture.MaterialRef = materialSelector.SelectedMaterial;

                    //Get number of other Material Entries that use the previous material ref, and ask if user wants to change those as well

                    var texturePartsThatUsedSameRef = empFile.GetTexturePartsThatUseMaterialRef(previousMaterialRef);

                    if (texturePartsThatUsedSameRef.Count > 0)
                    {
                        if (MessageBox.Show(String.Format("Do you also want to change the {0} other TextureParts in this EMP that uses \"{1}\" to use \"{2}\"?.", texturePartsThatUsedSameRef.Count, previousMaterialRef.Str_00, materialSelector.SelectedMaterial.Str_00), "Change Texture", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        {
                            foreach (var texturePart in texturePartsThatUsedSameRef)
                            {
                                undos.Add(new UndoableProperty<TexturePart>(nameof(TexturePart.MaterialRef), texturePart, texturePart.MaterialRef, materialSelector.SelectedMaterial));
                                texturePart.MaterialRef = materialSelector.SelectedMaterial;
                            }
                        }
                    }

                    UndoManager.Instance.AddCompositeUndo(undos, "Change Material");
                }
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }


        }

        private void Material_Goto_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var particleEffect = empTree.SelectedItem as ParticleEffect;

                if (particleEffect != null)
                {
                    if (particleEffect.Type_Texture.MaterialRef != null)
                    {
                        EmmEditForm emmForm = mainWindow.PBIND_OpenMaterialEditor();
                        emmForm.dataGrid.SelectedItem = particleEffect.Type_Texture.MaterialRef;
                        emmForm.dataGrid.ScrollIntoView(particleEffect.Type_Texture.MaterialRef);
                    }
                }
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Material_RemoveMaterialReference_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var particleEffect = empTree.SelectedItem as ParticleEffect;

                if (particleEffect != null)
                {
                    UndoManager.Instance.AddUndo(new UndoableProperty<TexturePart>(nameof(TexturePart.MaterialRef), particleEffect.Type_Texture, particleEffect.Type_Texture.MaterialRef, null, "Remove Material"));
                    particleEffect.Type_Texture.MaterialRef = null;
                }
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //TexturePart
        public RelayCommand TexturePart_AddTextureCommand => new RelayCommand(TexturePart_AddTextureReference);

        private void TexturePart_AddTextureReference()
        {
            try
            {
                var particleEffect = empTree.SelectedItem as ParticleEffect;
                if (particleEffect == null) return;

                var newTexture = new TextureEntryRef();
                UndoManager.Instance.AddUndo(new UndoableListAdd<TextureEntryRef>(particleEffect.Type_Texture.TextureEntryRef, newTexture, "Add Texture Ref"));
                particleEffect.Type_Texture.TextureEntryRef.Add(newTexture);
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        public RelayCommand TexturePart_RemoveTextureCommand => new RelayCommand(TexturePart_RemoveTextureReference, CanRemoveTextureRef);

        private void TexturePart_RemoveTextureReference()
        {
            try
            {
                var particleEffect = empTree.SelectedItem as ParticleEffect;
                if (particleEffect == null) return;

                if (textureRefDataGrid.SelectedItem != null)
                {
                    var texture = textureRefDataGrid.SelectedItem as TextureEntryRef;

                    if (texture != null)
                    {
                        UndoManager.Instance.AddUndo(new UndoableListRemove<TextureEntryRef>(particleEffect.Type_Texture.TextureEntryRef, texture, "Remove Texture Ref"));
                        particleEffect.Type_Texture.TextureEntryRef.Remove(texture);
                    }
                }
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanRemoveTextureRef()
        {
            return (textureRefDataGrid.SelectedItem is TextureEntryRef);
        }


        //Texture Tab > Type2 subdata
        private void button_TextureType2_AddKeyFrame_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (listBox_Textures.SelectedItem == null || comboBox_TextureType.SelectedIndex == -1) return;

                EMP_TextureDefinition texture = listBox_Textures.SelectedItem as EMP_TextureDefinition;
                var newData = new SubData_2_Entry();
                UndoManager.Instance.AddUndo(new UndoableListAdd<SubData_2_Entry>(texture.SubData2.Keyframes, newData, "New Spritesheet Keyframe"));
                texture.SubData2.Keyframes.Add(newData);
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void button_TextureType2_RemoveKeyframe_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (listBox_Textures.SelectedItem == null || comboBox_TextureType.SelectedIndex == -1) return;
                if (dataGrid_ScrollKeyframes.SelectedItem == null) return;

                EMP_TextureDefinition texture = listBox_Textures.SelectedItem as EMP_TextureDefinition;
                SubData_2_Entry keyframe = dataGrid_ScrollKeyframes.SelectedItem as SubData_2_Entry;

                if (texture.SubData2.Keyframes.Count == 1)
                {
                    MessageBox.Show("Cannot delete the last keyframe.", "Delete", MessageBoxButton.OK, MessageBoxImage.Stop);
                    return;
                }

                UndoManager.Instance.AddUndo(new UndoableListRemove<SubData_2_Entry>(texture.SubData2.Keyframes, keyframe, "Remove Spritesheet Keyframe"));
                texture.SubData2.Keyframes.Remove(keyframe);
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void ComboBox_Type0_Parameter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        //Texture Context Menu
        public RelayCommand TextureContextMenu_Copy_Command => new RelayCommand(TextureContextMenu_Copy, IsTextureSelected);
        private void TextureContextMenu_Copy()
        {
            try
            {
                List<EMP_TextureDefinition> selectedTextures = listBox_Textures.SelectedItems.Cast<EMP_TextureDefinition>().ToList();

                if (selectedTextures.Count > 0)
                {
                    textureContainer.SaveDdsImages();
                    Clipboard.SetData(ClipboardDataTypes.EmpTextureEntry, selectedTextures);
                }
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public RelayCommand TextureContextMenu_Paste_Command => new RelayCommand(TextureContextMenu_Paste, CanPasteEmpTexture);
        private void TextureContextMenu_Paste()
        {
            try
            {
                Texture_PasteTextures();
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Texture_PasteTextures()
        {
            List<EMP_TextureDefinition> embEntry = (List<EMP_TextureDefinition>)Clipboard.GetData(ClipboardDataTypes.EmpTextureEntry);

            if (embEntry != null)
            {
                List<IUndoRedo> undos = new List<IUndoRedo>();

                foreach (var textureEntry in embEntry)
                {
                    var newTexture = textureEntry.Clone();
                    var newEmbEntry = textureContainer.Add(newTexture.TextureRef, undos);
                    newTexture.TextureRef = newEmbEntry;

                    newTexture.EntryIndex = empFile.NextTextureId();
                    empFile.Textures.Add(newTexture);

                    undos.Add(new UndoableListAdd<EMP_TextureDefinition>(empFile.Textures, newTexture));
                }

                UndoManager.Instance.AddCompositeUndo(undos, embEntry.Count > 1 ? "Paste Textures" : "Paste Texture");
            }
        }

        public RelayCommand TextureContextMenu_Duplicate_Command => new RelayCommand(TextureContextMenu_Duplicate, IsTextureSelected);
        private void TextureContextMenu_Duplicate()
        {
            try
            {
                var selectedTexture = listBox_Textures.SelectedItem as EMP_TextureDefinition;

                if (selectedTexture != null)
                {
                    var newTexture = selectedTexture.Clone();
                    newTexture.EntryIndex = empFile.NextTextureId();
                    UndoManager.Instance.AddUndo(new UndoableListAdd<EMP_TextureDefinition>(empFile.Textures, newTexture, "Duplicate Texture (EMP)"));
                    empFile.Textures.Add(newTexture);
                }
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public RelayCommand TextureContextMenu_PasteValues_Command => new RelayCommand(TextureContextMenu_PasteValues, CanPasteEmpTexture);
        private void TextureContextMenu_PasteValues()
        {
            try
            {
                List<EMP_TextureDefinition> textures = (List<EMP_TextureDefinition>)Clipboard.GetData(ClipboardDataTypes.EmpTextureEntry);

                if (textures != null && listBox_Textures.SelectedItem != null)
                {
                    List<IUndoRedo> undos = new List<IUndoRedo>();

                    if (textures.Count > 0)
                    {
                        var newTexture = textures[0].Clone();
                        newTexture.TextureRef = textureContainer.Add(newTexture.TextureRef, undos);
                        var selectedItem = listBox_Textures.SelectedItem as EMP_TextureDefinition;

                        if (selectedItem != null)
                        {
                            selectedItem.ReplaceValues(newTexture, undos);
                            UndoManager.Instance.AddCompositeUndo(undos, "Paste Values (EMP)");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public RelayCommand TextureContextMenu_Delete_Command => new RelayCommand(TextureContextMenu_Delete, IsTextureSelected);
        private void TextureContextMenu_Delete()
        {
            try
            {
                TextureRemove();
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public RelayCommand TextureContextMenu_Merge_Command => new RelayCommand(TextureContextMenu_Merge, CanMergeEmpTextures);
        private void TextureContextMenu_Merge()
        {
            try
            {
                if (listBox_Textures.SelectedItems.Count > 0)
                {
                    var texture = listBox_Textures.SelectedItem as EMP_TextureDefinition;
                    List<EMP_TextureDefinition> selectedTextures = listBox_Textures.SelectedItems.Cast<EMP_TextureDefinition>().ToList();

                    selectedTextures.Remove(texture);

                    if (texture != null && selectedTextures.Count > 0)
                    {
                        List<IUndoRedo> undos = new List<IUndoRedo>();
                        int count = selectedTextures.Count + 1;

                        if (MessageBox.Show(string.Format("All currently selected textures will be MERGED into {0}.\n\nAll other selected textures will be deleted, with all references to them changed to {0}.\n\nDo you wish to continue?", texture.ToolName), string.Format("Merge ({0} textures)", count), MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                        {
                            foreach (var textureToRemove in selectedTextures)
                            {
                                empFile.RefactorTextureRef(textureToRemove, texture, undos);
                                undos.Add(new UndoableListRemove<EMP_TextureDefinition>(empFile.Textures, textureToRemove));
                                empFile.Textures.Remove(textureToRemove);
                            }

                            UndoManager.Instance.AddCompositeUndo(undos, "Merge Textures (EMP)");
                        }
                    }
                    else
                    {
                        MessageBox.Show("Cannot merge with less than 2 textures selected.\n\nTip: Use Left Ctrl + Left Mouse Click to multi-select.", "Merge", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }

                }

            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, SettingsManager.Instance.GetErrorLogPath()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private bool CanPasteEmpTexture()
        {
            return Clipboard.ContainsData(ClipboardDataTypes.EmpTextureEntry);
        }

        private bool CanMergeEmpTextures()
        {
            return listBox_Textures.SelectedItems.Count >= 2;
        }

        private void ComboBox_TextureType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedTexture = listBox_Textures.SelectedItem as EMP_TextureDefinition;

            try
            {
                if (selectedTexture != null)
                {
                    if (selectedTexture.SubData2.Keyframes == null)
                    {
                        selectedTexture.SubData2.Keyframes = AsyncObservableCollection<SubData_2_Entry>.Create();
                        selectedTexture.SubData2.Keyframes.Add(new SubData_2_Entry());
                    }

                    if (selectedTexture.SubData2.Keyframes.Count == 0)
                    {
                        selectedTexture.SubData2.Keyframes.Add(new SubData_2_Entry());
                    }
                }
            }
            catch
            {

            }
        }

        private void SetHideStatus(ParticleEffect particleEffect, bool hideStatus)
        {
            particleEffect.I_33_3 = hideStatus;

            if(particleEffect.ChildParticleEffects != null)
            {
                foreach(var child in particleEffect.ChildParticleEffects)
                {
                    SetHideStatus(child, hideStatus);
                }
            }
        }

    }
}
