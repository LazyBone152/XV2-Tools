using System;

namespace Xv2CoreLib.Resource.UndoRedo
{
    public class UndoablePropertyGeneric : IUndoRedo
    {
        public bool doLast { get; set; }
        private object _oldValue;
        private object _newValue;
        private string _property;
        private object _instance;
        public string Message { get; }

        public UndoablePropertyGeneric(string property, object instance, object oldValue, object newValue, string description = "")
        {
            _instance = instance;
            _property = property;
            _oldValue = oldValue;
            _newValue = newValue;

            Message = description;
        }

        public void Undo()
        {
            _instance.GetType().GetProperty(_property).SetValue(_instance, _oldValue, null);
        }

        public void Redo()
        {
            _instance.GetType().GetProperty(_property).SetValue(_instance, _newValue, null);
        }

    }
}
