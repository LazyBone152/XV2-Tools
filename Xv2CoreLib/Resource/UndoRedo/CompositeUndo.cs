using System.Collections.Generic;

namespace Xv2CoreLib.Resource.UndoRedo
{
    public class CompositeUndo : IUndoRedo
    {
        private List<IUndoRedo> undos;
        public string Message { get; set; }

        public CompositeUndo(List<IUndoRedo> _undos, string message)
        {
            undos = (_undos != null) ? _undos : new List<IUndoRedo>();
            Message = message;
        }

        public void Undo()
        {
            foreach (var undo in undos)
                undo.Undo();
        }

        public void Redo()
        {
            foreach (var undo in undos)
                undo.Redo();
        }
    }
}
