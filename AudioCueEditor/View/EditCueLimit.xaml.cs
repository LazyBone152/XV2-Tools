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
using Xv2CoreLib.ACB;

namespace AudioCueEditor.View
{
    /// <summary>
    /// Interaction logic for EditCueLimit.xaml
    /// </summary>
    public partial class EditCueLimit : MetroWindow
    {
        Cue_Wrapper CueWrapper = null;
        public ushort CueLimit { get; set; }
        private ushort originalCueLimit = 0;

        public EditCueLimit(Window parent, Cue_Wrapper cue)
        {
            CueWrapper = cue;
            var command = cue.SequenceCommand;
            ACB_Command cueLimitCommand = command?.Commands.FirstOrDefault(x => x.CommandType == CommandType.CueLimit);
            
            if(cueLimitCommand != null)
                CueLimit = cueLimitCommand.Param1;

            originalCueLimit = CueLimit;
            
            InitializeComponent();
            DataContext = this;
            Owner = parent;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (CueLimit == originalCueLimit) Close();

            CueWrapper.UndoableEditCueLimit(CueLimit);
            Close();
        }
    }
}
