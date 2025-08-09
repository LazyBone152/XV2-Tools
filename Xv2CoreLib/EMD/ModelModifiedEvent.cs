using System;

namespace Xv2CoreLib.EMD
{
    public enum EditTypeEnum
    {
        Add,
        Remove,
        Vertex,
        Sampler,
        Material,
        Bone
    }

    public delegate void ModelModifiedEventHandler(object source, ModelModifiedEventArgs e);

    public class ModelModifiedEventArgs : EventArgs
    {
        public EditTypeEnum EditType { get; private set; }
        public object Context { get; private set; }
        public object Parent { get; private set; }

        public ModelModifiedEventArgs(EditTypeEnum editType, object context, object parent)
        {
            EditType = editType;
            Context = context;
            Parent = parent;
        }
    }

    public interface IModelFile 
    {
        event ModelModifiedEventHandler ModelModified;

        void TriggerModelModifiedEvent(EditTypeEnum editType, object context, object parent);
    }

}