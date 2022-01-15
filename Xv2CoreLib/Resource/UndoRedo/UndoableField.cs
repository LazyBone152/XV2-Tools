namespace Xv2CoreLib.Resource.UndoRedo
{
    public class UndoableField : IUndoRedo
    {
        public bool doLast { get; set; }
        private object _oldValue;
        private object _newValue;
        private string _field;
        private object _instance;
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
