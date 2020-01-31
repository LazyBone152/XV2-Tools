namespace Xv2CoreLib.Resource.UndoRedo
{
    public interface IUndoRedo
    {
        string Message { get; }
        void Undo();
        void Redo();
    }
}
