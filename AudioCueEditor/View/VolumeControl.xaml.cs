using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using Xv2CoreLib.ACB;

namespace AudioCueEditor.View
{
    /// <summary>
    /// Interaction logic for VolumeControl.xaml
    /// </summary>
    public partial class VolumeControl : MetroWindow, INotifyPropertyChanged
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

        Cue_Wrapper CueWrapper = null;
        Track_Wrapper TrackWrapper = null;

        //values
        private ushort baseVolume = 0;
        private ushort randomRange = 0;
        private ushort _originalBaseVolume = 0;
        private ushort _originalRandomRange = 0;

        public ushort BaseVolume
        {
            get
            {
                return baseVolume;
            }
            set
            {
                baseVolume = value;
                NotifyPropertyChanged("BaseVolume");
                NotifyPropertyChanged("MaxRandom");
            }
        }
        public ushort RandomRange
        {
            get
            {
                return randomRange;
            }
            set
            {
                randomRange = value;
                NotifyPropertyChanged("RandomRange");
                NotifyPropertyChanged("MaxVolume");
            }
        }
        public int MaxVolume
        {
            get
            {
                return 100 - randomRange;
            }
        }
        public int MaxRandom
        {
            get
            {
                return 100 - baseVolume;
            }
        }

        public VolumeControl(Window parent, Cue_Wrapper cue)
        {
            DataContext = this;
            CueWrapper = cue;

            var command = cue.SequenceCommand;
            ACB_Command volumeCommand = command?.Commands.FirstOrDefault(x => x.CommandType == CommandType.VolumeRandomization1 || x.CommandType == CommandType.VolumeRandomization2);
            InitValues(command, volumeCommand);

            InitializeComponent();
            Title = "Volume (cue-level)";
            Owner = parent;
        }

        public VolumeControl(Window parent, Track_Wrapper track)
        {
            DataContext = this;
            TrackWrapper = track;

            var command = track.TrackCommand;
            ACB_Command volumeCommand = command?.Commands.FirstOrDefault(x => x.CommandType == CommandType.VolumeRandomization1 || x.CommandType == CommandType.VolumeRandomization2);
            InitValues(command, volumeCommand);

            InitializeComponent();
            Title = "Volume (track-level)";
            Owner = parent;
        }

        private void InitValues(ACB_CommandGroup commandGroup, ACB_Command volumeCommand)
        {
            if (commandGroup != null && volumeCommand != null)
            {
                if (volumeCommand.CommandType == CommandType.VolumeRandomization1)
                {
                    RandomRange = volumeCommand.Param1;
                    BaseVolume = 0;
                }
                else
                {
                    BaseVolume = volumeCommand.Param1;
                    RandomRange = volumeCommand.Param2;
                }
            }
            else
            {
                //Default values
                BaseVolume = 100;
                RandomRange = 0;
            }

            //Some acbs have a sum slightly greater than 100 for some reason, but this set up wont like that so it needs to be fixed.
            if (baseVolume + randomRange > 100)
                randomRange = (ushort)(100 - baseVolume);

            _originalBaseVolume = baseVolume;
            _originalRandomRange = randomRange;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (_originalBaseVolume == BaseVolume && _originalRandomRange == RandomRange)
                Close(); //No changes

            if(CueWrapper != null)
            {
                CueWrapper.UndoableEditVolumeControl(BaseVolume, RandomRange);
            }
            else if(TrackWrapper != null)
            {
                TrackWrapper.UndoableEditVolumeControl(BaseVolume, RandomRange);
            }
            else
            {
                throw new InvalidOperationException("VolumeControl: Both CueWrapper and TrackWrapper are null.");
            }
            Close();
        }
    }
}
