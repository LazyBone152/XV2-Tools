using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Xv2CoreLib.EMM;
using Xv2CoreLib.EMP;
using EEPK_Organiser.Misc;
using System.Collections.Generic;
using System.Linq;

namespace EEPK_Organiser.Forms.EMP
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class EMP_Editor : Window, INotifyPropertyChanged
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
        private MainWindow mainWindow = null;

        public EMP_Editor(EMP_File _empFile, string empName, Xv2CoreLib.EMB_CLASS.EMB_File _textureContainer, EMM_File _materialFile, MainWindow _mainWindow)
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

        //ParticleEffect (general)
        private void empTree_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.C))
            {
                //Copy
                Copy_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.LeftAlt) && Keyboard.IsKeyDown(Key.V))
            {
                //Paste (Child)
                PasteChild_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.V))
            {
                //Paste
                Paste_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.LeftShift) && Keyboard.IsKeyDown(Key.V))
            {
                //Paste Values
                PasteValues_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.Up))
            {
                //Move Up
                MoveUp_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.Down))
            {
                //Move Down
                MoveDown_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.Delete))
            {
                //Remove
                RemoveParticleEffect_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.N))
            {
                //Add New
                AddParticleEffect_Click(null, null);
                e.Handled = true;
            }
        }

        private void AddParticleEffect_Click(object sender, RoutedEventArgs e)
        {
            if (empFile == null) return;

            try
            {
                empFile.AddNew();
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void AddParticleEffectAsChild_Click(object sender, RoutedEventArgs e)
        {
            if (empFile == null) return;

            try
            {
                ParticleEffect _particleEffect = empTree.SelectedItem as ParticleEffect;
                if (_particleEffect == null) return;
                _particleEffect.AddNew();
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemoveParticleEffect_Click(object sender, RoutedEventArgs e)
        {

            if (empFile == null) return;
            ParticleEffect _particleEffect = empTree.SelectedItem as ParticleEffect;
            if (_particleEffect == null) return;
            //undoManager.CreateUndoPoint();
            empFile.RemoveParticleEffect(_particleEffect);

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

        //TreeView Context Menu
        private void AddNewAbove_Click(object sender, RoutedEventArgs e)
        {
            ParticleEffect _particleEffect = empTree.SelectedItem as ParticleEffect;
            if (_particleEffect == null) return;
            ObservableCollection<ParticleEffect> parentList = empFile.GetParentList(_particleEffect);

            if(parentList != null)
            {
                int index = parentList.IndexOf(_particleEffect);
                parentList.Insert(index, ParticleEffect.GetNew());
            }

        }

        private void AddNewBelow_Click(object sender, RoutedEventArgs e)
        {
            ParticleEffect _particleEffect = empTree.SelectedItem as ParticleEffect;
            if (_particleEffect == null) return;
            ObservableCollection<ParticleEffect> parentList = empFile.GetParentList(_particleEffect);

            if (parentList != null)
            {
                int index = parentList.IndexOf(_particleEffect);
                parentList.Insert(index+1, ParticleEffect.GetNew());
            }
        }

        private void MoveUp_Click(object sender, RoutedEventArgs e)
        {
            ParticleEffect _particleEffect = empTree.SelectedItem as ParticleEffect;
            if (_particleEffect == null) return;

            try
            {
                ObservableCollection<ParticleEffect> parentList = empFile.GetParentList(_particleEffect);

                if (parentList != null)
                {
                    int oldIndex = parentList.IndexOf(_particleEffect);
                    //if (oldIndex <= parentList.Count - 1) return;
                    if (oldIndex == 0) return;
                    parentList.Move(oldIndex, oldIndex - 1);
                }
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MoveDown_Click(object sender, RoutedEventArgs e)
        {
            ParticleEffect _particleEffect = empTree.SelectedItem as ParticleEffect;

            try
            {
                if (_particleEffect == null) return;
                ObservableCollection<ParticleEffect> parentList = empFile.GetParentList(_particleEffect);

                if (parentList != null)
                {
                    int oldIndex = parentList.IndexOf(_particleEffect);
                    if (oldIndex >= parentList.Count - 1) return;
                    parentList.Move(oldIndex, oldIndex + 1);
                }
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void Copy_Click(object sender, RoutedEventArgs e)
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Paste_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ParticleEffect particleEffect = (ParticleEffect)Clipboard.GetData(ClipboardDataTypes.EmpParticleEffect);

                if (particleEffect == null) return;

                //Import texture entries
                ParticleEffect newParticleEffect = particleEffect;

                ImportTextureEntries(newParticleEffect);
                ImportMaterialEntries(newParticleEffect);

                 //Paste particleEffect
                 ParticleEffect _particleEffect = empTree.SelectedItem as ParticleEffect;

                //If selectedParticleEffect is null, we paste it at the root level
                if (_particleEffect == null)
                {
                    empFile.ParticleEffects.Add(newParticleEffect);
                }

                //Else we place it at the level of the selected particleEffect
                ObservableCollection<ParticleEffect> parentList = empFile.GetParentList(_particleEffect);

                if (parentList != null)
                {
                    parentList.Add(newParticleEffect);
                }
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            }
        }

        private void PasteChild_Click(object sender, RoutedEventArgs e)
        {
            ParticleEffect _particleEffect = empTree.SelectedItem as ParticleEffect;
            if (_particleEffect == null) return;

            try
            {
                ParticleEffect particleEffect = (ParticleEffect)Clipboard.GetData(ClipboardDataTypes.EmpParticleEffect);

                if (particleEffect == null) return;
                ParticleEffect newParticleEffect = particleEffect.Clone();

                ImportTextureEntries(newParticleEffect);
                ImportMaterialEntries(newParticleEffect);

                _particleEffect.ChildParticleEffects.Add(newParticleEffect);
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
        }
        
        private void PasteValues_Click(object sender, RoutedEventArgs e)
        {
            ParticleEffect selectedParticleEffect = empTree.SelectedItem as ParticleEffect;
            if (selectedParticleEffect == null) return;

            try
            {
                ParticleEffect copiedParticleEffect = (ParticleEffect)Clipboard.GetData(ClipboardDataTypes.EmpParticleEffect);
                if (copiedParticleEffect == null) return;

                //Ensure the materials and textures exist
                ImportTextureEntries(copiedParticleEffect);
                ImportMaterialEntries(copiedParticleEffect);

                selectedParticleEffect.CopyValues(copiedParticleEffect);
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImportTextureEntries(ParticleEffect particleEffect)
        {
            //Add referenced texture entries if they are not found

            foreach (var texture in particleEffect.Type_Texture.TextureEntryRef)
            {
                var newTex = empFile.GetTexture(texture.TextureRef);

                if (newTex == null)
                {
                    texture.TextureRef.TextureRef = textureContainer.Add(texture.TextureRef.TextureRef);

                    //Add the texture
                    texture.TextureRef.EntryIndex = empFile.NextTextureId();
                    empFile.Textures.Add(texture.TextureRef);
                }
                else
                {
                    //An identical texture was found, so set that as the ref
                    texture.TextureRef = newTex;
                }
            }
            
            //Recursively call this method again if this ParticleEffect has children
            if(particleEffect.ChildParticleEffects != null)
            {
                foreach(var child in particleEffect.ChildParticleEffects)
                {
                    ImportTextureEntries(child);
                }
            }
        }

        private void ImportMaterialEntries(ParticleEffect particleEffect)
        {
            //Add referenced material entries if they are not found
            if(particleEffect.Type_Texture.MaterialRef != null)
            {

                if (!materialFile.Materials.Contains(particleEffect.Type_Texture.MaterialRef))
                {
                    var result = materialFile.Compare(particleEffect.Type_Texture.MaterialRef);
                    
                    if(result == null)
                    {
                        //Material didn't exist so we have to add it
                        particleEffect.Type_Texture.MaterialRef.Str_00 = materialFile.GetUnusedName(particleEffect.Type_Texture.MaterialRef.Str_00);
                        materialFile.Materials.Add(particleEffect.Type_Texture.MaterialRef);
                    }
                    else if(result != particleEffect.Type_Texture.MaterialRef)
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
                    ImportMaterialEntries(child);
                }
            }
        }
        
        private void HueAdjustment_Click(object sender, RoutedEventArgs e)
        {
#if !DEBUG
            try
#endif
            {
                ParticleEffect _particleEffect = empTree.SelectedItem as ParticleEffect;
                if (_particleEffect == null) return;

                Forms.RecolorAll recolor = new Forms.RecolorAll(_particleEffect, this);
                recolor.ShowDialog();
            }
#if !DEBUG
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
#endif
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
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                _particleEffect.Type_0.Remove(_type0);

                if (_particleEffect.Type_0.Count > 0)
                {
                    comboBox_Type0.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

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
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                _particleEffect.Type_1.Remove(_type1_Header);

                if (_particleEffect.Type_1.Count > 0)
                {
                    comboBox_Type1_Header.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                _type1_header.Entries.Remove(_type0);

                if (_particleEffect.Type_1.Count > 0)
                {
                    comboBox_Type1.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                }
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            //Reset selected items for ComboBoxes
            
        }

        private void ColorCanvas_Color1_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            //System.Windows.MessageBox.Show(String.Format("R = {0}, G = {1}, B = {2}, A = {3}", ColorCanvas_Color1.R.ToString(), ColorCanvas_Color1.G.ToString(), ColorCanvas_Color1.B.ToString(), ColorCanvas_Color1.A.ToString()));
        }

        

        //Texture Tab

        private void Button_TextureRemove_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (listBox_Textures.SelectedItem == null || comboBox_TextureType.SelectedIndex == -1) return;

                List<EMP_TextureDefinition> selectedTextures = listBox_Textures.SelectedItems.Cast<EMP_TextureDefinition>().ToList();
                if (selectedTextures == null) return;

                List<TexturePart> textureInstances = new List<TexturePart>();

                foreach(var texture in selectedTextures)
                {
                    textureInstances.AddRange(empFile.GetTexturePartsThatUseEmbEntryRef(texture));
                }

                if (MessageBox.Show(String.Format("The selected texture(s) will be deleted and all references to them on {0} Particle Effects in this EMP will be removed.\n\nContinue?", textureInstances.Count, selectedTextures.Count), "Delete", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    foreach(var texture in selectedTextures)
                    {
                        empFile.RemoveTextureReferences(texture);
                        empFile.Textures.Remove(texture);
                    }
                }
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void Button_TextureAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (empFile == null) return;
                if (empFile.Textures == null) empFile.Textures = new System.Collections.ObjectModel.ObservableCollection<EMP_TextureDefinition>();

                var newTexture = EMP_TextureDefinition.GetNew();
                newTexture.EntryIndex = empFile.NextTextureId();

                empFile.Textures.Add(newTexture);
                listBox_Textures.SelectedItem = newTexture;
                listBox_Textures.ScrollIntoView(newTexture);
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TextureTab_ChangeTexture_Click(object sender, RoutedEventArgs e)
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
                    _selectedTextureEntry.TextureRef = textureSelector.SelectedTexture;

                    //Get number of other Texture Entries that use the previous texture ref, and ask if user wants to change those as well
                    if(previousTextureRef != null)
                    {
                        var textureEntriesThatUsedSameRef = empFile.GetTextureEntriesThatUseRef(previousTextureRef);

                        if (textureEntriesThatUsedSameRef.Count > 0)
                        {
                            if (MessageBox.Show(String.Format("Do you also want to change the {0} other texture entries in this EMP that uses \"{1}\" to use \"{2}\"?.", textureEntriesThatUsedSameRef.Count, previousTextureRef.Name, textureSelector.SelectedTexture.Name), "Change Texture", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                            {
                                foreach (var texture in textureEntriesThatUsedSameRef)
                                {
                                    texture.TextureRef = textureSelector.SelectedTexture;
                                }
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TextureTab_GoToTexture_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var texture = listBox_Textures.SelectedItem as EMP_TextureDefinition;

                if (texture != null)
                {
                    if(texture.TextureRef != null)
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TextureTab_RemoveTexture_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var texture = listBox_Textures.SelectedItem as EMP_TextureDefinition;

                if (texture != null)
                {
                    texture.TextureRef = null;
                }
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                    particleEffect.Type_Texture.MaterialRef = materialSelector.SelectedMaterial;

                    //Get number of other Material Entries that use the previous material ref, and ask if user wants to change those as well

                    var texturePartsThatUsedSameRef = empFile.GetTexturePartsThatUseMaterialRef(previousMaterialRef);

                    if (texturePartsThatUsedSameRef.Count > 0)
                    {
                        if (MessageBox.Show(String.Format("Do you also want to change the {0} other TextureParts in this EMP that uses \"{1}\" to use \"{2}\"?.", texturePartsThatUsedSameRef.Count, previousMaterialRef.Str_00, materialSelector.SelectedMaterial.Str_00), "Change Texture", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        {
                            foreach (var texturePart in texturePartsThatUsedSameRef)
                            {
                                texturePart.MaterialRef = materialSelector.SelectedMaterial;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            

        }

        private void Material_Goto_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var particleEffect = empTree.SelectedItem as ParticleEffect;

                if (particleEffect != null)
                {
                    if(particleEffect.Type_Texture.MaterialRef != null)
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Material_RemoveMaterialReference_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var particleEffect = empTree.SelectedItem as ParticleEffect;

                if (particleEffect != null)
                {
                    particleEffect.Type_Texture.MaterialRef = null;
                }
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //TexturePart
        private void TexturePart_AddTextureReference_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var particleEffect = empTree.SelectedItem as ParticleEffect;
                if (particleEffect == null) return;

                particleEffect.Type_Texture.TextureEntryRef.Add(new TextureEntryRef());
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void TexturePart_RemoveTextureReference_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var particleEffect = empTree.SelectedItem as ParticleEffect;
                if (particleEffect == null) return;

                if (textureRefDataGrid.SelectedItem != null)
                {
                    particleEffect.Type_Texture.TextureEntryRef.Remove(textureRefDataGrid.SelectedItem as TextureEntryRef);
                }
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        

        //Texture Tab > Type2 subdata
        private void button_TextureType2_AddKeyFrame_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (listBox_Textures.SelectedItem == null || comboBox_TextureType.SelectedIndex == -1) return;

                EMP_TextureDefinition texture = listBox_Textures.SelectedItem as EMP_TextureDefinition;

                texture.SubData2.Keyframes.Add(new SubData_2_Entry());
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

                if(texture.SubData2.Keyframes.Count == 1)
                {
                    MessageBox.Show("Cannot delete the last keyframe.", "Delete", MessageBoxButton.OK, MessageBoxImage.Stop);
                    return;
                }

                texture.SubData2.Keyframes.Remove(keyframe);
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //Tool
        private void Tool_RecolorMultiSelect(object sender, RoutedEventArgs e)
        {
            try
            {
                Forms.EMP.RecolorAll recolorForm = new Forms.EMP.RecolorAll(empFile);
                recolorForm.ShowDialog();
                recolorForm = null;
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ComboBox_Type0_Parameter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        //Texture Context Menu
        private void TextureContextMenu_Copy_Click(object sender, RoutedEventArgs e)
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TextureContextMenu_Paste_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Texture_PasteTextures();
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Texture_PasteTextures()
        {

            List<EMP_TextureDefinition> embEntry = (List<EMP_TextureDefinition>)Clipboard.GetData(ClipboardDataTypes.EmpTextureEntry);

            if(embEntry != null)
            {
                foreach(var textureEntry in embEntry)
                {
                    var newTexture = textureEntry.Clone();

                    newTexture.TextureRef = textureContainer.Add(newTexture.TextureRef);

                    newTexture.EntryIndex = empFile.NextTextureId();
                    empFile.Textures.Add(newTexture);
                }
            }
        }

        private void TextureContextMenu_Duplicate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedTexture = listBox_Textures.SelectedItem as EMP_TextureDefinition;

                if (selectedTexture != null)
                {
                    var newTexture = selectedTexture.Clone();
                    newTexture.EntryIndex = empFile.NextTextureId();
                    empFile.Textures.Add(newTexture);
                }
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void TextureContextMenu_PasteValues_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                List<EMP_TextureDefinition> textures = (List<EMP_TextureDefinition>)Clipboard.GetData(ClipboardDataTypes.EmpTextureEntry);

                if (textures != null && listBox_Textures.SelectedItem != null)
                {
                    if(textures.Count > 0)
                    {
                        var newTexture = textures[0].Clone();
                        newTexture.TextureRef = textureContainer.Add(newTexture.TextureRef);
                        var selectedItem = listBox_Textures.SelectedItem as EMP_TextureDefinition;
                        selectedItem.ReplaceValues(newTexture);
                    }
                }
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void TextureContextMenu_Delete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button_TextureRemove_Click(null, null);
            }
            catch (Exception ex)
            {
                mainWindow.SaveExceptionLog(ex.ToString());
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void TextureContextMenu_Merge_Click(object sender, RoutedEventArgs e)
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
                        int count = selectedTextures.Count + 1;

                        if (MessageBox.Show(string.Format("All currently selected textures will be MERGED into {0}.\n\nAll other selected textures will be deleted, with all references to them changed to {0}.\n\nDo you wish to continue?", texture.ToolName), string.Format("Merge ({0} textures)", count), MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                        {
                            foreach (var textureToRemove in selectedTextures)
                            {
                                empFile.RefactorTextureRef(textureToRemove, texture);
                                empFile.Textures.Remove(textureToRemove);
                            }
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
                MessageBox.Show(String.Format("An error occured.\n\nDetails: {0}\n\nA log containing more details about the error was saved at \"{1}\".", ex.Message, GeneralInfo.ERROR_LOG_PATH), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void ListBox_Textures_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.V))
            {
                //Paste
                TextureContextMenu_Paste_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.LeftShift) && Keyboard.IsKeyDown(Key.V))
            {
                //Paste Values
                TextureContextMenu_PasteValues_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.C))
            {
                //Copy
                TextureContextMenu_Copy_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.D))
            {
                //Duplicate
                TextureContextMenu_Duplicate_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.Delete))
            {
                //Delete
                Button_TextureRemove_Click(null, null);
                e.Handled = true;
            }
            else if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.N))
            {
                //New
                Button_TextureAdd_Click(null, null);
                e.Handled = true;
            }
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
                        selectedTexture.SubData2.Keyframes = new ObservableCollection<SubData_2_Entry>() { new SubData_2_Entry() };
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
        

    }
}
