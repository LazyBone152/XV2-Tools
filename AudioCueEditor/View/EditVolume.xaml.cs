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
using Xv2CoreLib.ACB_NEW;

namespace AudioCueEditor.View
{
    /// <summary>
    /// Interaction logic for EditVolume.xaml
    /// </summary>
    public partial class EditVolume : Window
    {
        //DISABLED for now because this bus / volume command is VERY crash prone, and the volume randomization commands are much simpler to use (and much more useful!)

        public ACB_StringValue StringValue { get; set; }
        public ushort Volume { get; set; }
        public List<ACB_StringValue> StringValues { get; set; }

        Cue_Wrapper CueWrapper;

        //original values
        private ACB_StringValue originalStringValue;
        private ushort originalVolume;

        public EditVolume(Window parent, Cue_Wrapper cue)
        {
            Owner = parent;
            DataContext = this;
            CueWrapper = cue;
            StringValues = cue.WrapperRoot.AcbFile.StringValues;

            var command = cue.SequenceCommand;
            ACB_Command volumeCommand = command?.Commands.FirstOrDefault(x => x.CommandType == CommandType.VolumeBus);
            if (command != null && volumeCommand != null)
            {
                StringValue = cue.WrapperRoot.AcbFile.GetTable(volumeCommand.ReferenceIndex.TableGuid, cue.WrapperRoot.AcbFile.StringValues, true);
                Volume = volumeCommand.Param2;
            }
            else
            {
                //Default values
                StringValue = (CueWrapper.WrapperRoot.AcbFile.StringValues.Count > 0) ? CueWrapper.WrapperRoot.AcbFile.StringValues[0] : null;
                Volume = 10000;
            }

            originalStringValue = StringValue;
            originalVolume = Volume;

            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (originalVolume == Volume && StringValue == originalStringValue)
                Close();

            CueWrapper.UndoableEditVolumeBus(StringValue.InstanceGuid, Volume);
            Close();
        }
    }
}
