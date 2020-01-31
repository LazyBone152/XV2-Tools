using Microsoft.Win32;
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
using System.Windows.Navigation;
using UsefulThings.WPF;
using System.Diagnostics;
using System.Windows.Media.Animation;
using System.IO;
using System.Windows.Threading;


namespace CSharpImageLibrary
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ViewModel vm = new ViewModel();

        const double duration = 0.6;
        const double labelDuration = 0.3;
        GridLengthAnimation GridOpeningAnim = new GridLengthAnimation();
        GridLengthAnimation GridClosingAnim = new GridLengthAnimation();
        DoubleAnimation WindowOpeningAnim = new DoubleAnimation(1137.211, TimeSpan.FromSeconds(duration));
        DoubleAnimation WindowClosingAnim = new DoubleAnimation(600, TimeSpan.FromSeconds(duration));

        DoubleAnimation SaveMessageOpen = new DoubleAnimation(1, TimeSpan.FromSeconds(labelDuration));
        DoubleAnimation SaveMessageClose = new DoubleAnimation(0, TimeSpan.FromSeconds(labelDuration));
        Dispatcher mainDispatcher = null;

        bool isOpen = false;

        public MainWindow()
        {
            
            InitializeComponent();
            RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.Fant);
            mainDispatcher = this.Dispatcher;
            DataContext = vm;

            GridOpeningAnim.Duration = TimeSpan.FromSeconds(duration);
            GridClosingAnim.Duration = TimeSpan.FromSeconds(duration);

            GridOpeningAnim.From = new GridLength(0, GridUnitType.Star);
            GridOpeningAnim.To = new GridLength(563, GridUnitType.Star);

            GridClosingAnim.From = new GridLength(563, GridUnitType.Star);
            GridClosingAnim.To = new GridLength(0, GridUnitType.Star);

            
            QuarticEase easer = new QuarticEase();
            easer.EasingMode = EasingMode.EaseOut;

            GridOpeningAnim.EasingFunction = easer;
            GridClosingAnim.EasingFunction = easer;
            WindowOpeningAnim.EasingFunction = easer;
            WindowClosingAnim.EasingFunction = easer;
            SaveMessageClose.EasingFunction = easer;
            SaveMessageOpen.EasingFunction = easer;

            ThisWindow.BeginAnimation(Window.WidthProperty, WindowClosingAnim);
            SaveColumn.BeginAnimation(ColumnDefinition.WidthProperty, GridClosingAnim);
            SuccessfulSaveMessage.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, SaveMessageClose);
            FailedSaveMessage.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, SaveMessageClose);

            vm.PropertyChanged += (source, args) =>
             {
                 if (args.PropertyName == "SaveSuccess")
                     mainDispatcher.BeginInvoke(new Action(() => ChangeSaveMessageVisibility(vm.SaveSuccess)));
             };
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.PageDown)
            {
                vm.MipIndex++;
                e.Handled = true;
            }
            else if (e.Key == Key.PageUp)
            {
                vm.MipIndex--;
                e.Handled = true;
            }
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            ChangeConvertPanel(false);
            isOpen = false;

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Supported Image Files|*.dds;*.jpg;*.png;*.jpeg;*.bmp;*.tga";
            ofd.Title = "Select image to load";
            if (ofd.ShowDialog() == true)
                vm.LoadImage(ofd.FileName);
        }

        private void FormatComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            vm.GenerateSavePreview();
            vm.SavePath = vm.GetAutoSavePath((ImageEngineFormat)e.AddedItems[0]);
        }

        private void OpenConvertPanel_Click(object sender, RoutedEventArgs e)
        {
            if (vm.img != null && !isOpen)  // only change stuff when opening
            {
                vm.SaveFormat = vm.img.Format.SurfaceFormat;
                vm.SavePath = vm.GetAutoSavePath(vm.img.Format.SurfaceFormat);
            }

            ChangeConvertPanel(!isOpen);
            isOpen = !isOpen;

        }

        private void ChangeConvertPanel(bool toState)
        {
            if (isOpen == toState)
                return;

            if (!toState)
            {
                ThisWindow.BeginAnimation(Window.WidthProperty, WindowClosingAnim);
                SaveColumn.BeginAnimation(ColumnDefinition.WidthProperty, GridClosingAnim);
            }
            else
            {
                ThisWindow.BeginAnimation(Window.WidthProperty, WindowOpeningAnim);
                SaveColumn.BeginAnimation(ColumnDefinition.WidthProperty, GridOpeningAnim);
            }

        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(() => vm.Save());
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            string filterstring = null;
            string autoSavePath = vm.GetAutoSavePath(vm.SaveFormat);
            sfd.FileName = Path.GetFileName(autoSavePath);
            switch (vm.SaveFormat)
            {
                case ImageEngineFormat.BMP:
                    filterstring = "Bitmap Images|*.bmp";
                    break;
                case ImageEngineFormat.DDS_ARGB:
                case ImageEngineFormat.DDS_ATI1:
                case ImageEngineFormat.DDS_ATI2_3Dc:
                case ImageEngineFormat.DDS_DXT1:
                case ImageEngineFormat.DDS_DXT2:
                case ImageEngineFormat.DDS_DXT3:
                case ImageEngineFormat.DDS_DXT4:
                case ImageEngineFormat.DDS_DXT5:
                case ImageEngineFormat.DDS_G8_L8:
                case ImageEngineFormat.DDS_V8U8:
                    filterstring = "DDS Images|*.dds";
                    break;
                case ImageEngineFormat.JPG:
                    filterstring = "JPG Images|*.jpg;*.jpeg";
                    break;
                case ImageEngineFormat.PNG:
                    filterstring = "PNG Images|*.png";
                    break;
                case ImageEngineFormat.TGA:
                    filterstring = "Targa Images|*.tga";
                    break;
            }

            sfd.Filter = filterstring;
            sfd.Title = "Select location to save image";
            if (sfd.ShowDialog() == true)
                vm.SavePath = sfd.FileName;
        }

        private void ChangeSaveMessageVisibility(bool? state)
        {
            if (state == true)
            {
                SuccessfulSaveMessage.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, SaveMessageOpen);
                FailedSaveMessage.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, SaveMessageClose);
            }
            else if(state == false)
            {
                SuccessfulSaveMessage.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, SaveMessageClose);
                FailedSaveMessage.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, SaveMessageOpen);
            }
            else
            {
                SuccessfulSaveMessage.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, SaveMessageClose);
                FailedSaveMessage.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, SaveMessageClose);
            }
        }

        private void SaveFailedLabel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show(vm.SavingFailedErrorMessage);
        }

        private void ThisWindow_Drop(object sender, DragEventArgs e)
        {
            ChangeConvertPanel(false);
            isOpen = false;
            string[] filenames = (string[])(e.Data.GetData(DataFormats.FileDrop));
            
            // Only loads the first image dropped. Can only have the one image loaded, so why would people drop more than one...
            // Come to think of it, I wonder if this can be filtered out in the DragEnter event? Like if more than one file is being dragged, indicate somehow that only one can be used.


            vm.LoadImage(filenames[0]);
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                SaveButton_Click(null, null);
        }
    }
}
