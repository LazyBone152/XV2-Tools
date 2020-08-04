using System;

namespace Xv2CoreLib.Resource.UndoRedo
{
    public interface IUndoRedo
    {
        bool doLast { get; set; }
        string Message { get; }
        void Undo();
        void Redo();
    }
}
