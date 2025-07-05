using MIConvexHull;
using System.Numerics;

namespace Xv2CoreLib.Havok
{
    public class ConvexVertex : IVertex
    {
        public double[] Position { get; }

        public Vector3 Original;

        public ConvexVertex(System.Numerics.Vector3 v)
        {
            Original = v;
            Position = new[] { (double)v.X, (double)v.Y, (double)v.Z };
        }
    }
}
