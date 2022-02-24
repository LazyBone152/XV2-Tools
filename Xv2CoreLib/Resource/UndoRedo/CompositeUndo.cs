using System.Collections.Generic;
using System.Linq;

namespace Xv2CoreLib.Resource.UndoRedo
{
    public class CompositeUndo : IUndoRedo
    {
        public bool doLast { get; set; }
        internal List<IUndoRedo> undos;
        public string Message { get; set; }
        public UndoGroup UndoGroup { get; set; }

        /// <summary>
        /// A composition undoable step encapsulating multiple undoable actions.
        /// </summary>
        /// <param name="_undos">The actions to undo.</param>
        public CompositeUndo(List<IUndoRedo> _undos, string message, UndoGroup undoGroup = UndoGroup.Default)
        {
            undos = (_undos != null) ? _undos : new List<IUndoRedo>();
            Message = message;
            UndoGroup = undoGroup;

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

        public bool CanMerge(CompositeUndo undo)
        {
            if (undo.undos.Count != undos.Count) return false;

            for(int i = 0; i < undos.Count; i++)
            {
                if (undo.undos[i] is IMergableUndo mergeUndo && undos[i] is IMergableUndo prevMergeUndo)
                {
                    //Undos qualify for merging
                    if (mergeUndo._field == prevMergeUndo._field && mergeUndo._instance == prevMergeUndo._instance && mergeUndo.doLast == prevMergeUndo.doLast)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            return false;
        }
    
        public void MergeCompositeUndos(CompositeUndo undoToMerge)
        {
            for (int i = 0; i < undos.Count; i++)
            {
                if (undoToMerge.undos[i] is IMergableUndo mergeUndo && undos[i] is IMergableUndo prevMergeUndo)
                {
                    //Undos qualify for merging
                    if (mergeUndo._field == prevMergeUndo._field && mergeUndo._instance == prevMergeUndo._instance && mergeUndo.doLast == prevMergeUndo.doLast)
                    {
                        prevMergeUndo._newValue = mergeUndo._newValue;
                    }
                }
            }
        }
    }
}
