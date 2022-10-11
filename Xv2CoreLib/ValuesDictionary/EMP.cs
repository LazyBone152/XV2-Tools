using System.Collections.Generic;
using Xv2CoreLib.EMP_NEW;

namespace Xv2CoreLib.ValuesDictionary
{
    public static class EMP
    {
        public static Dictionary<ParticleNodeType, string> NodeType { get; private set; } = new Dictionary<ParticleNodeType, string>()
        {
            { ParticleNodeType.Null , "None" },
            { ParticleNodeType.Emitter , "Emitter" },
            { ParticleNodeType.Emission , "Emission" },
        };

        public static Dictionary<ParticleAutoRotationType, string> AutoRotationType { get; private set; } = new Dictionary<ParticleAutoRotationType, string>()
        {
            { ParticleAutoRotationType.Camera , "Camera" },
            { ParticleAutoRotationType.Front , "Front" },
        };

        public static Dictionary<ParticleAutoRotationType, string> ShapeDraw_AutoRotationType { get; private set; } = new Dictionary<ParticleAutoRotationType, string>()
        {
            { ParticleAutoRotationType.Camera , "Camera" },
            { ParticleAutoRotationType.Front , "Front" },
            { ParticleAutoRotationType.None , "None" },
        };

        public static Dictionary<ParticleEmitter.ParticleEmitterShape, string> EmitterShapes { get; private set; } = new Dictionary<ParticleEmitter.ParticleEmitterShape, string>()
        {
            { ParticleEmitter.ParticleEmitterShape.Circle , "Circle" },
            { ParticleEmitter.ParticleEmitterShape.Square , "Square" },
            { ParticleEmitter.ParticleEmitterShape.Sphere , "Sphere" },
            { ParticleEmitter.ParticleEmitterShape.Cone , "Cone" },
        };
    }
}
