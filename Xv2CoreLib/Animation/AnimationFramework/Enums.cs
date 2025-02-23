namespace Xv2CoreLib.AnimationFramework
{
    public enum AnimationFileType
    {
        Object,
        Camera,
        Light,
        Material
    }

    public enum KeyframeFloatType
    {
        Vector4 = 0, //Used for mat.ema. Is actually just a float32, but a specific type needed for mat animations
        Float16 = 1,
        Float32 = 2
    }

    public enum AnimationComponentType
    {
        Position,
        Rotation,
        Scale
    }

    public enum AnimationChannelType
    {
        X,
        Y,
        Z,
        W //Only used for material and light animations (object rotation is in euler angles, so just XYZ, and position and scale is also resolved to just XYZ)
    }

    public enum AnimationInterpolationType
    {
        Linear,
        QuadraticBezier,
        CubicBezier
    }
}
