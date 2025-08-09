using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Xv2CoreLib.EMD;
using Xv2CoreLib.ESK;
using Xv2CoreLib.Havok;
using Xv2CoreLib.Properties;

namespace Xv2CoreLib.FMP
{
    /// <summary>
    /// Provides static methods for creating and exporting Havok collision in <see cref="FMP_File"/> instances.
    /// </summary>
    public static class CollisionCreator
    {
        #region Import
        public static void CreateCollision(FMP_File fmp, FMP_Object obj, EMD_File emd, ESK_File esk, List<MeshFlags> externalFlags = null)
        {
            if (!fmp.Objects.Contains(obj))
                throw new ArgumentException($"CreateCollision: the input object does not exist in the map file!");

            obj.Flags = (ObjectFlags)21; //Unsure what this is, but if its not 21 and the object has custom collision, the game will crash

            FMP_CollisionGroup collisionGroup = CreateCollisionGroup(emd, esk, externalFlags, obj.Name, out Dictionary<int, FMP_Transform> matrices, out Dictionary<int, MeshFlags[]> flags);
            FMP_CollisionGroup existingGroup = null;

            if(obj.CollisionGroupInstance?.CollisionGroupIndex != ushort.MaxValue)
            {
                existingGroup = fmp.CollisionGroups[obj.CollisionGroupInstance.CollisionGroupIndex];
            }
            else
            {
                existingGroup = fmp.CollisionGroups.FirstOrDefault(x => x.Name == obj.Name);
            }

            if (existingGroup != null)
            {
                //Reuse the existing group and update any other references to it by other map objects

                existingGroup.Colliders = collisionGroup.Colliders;
                existingGroup.UnorderedHitboxList = collisionGroup.UnorderedHitboxList;
                collisionGroup = existingGroup;

                foreach (var otherMapObject in fmp.Objects)
                {
                    if (otherMapObject.CollisionGroupInstance == null || otherMapObject == obj) continue;

                    if (otherMapObject.CollisionGroupInstance.CollisionGroupIndex == existingGroup.Index)
                    {
                        otherMapObject.CollisionGroupInstance.CreateCollisionInstanceTree(collisionGroup, matrices, flags);
                    }
                }
            }
            else
            {
                collisionGroup.Index = fmp.CollisionGroups.Count;
                fmp.CollisionGroups.Add(collisionGroup);
            }

            obj.CollisionGroupInstance = new FMP_CollisionGroupInstance() { CollisionGroupIndex = (ushort)collisionGroup.Index, CollisionGroup = collisionGroup };
            obj.CollisionGroupInstance.CreateCollisionInstanceTree(collisionGroup, matrices, flags);

        }

        internal static FMP_CollisionGroup CreateCollisionGroup(EMD_File emd, ESK_File esk, List<MeshFlags> externalFlags, string name, out Dictionary<int, FMP_Transform> matrices, out Dictionary<int, MeshFlags[]> flags)
        {
            //Split up the submeshes and pre-compute the resulting havok collider files accross many threads before making the actual collision group, as this is the slowest part

            List<HavokColliderStep> havokStep = new List<HavokColliderStep>();

            if (esk != null)
            {
                esk.Skeleton.GenerateAbsoluteMatrices();
                esk.Skeleton.CreateNonRecursiveBoneList();
            }

            foreach (var model in emd.Models)
            {
                Matrix4x4 matrix = Matrix4x4.Identity;
                ESK_Bone bone = esk?.Skeleton.GetBone(model.Name);

                if (bone != null)
                {
                    matrix = bone.GeneratedAbsoluteMatrix;
                }

                foreach (var mesh in model.Meshes)
                {
                    foreach(var submesh in mesh.Submeshes)
                    {
                        havokStep.Add(new HavokColliderStep(new HavokCollisionMesh(submesh, matrix), submesh.Name, externalFlags));
                    }
                }
            }

            Task[] tasks = new Task[havokStep.Count];

            for (int i = 0; i < havokStep.Count; i++)
            {
                int hvkStepIdx = i;
                tasks[hvkStepIdx] = Task.Run(() => havokStep[hvkStepIdx].CreateHavokCollider());
                //havokStep[i].CreateHavokCollider();
            }

            Task.WaitAll(tasks);

            //Now that the havok files are created, lets make the collision group:
            matrices = new Dictionary<int, FMP_Transform>();
            flags = new Dictionary<int, MeshFlags[]>();

            FMP_CollisionGroup collisionGroup = new FMP_CollisionGroup();
            collisionGroup.Name = name;

            FMP_Collider root = new FMP_Collider();
            root.Name = "GeneratedCollisionRoot";
            collisionGroup.Colliders.Add(root);

            int idx = 1;
            int totalSubmeshIdx = 0;

            foreach (var model in emd.Models)
            {
                FMP_Collider modelRoot = new FMP_Collider();
                //Using the model/mesh name can cause problems with the positioning of the stage geometry. Why, i dont know. It even happens if the name has a prefix, suffix, or text insrted in the middle
                modelRoot.Name = "GROUP_" + idx;

                idx++;
                root.Colliders.Add(modelRoot);

                /*
                if (esk != null)
                {
                    ESK_Bone bone = esk.Skeleton.GetBone(model.Name);

                    if (bone != null)
                    {
                        //matrices.Add(idx, FMP_Matrix.CreateFromMatrix(bone.GeneratedAbsoluteMatrix));
                    }
                }
                */

                foreach (var mesh in model.Meshes)
                {
                    FMP_Collider collider = new FMP_Collider();
                    collider.Name = "PART_" + idx;
                    collider.unk_a0 = ushort.MaxValue;

                    MeshFlags prevOptions = new MeshFlags();
                    List<MeshFlags> flagArray = new List<MeshFlags>(); //Per-group options.
                    int submeshIdx = 0;
                    int havokGroup = -1;

                    foreach (var submesh in mesh.Submeshes)
                    {
                        //Read flags defined in submesh name
                        MeshFlags options = havokStep[totalSubmeshIdx].Flags;

                        if (!options.Ignore)
                        {
                            //We can group together all submeshes that have the same flags into the same havok group
                            //But if the flags are different, the group needs to be different, since some flags change the per-group parameters
                            if (!options.Compare(prevOptions) || submeshIdx == 0)
                            {
                                havokGroup++;
                                flagArray.Add(options);
                            }

                            prevOptions = options;

                            if (havokStep[totalSubmeshIdx].Success)
                            {
                                HavokTagFile havokFile = havokStep[totalSubmeshIdx].HavokFile;

                                FMP_Havok havok = new FMP_Havok();
                                havok.Group = havokGroup;

                                //The FMP Havok entry requires different values for convex/mesh colliders, or the game crashes
                                //I'm not sure on the exact values; but these work...
                                if (havokStep[totalSubmeshIdx].IsConvex)
                                {
                                    havok.I_08 = 0;
                                    havok.I_32 = 4;
                                }
                                else
                                {
                                    havok.I_08 = 78;
                                    havok.I_28 = 6;
                                    havok.I_28 = 26;
                                }

                                havok.F_36 = 0.01f;
                                havok.HvkFile = havokFile.Write();

                                //Set fragment group
                                havok.FragmentGroup = options.FragmentGroup;

                                collider.HavokColliders.Add(havok);

                            }
                            else
                            {
                                Console.WriteLine($"Failed to import submesh \"{submesh.Name}\". Possibly it had too many triangles.");
                            }

                        }
                        else
                        {
                            Console.WriteLine($"Ignoring submesh \"{submesh.Name}\"");
                        }

                        submeshIdx++;
                        totalSubmeshIdx++;
                    }

                    if (collider.HavokColliders.Count > 0)
                    {
                        flags.Add(idx, flagArray.ToArray());
                        idx++;
                        modelRoot.Colliders.Add(collider);
                    }
                }

            }

            return collisionGroup;
        }

        /// <summary>
        /// Create a single Havok file containing one collision mesh.
        /// </summary>
        /// <param name="emd">Must only contain one submesh</param>
        /// <returns> A <see cref="HavokTagFile"/> instance containing the collision mesh if successful. Otherwise, <see cref="null"/> </returns>
        public static HavokTagFile CreateCollision(EMD_File emd)
        {
            const string error = "CollisionCreator.CreateCollision: The input EMD_File instance contains more than one submesh, which is not allowed.";
            if (emd.Models.Count > 1) throw new ArgumentException(error);
            if (emd.Models[0].Meshes.Count > 1) throw new ArgumentException(error);
            if (emd.Models[0].Meshes[0].Submeshes.Count > 1) throw new ArgumentException(error);

            HavokCollisionMesh havokMesh = new HavokCollisionMesh(emd.Models[0].Meshes[0].Submeshes[0], Matrix4x4.Identity);
            HavokTagFile havokFile = HavokTagFile.Load(Resources.HavokMeshTemplate);

            return havokFile.ReplaceMesh(havokMesh) ? havokFile : null;
        }
        #endregion

        #region Export
        public static EMD_File ExportCollisionAsEmd(FMP_File fmpFile, FMP_Object obj)
        {
            if (!fmpFile.Objects.Contains(obj))
                throw new ArgumentException($"ExportCollisionAsEmd: the input object does not exist in the map file!");

            if (obj.CollisionGroupInstance == null || obj.CollisionGroupInstance?.CollisionGroupIndex == ushort.MaxValue) return null;

            FMP_CollisionGroup collisionGroup = fmpFile.CollisionGroups.FirstOrDefault(x => x.Index == obj.CollisionGroupInstance.CollisionGroupIndex);

            if (collisionGroup != null)
            {
                if (!collisionGroup.HasHavokCollisionData())
                    return null;

                Console.WriteLine($"Extracting collision from object \"{obj.Name}\"....");

                EMD_File emd = new EMD_File();
                ExportCollisionAsEmd(emd, obj.CollisionGroupInstance.ColliderInstances, collisionGroup.Colliders, Matrix4x4.Identity);

                return emd;
            }

            return null;
        }

        private static void ExportCollisionAsEmd(EMD_File emdFile, List<FMP_ColliderInstance> colliderInstances, List<FMP_Collider> colliders, Matrix4x4 world)
        {
            if (colliderInstances.Count != colliders.Count)
                throw new Exception("Mismatch between collider and collider instances");

            for (int i = 0; i < colliders.Count; i++)
            {
                Matrix4x4 transform = world * colliderInstances[i].Matrix.ToMatrix();

                EMD_Model emdModel = new EMD_Model();
                emdModel.Name = colliders[i].Name;

                EMD_Mesh emdMesh = new EMD_Mesh();
                emdMesh.Name = colliders[i].Name;
                emdMesh.AABB = new EMD_AABB();
                emdModel.Meshes.Add(emdMesh);

                int idx = 0;
                foreach (var havok in colliders[i].HavokColliders)
                {
                    if (havok.HvkFile?.Length > 0)
                    {
                        HavokTagFile havokFile = HavokTagFile.Load(havok.HvkFile);
                        HavokCollisionMesh mesh = havokFile.ExtractMesh();

                        if (mesh != null && mesh?.HasData() == true)
                        {
                            //Apply a transformation to the mesh so that the original position is maintained in the exported model, and no longer relies on the matrix defined in the FMP file
                            //This also makes re-importing a lot easier, as we can just leave the matrix set to identity
                            mesh.Transform(transform);
                            FMP_HavokGroupParameters groupParams = colliderInstances[i].HavokGroupParameters[havok.Group];
                            string submeshName = $"{colliders[i].Name}_{idx}{GetSubmeshFlag(havok, groupParams, havokFile.IsConvexMesh())}";

                            emdMesh.Submeshes.Add(mesh.ToEmdSubmesh(submeshName));
                        }
                    }

                    idx++;
                }

                if (emdMesh.Submeshes.Count > 0)
                    emdFile.Models.Add(emdModel);

                if (colliders[i].Colliders?.Count > 0)
                {
                    ExportCollisionAsEmd(emdFile, colliderInstances[i].ColliderInstances, colliders[i].Colliders, transform);
                }

            }
        }

        private static string GetSubmeshFlag(FMP_Havok havokEntry, FMP_HavokGroupParameters havokGroupParams, bool isConvex)
        {
            List<string> flags = new List<string>();

            if (isConvex)
                flags.Add("convex");

            if (havokEntry.FragmentGroup > 0)
                flags.Add("fragment=" + havokEntry.FragmentGroup);

            flags.Add($"param1={havokGroupParams.Param1}");
            flags.Add($"param2={havokGroupParams.Param2}");

            if (flags.Count == 0) return null;
            string flag = "@[";

            for (int i = 0; i < flags.Count; i++)
            {
                flag += flags[i];
                if (i != flags.Count - 1)
                    flag += ", ";
            }

            flag += "]";
            return flag;
        }

        public static EMD_File ExportCollisionAsEmd(HavokTagFile havokFile, string name)
        {
            var collisionMesh = havokFile.ExtractMesh();

            EMD_File emd = new EMD_File();
            emd.Models.Add(new EMD_Model());
            emd.Models[0].Name = name;
            emd.Models[0].Meshes.Add(new EMD_Mesh());
            emd.Models[0].Meshes[0].Name = name;
            emd.Models[0].Meshes[0].AABB = new EMD_AABB();
            emd.Models[0].Meshes[0].Submeshes.Add(collisionMesh.ToEmdSubmesh(name));

            return emd;
        }
        #endregion

        public struct MeshFlags
        {
            public string SubmeshName { get; private set; }
            public bool Ignore;

            //Flags on Havok entry
            public bool Convex;
            public int FragmentGroup;

            //HavokGroupParameter.Param1
            public bool Param1_EdgeVFX;
            public bool Param1_Float;
            public int Param1_Custom;

            //HavokGroupParameter.Param2
            public int Param2_Custom;

            public static MeshFlags ReadFlagsFromName(string meshName)
            {
                MeshFlags meshOptions = new MeshFlags();
                meshOptions.Param1_Custom = -1;
                meshOptions.Param2_Custom = -1;

                int startIdx = meshName.IndexOf("@[");

                if (startIdx != -1)
                {
                    int endIdx = meshName.IndexOf("]", startIdx + 2);

                    if (endIdx != -1 && endIdx > startIdx)
                    {
                        string flags = meshName.Substring(startIdx + 2, endIdx - startIdx - 2);
                        string[] splitFlags = flags.Replace(" ", "").Split(',');
                        meshOptions.ReadFlags(splitFlags);
                    }

                }

                return meshOptions;
            }

            public static MeshFlags ReadFlagsDirect(string submeshName, string flagsStr)
            {
                MeshFlags meshOptions = new MeshFlags();
                meshOptions.SubmeshName = submeshName.TrimStart(' ').TrimEnd(' ');
                meshOptions.Param1_Custom = -1;
                meshOptions.Param2_Custom = -1;

                string[] flags = flagsStr.Replace(" ", "").Split(',');
                meshOptions.ReadFlags(flags);

                return meshOptions;
            }

            public static List<MeshFlags> ReadFlagsFromFile(string filePath)
            {
                List<MeshFlags> flags = new List<MeshFlags>();

                if (Path.GetExtension(filePath) == ".txt" && File.Exists(filePath))
                {
                    string[] lines = File.ReadAllLines(filePath);

                    foreach(string line in lines)
                    {
                        if (line.TrimStart(' ').StartsWith("#")) continue; //Is comment, skip
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        string[] splitLine = line.Replace(" ", "").Split(':');

                        if(splitLine.Length == 2)
                        {
                            if(flags.Any(x => x.SubmeshName == splitLine[0]))
                            {
                                Console.WriteLine($"Flag already defined for submesh {splitLine[0]}");
                                continue;
                            }

                            flags.Add(ReadFlagsDirect(splitLine[0], splitLine[1]));
                        }
                        else
                        {
                            Console.WriteLine($"Flag incorrectly formatted ({line})");
                        }
                    }
                }

                return flags;
            }

            private void ReadFlags(string[] flags)
            {
                foreach (var flag in flags)
                {
                    string[] split = flag.Split('=');

                    if (split.Length > 0)
                    {
                        string name = split[0];
                        string argument = split.Length == 2 ? split[1] : string.Empty;

                        switch (name.ToLower())
                        {
                            case "ignore":
                                Ignore = true;
                                break;
                            case "convex":
                                Convex = true;
                                break;
                            case "fragment":
                            case "fragments":
                            case "fragmentGroup":
                                int.TryParse(argument, out FragmentGroup);
                                break;
                            case "edgevfx":
                            case "param1_edgevfx":
                                Param1_EdgeVFX = true;
                                break;
                            case "param1_float":
                            case "float":
                                Param1_Float = true;
                                break;
                            case "param1":
                                int.TryParse(argument, out Param1_Custom);
                                break;
                            case "param2":
                                int.TryParse(argument, out Param2_Custom);
                                break;
                        }
                    }

                }

            }

            public readonly bool Compare(MeshFlags meshOptions)
            {
                if (Convex != meshOptions.Convex) return false;
                if (Param1_EdgeVFX != meshOptions.Param1_EdgeVFX) return false;
                if (FragmentGroup != meshOptions.FragmentGroup) return false;
                if (Param1_Float != meshOptions.Param1_Float) return false;
                if (Param1_Custom != meshOptions.Param1_Custom) return false;
                if (Param2_Custom != meshOptions.Param2_Custom) return false;

                return true;
            }
        }

        private class HavokColliderStep
        {
            private readonly HavokCollisionMesh SourceSubmesh;
            public HavokTagFile HavokFile { get; private set; }
            public bool Success { get; private set; }
            public bool IsConvex { get; private set; }
            public MeshFlags Flags { get; private set; }
            private readonly string SubmeshName;

            public HavokColliderStep(HavokCollisionMesh mesh, string submeshName, List<MeshFlags> externalFlags)
            {
                SubmeshName = submeshName;
                SourceSubmesh = mesh;

                if(externalFlags.Any(x => x.SubmeshName == submeshName))
                {
                    Flags = externalFlags.FirstOrDefault(x => x.SubmeshName == submeshName);
                }
                else
                {
                    Flags = MeshFlags.ReadFlagsFromName(submeshName);
                }
            }

            public void CreateHavokCollider()
            {
                if (Flags.Convex)
                {
                    HavokCollisionMesh convexCopy = SourceSubmesh.CreateConvexCopy();

                    if(convexCopy?.Vertices?.Length == SourceSubmesh.Vertices.Length || SourceSubmesh.Vertices.Length <= 4)
                    {
                        IsConvex = true;
                        Console.WriteLine($"[{SubmeshName}] Convex mesh -> using hknpConvexShape");
                    }
                }

                if (!IsConvex)
                {
                    Console.WriteLine($"[{SubmeshName}] Concave mesh -> using hknpExternMeshShape");
                }

                HavokTagFile havokFile = IsConvex ? HavokTagFile.Load(Resources.HavokConvexTemplate) : HavokTagFile.Load(Resources.HavokMeshTemplate);

                if (havokFile.ReplaceMesh(SourceSubmesh))
                {
                    Success = true;
                    HavokFile = havokFile;
                }
            }
        }
    }
}
