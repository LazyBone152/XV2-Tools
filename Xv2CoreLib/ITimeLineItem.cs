using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Media;
using Xv2CoreLib.Resource.UndoRedo;

namespace Xv2CoreLib
{
    public interface ITimeLineItem : INotifyPropertyChanged
    {
        bool TimeLine_IsSelected { get; set; }
        int TimeLine_StartTime { get; set; }
        int TimeLine_Duration { get; set; }
        int Layer { get; set; }
        int LayerGroup { get; }

        string DisplayName { get; }

        Brush TimeLineMainBrush { get; }
        Brush TimeLineTextBrush { get; }
        Brush TimeLineBorderBrush { get; }

        void UpdateSourceValues(ushort newStartTime, ushort newDuration, List<IUndoRedo> undos = null);
    }
}
