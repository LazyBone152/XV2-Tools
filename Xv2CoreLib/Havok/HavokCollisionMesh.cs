using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Xv2CoreLib.EMD;
using MIConvexHull;

namespace Xv2CoreLib.Havok
{
    public class HavokCollisionMesh
    {
        public int[] Indices { get; internal set; }
        public Vector3[] Vertices { get; internal set; }

        public HavokCollisionMesh() { }

        public HavokCollisionMesh(EMD_Submesh emdSubmesh, Matrix4x4 world)
        {
            List<int> faces = new List<int>();
            Vertices = new Vector3[emdSubmesh.Vertexes.Count];

            for (int i = 0; i < emdSubmesh.Triangles.Count; i++)
            {
                for (int a = 0; a < emdSubmesh.Triangles[i].Faces.Count; a++)
                {
                    faces.Add(emdSubmesh.Triangles[i].Faces[a]);
                }
            }

            for (int i = 0; i < emdSubmesh.Vertexes.Count; i++)
            {
                Vertices[i] = new Vector3(emdSubmesh.Vertexes[i].PositionX, emdSubmesh.Vertexes[i].PositionY, emdSubmesh.Vertexes[i].PositionZ);

                if(world != Matrix4x4.Identity)
                {
                    Vertices[i] = Vector3.Transform(Vertices[i], world);
                }
            }

            Indices = faces.ToArray();
        }

        /// <summary>
        /// Check if the mesh instance has any relevant data
        /// </summary>
        /// <returns></returns>
        public bool HasData()
        {
            return Vertices?.Length > 0 && Indices?.Length > 0;
        }

        public void Transform(Matrix4x4 world)
        {
            for(int i = 0; i < Vertices.Length; i++)
            {
                Vertices[i] = Vector3.Transform(Vertices[i], world);
            }
        }

        #region Conversion
        public HavokCollisionMesh CreateConvexCopy()
        {
            if (Vertices.Length <= 4)
            {
                //Already convex
                return this;
            }
            else
            {
                var verts = Vertices.Select(v => new ConvexVertex(v)).ToList();

                var hull = ConvexHull.Create(verts);
                List<int> indices = new List<int>();
                List<Vector3> newConvexVertices = new List<Vector3>();

                var vertToIndex = new Dictionary<Vector3, int>();

                if (hull.Outcome == ConvexHullCreationResultOutcome.Success)
                {
                    foreach (var face in hull.Result.Faces)
                    {
                        foreach (var vert in face.Vertices)
                        {
                            Vector3 pos = ((ConvexVertex)vert).Original;
                            if (!vertToIndex.TryGetValue(pos, out int index))
                            {
                                index = newConvexVertices.Count;
                                newConvexVertices.Add(pos);
                                vertToIndex[pos] = index;
                            }
                            indices.Add((short)index);
                        }
                    }

                    HavokCollisionMesh newConvexMesh = new HavokCollisionMesh();
                    newConvexMesh.Vertices = newConvexVertices.ToArray();
                    newConvexMesh.Indices = indices.ToArray();

                    return newConvexMesh;
                }

            }

            return null;
        }

        public EMD_Submesh ToEmdSubmesh(string name)
        {
            EMD_Submesh emd = new EMD_Submesh();
            emd.Name = name;
            emd.VertexFlags = VertexFlags.Position;
            emd.AABB = new EMD_AABB();

            emd.Triangles.Add(new EMD_Triangle());
            emd.Triangles[0].Faces = Indices.ToList();

            foreach (var vertex in Vertices)
            {
                EMD_Vertex emdVertex = new EMD_Vertex();
                emdVertex.PositionX = vertex.X;
                emdVertex.PositionY = vertex.Y;
                emdVertex.PositionZ = vertex.Z;

                emd.Vertexes.Add(emdVertex);
            }

            return emd;
        }
        #endregion

        #region AABB
        public void CalculateFaceAABB(out Vector3 min, out Vector3 max, params int[] triangleIndices)
        {
            if (triangleIndices.Length != 3)
                throw new ArgumentException("CalculateFaceAABB: faces must be length 3");

            if (Vertices.Length <= triangleIndices[0] || Vertices.Length <= triangleIndices[1] || Vertices.Length <= triangleIndices[2])
                throw new ArgumentException("CalculateFaceAABB: face idx is not valid");

            min = new Vector3(float.PositiveInfinity);
            max = new Vector3(float.NegativeInfinity);

            for (int i = 0; i < 3; i++)
            {
                min = Vector3.Min(Vertices[triangleIndices[i]], min);
                max = Vector3.Max(Vertices[triangleIndices[i]], max);
            }
        }

        public void CalculateMeshAABB(out Vector3 min, out Vector3 max)
        {
            min = new Vector3(float.PositiveInfinity);
            max = new Vector3(float.NegativeInfinity);

            foreach (var _vertex in Vertices)
            {
                min = Vector3.Min(_vertex, min);
                max = Vector3.Max(_vertex, max);
            }
        }

        #endregion

    }
}
