using System;

namespace Xv2CoreLib.Resource.UndoRedo
{
    /// <summary>
    /// Calls NotifyPropertyChanged on all properties of the specified instance. NOTE: Requires NotifyPropertyChanged to be public!
    /// </summary>
    public class UndoActionPropNotify : IUndoRedo
    {
        public string Message { get; set; }
        public bool doLast { get; set; }

        private string propName = string.Empty;
        private object instance;

        public UndoActionPropNotify(object _instance, bool _doLast, string _message = "")
        {
            instance = _instance;
            Message = _message;
            doLast = _doLast;
        }

        public UndoActionPropNotify(object _instance, string propName, bool _doLast, string _message = "")
        {
            instance = _instance;
            this.propName = propName;
            Message = _message;
            doLast = _doLast;
        }

        public void Undo()
        {
            if (!string.IsNullOrWhiteSpace(propName))
            {
                ObjectExtensions.NotifyPropsChanged(instance, propName);
            }
            else
            {
                ObjectExtensions.NotifyPropsChanged(instance);
            }
        }

        public void Redo()
        {
            Undo();
        }
    }
}
