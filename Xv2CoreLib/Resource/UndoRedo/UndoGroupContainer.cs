namespace Xv2CoreLib.Resource.UndoRedo
{
    /// <summary>
    /// An undoable action that can encapsulate another undoable action along with an UndoGroup.
    /// </summary>
    public class UndoGroupContainer : IUndoRedo
    {
        public bool doLast { get; set; }
        internal IUndoRedo undo;
        public string Message { get; set; }
        public UndoGroup UndoGroup { get; set; }
        public string UndoArg { get; set; }
        public object UndoContext { get; set; }

        public UndoGroupContainer(IUndoRedo undo, UndoGroup undoGroup = UndoGroup.Default, string undoArg = null, object undoContext = null)
        {
            doLast = undo.doLast;
            Message = undo.Message;
            this.undo = undo;
            UndoGroup = undoGroup;
            UndoArg = undoArg;
            UndoContext = undoContext;
        }

        public void Undo()
        {
            undo.Undo();
        }

        public void Redo()
        {
            undo.Redo();
        }
    }
}
