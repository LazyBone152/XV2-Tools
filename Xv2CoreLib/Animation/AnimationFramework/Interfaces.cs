using System.Collections.Generic;
using Xv2CoreLib.Resource.UndoRedo;

namespace Xv2CoreLib.AnimationFramework
{
    public interface IAnimation
    {

    }

    public interface IAnimationNode
    {
        bool IsExpanded { get; set; }
        IAnimationNode ParentNode { get; }

        IEnumerable<int> GetAllKeyframes();
        void RebaseKeyframes(int startFrame, int amount, List<IUndoRedo> undos = null);
        int GetMinFrame();
    }

    public interface IAnimationNodeBone : IAnimationNode
    {

    }

    public interface IAnimationNodeComponent : IAnimationNode
    {

    }

    public interface IAnimationNodeChannel : IAnimationNode
    {

    }
}
