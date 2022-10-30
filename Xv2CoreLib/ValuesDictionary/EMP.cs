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

        public static Dictionary<ParticleBillboardType, string> BillboardTypes { get; private set; } = new Dictionary<ParticleBillboardType, string>()
        {
            { ParticleBillboardType.None , "None" },
            { ParticleBillboardType.Camera , "Camera" },
            { ParticleBillboardType.Front , "Front" },
        };

        public static Dictionary<ParticleEmitter.ParticleEmitterShape, string> EmitterShapes { get; private set; } = new Dictionary<ParticleEmitter.ParticleEmitterShape, string>()
        {
            { ParticleEmitter.ParticleEmitterShape.Circle , "Circle" },
            { ParticleEmitter.ParticleEmitterShape.Square , "Square" },
            { ParticleEmitter.ParticleEmitterShape.Sphere , "Sphere" },
            { ParticleEmitter.ParticleEmitterShape.Point , "Point" },
        };

        public static Dictionary<ParticleEmission.ParticleEmissionType, string> EmissionTypes { get; private set; } = new Dictionary<ParticleEmission.ParticleEmissionType, string>()
        {
            { ParticleEmission.ParticleEmissionType.Plane , "Plane" },
            { ParticleEmission.ParticleEmissionType.ConeExtrude , "Cone Extrude" },
            { ParticleEmission.ParticleEmissionType.Mesh , "Static Mesh" },
            { ParticleEmission.ParticleEmissionType.ShapeDraw , "Shape Draw" },
        };

        public static Dictionary<EMP_ScrollState.ScrollTypeEnum, string> ScrollType { get; private set; } = new Dictionary<EMP_ScrollState.ScrollTypeEnum, string>()
        {
            { EMP_ScrollState.ScrollTypeEnum.Static , "Static" },
            { EMP_ScrollState.ScrollTypeEnum.Speed , "Scroll Speed" },
            { EMP_ScrollState.ScrollTypeEnum.SpriteSheet , "Sprite Sheet" },
        };

        public static Dictionary<EMP_TextureSamplerDef.TextureFiltering, string> TextureFiltering { get; private set; } = new Dictionary<EMP_TextureSamplerDef.TextureFiltering, string>()
        {
            { EMP_TextureSamplerDef.TextureFiltering.None , "None" },
            { EMP_TextureSamplerDef.TextureFiltering.Point , "Point" },
            { EMP_TextureSamplerDef.TextureFiltering.Linear , "Linear" },
        };

        public static Dictionary<EMP_TextureSamplerDef.TextureRepitition, string> TextureRepitition { get; private set; } = new Dictionary<EMP_TextureSamplerDef.TextureRepitition, string>()
        {
            { EMP_TextureSamplerDef.TextureRepitition.Wrap , "Wrap" },
            { EMP_TextureSamplerDef.TextureRepitition.Mirror , "Mirror" },
            { EMP_TextureSamplerDef.TextureRepitition.Clamp , "Clamp" },
            { EMP_TextureSamplerDef.TextureRepitition.Border , "Border" }
        };
    }
}
