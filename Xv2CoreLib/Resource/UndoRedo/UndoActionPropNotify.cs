using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xv2CoreLib.Resource.UndoRedo
{
    /// <summary>
    /// Calls NotifyPropertyChanged on all properties of the specified instance. NOTE: Requires NotifyPropertyChanged to be public!
    /// </summary>
    public class UndoActionPropNotify : IUndoRedo
    {
        public string Message { get; set; }
        public bool doLast { get; set; }

        private object instance;

        public UndoActionPropNotify(object _instance, bool _doLast, string _message = "")
        {
            instance = _instance;
            Message = _message;
            doLast = _doLast;
        }


        public void Undo()
        {
            ObjectExtensions.NotifyPropsChanged(instance);
        }

        public void Redo()
        {
            ObjectExtensions.NotifyPropsChanged(instance);
        }
    }
}
