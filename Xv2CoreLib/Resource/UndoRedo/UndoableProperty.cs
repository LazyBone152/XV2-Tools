using System;

namespace Xv2CoreLib.Resource.UndoRedo
{
    public class UndoableProperty<T> : IUndoRedo
    {
        public bool doLast { get; set; }
        private object _oldValue;
        private object _newValue;
        private string _property;
        private T _instance;
        public string Message { get; }

        public UndoableProperty(string property, T instance, object oldValue, object newValue, string description = "")
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
