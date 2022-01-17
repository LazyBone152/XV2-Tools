namespace Xv2CoreLib.Resource.UndoRedo
{
    public class UndoableField : IUndoRedo, IMergableUndo
    {
        public bool doLast { get; set; }
        public object _oldValue { get; set; }
        public object _newValue { get; set; }
        public string _field { get; set; }
        public object _instance { get; set; }
        public string Message { get; }

        public UndoableField(string field, object instance, object oldValue, object newValue, string description = "")
        {
            _instance = instance;
            _field = field;
            _oldValue = oldValue;
            _newValue = newValue;

            Message = description;
        }

        public void Undo()
        {
            _instance.GetType().GetField(_field).SetValue(_instance, _oldValue);
        }

        public void Redo()
        {
            _instance.GetType().GetField(_field).SetValue(_instance, _newValue);
        }

    }
}
