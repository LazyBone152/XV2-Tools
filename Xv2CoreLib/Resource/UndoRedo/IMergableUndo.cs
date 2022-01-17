namespace Xv2CoreLib.Resource.UndoRedo
{
    internal interface IMergableUndo
    {
        bool doLast { get; set; }
        object _oldValue { get; set; }
        object _newValue { get; set; }
        string _field { get; set; }
        object _instance { get; set; }
    }
}
