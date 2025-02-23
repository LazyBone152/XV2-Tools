namespace Xv2CoreLib.EMA
{
    public interface IAnimation
    {

    }

    public interface IAnimationNode
    {
        bool IsExpanded { get; set; }
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
