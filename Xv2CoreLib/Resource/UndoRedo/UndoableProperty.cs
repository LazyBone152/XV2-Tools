using System;

namespace Xv2CoreLib.Resource.UndoRedo
{
    public class UndoableProperty<T> : IUndoRedo, IMergableUndo
    {
        //Was accidently made a generic class. Cant remove that now.

        public bool doLast { get; set; }
        public object _oldValue { get; set; }
        public object _newValue { get; set; }
        public string _field { get; set; }
        public object _instance { get; set; }
        public string Message { get; }

        public UndoableProperty(string property, object instance, object oldValue, object newValue, string description = "")
        {
            _instance = instance;
            _field = property;
            _oldValue = oldValue;
            _newValue = newValue;

            Message = description;
        }

        public void Undo()
        {
            _instance.GetType().GetProperty(_field).SetValue(_instance, _oldValue, null);
        }

        public void Redo()
        {
            _instance.GetType().GetProperty(_field).SetValue(_instance, _newValue, null);
        }
        
    }
}
