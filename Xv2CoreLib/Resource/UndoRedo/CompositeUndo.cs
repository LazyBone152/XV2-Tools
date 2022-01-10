using System.Collections.Generic;
using System.Linq;

namespace Xv2CoreLib.Resource.UndoRedo
{
    public class CompositeUndo : IUndoRedo
    {
        public bool doLast { get; set; }
        private List<IUndoRedo> undos;
        public string Message { get; set; }

        /// <summary>
        /// A composition undoable step encapsulating multiple undoable actions.
        /// </summary>
        /// <param name="_undos">The actions to undo.</param>
        public CompositeUndo(List<IUndoRedo> _undos, string message)
        {
            undos = (_undos != null) ? _undos : new List<IUndoRedo>();
            Message = message;

            //Remove duplicate doLast actions
            List<int> existing = new List<int>();

            for (int i = undos.Count - 1; i >= 0; i--)
            {
                if (undos[i].doLast)
                {
                    int hash = undos[i].GetHashCode();

                    if (existing.Contains(hash))
                    {
                        undos.RemoveAt(i);
                        continue;
                    }
                    else
                    {
                        existing.Add(hash);
                    }
                }
            }
        }

        public void Undo()
        {
            foreach (IUndoRedo undo in undos.Where(x => !x.doLast).Reverse())
                undo.Undo();
            
            foreach (IUndoRedo undo in undos.Where(x => x.doLast).Reverse())
                undo.Undo();
        }

        public void Redo()
        {
            foreach (IUndoRedo redo in undos.Where(x => !x.doLast))
                redo.Redo();

            foreach (IUndoRedo redo in undos.Where(x => x.doLast))
                redo.Redo();
        }
    }
}
