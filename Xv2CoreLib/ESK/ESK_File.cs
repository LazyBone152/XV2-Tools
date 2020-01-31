using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.ESK
{
    public class ESK_File
    {
        [YAXAttributeForClass]
        public int I_12 { get; set; }
        [YAXAttributeForClass]
        public int I_20 { get; set; }
        [YAXAttributeForClass]
        public int I_24 { get; set; }

        public ESK_Skeleton Skeleton { get; set; }
        
        public byte[] SaveToBytes()
        {
            return new Deserializer(this).bytes.ToArray();
        }

        public static ESK_File Load(byte[] bytes)
        {
            return new Parser(bytes).eskFile;
        }

        public static ESK_File Load(string path)
        {
            return new Parser(path, false).eskFile;
        }

        public void Save(string path)
        {
            new Deserializer(this, path);
        }
    }


    [YAXSerializeAs("Skeleton")]
    public class ESK_Skeleton
    {
        [YAXAttributeForClass]
        [YAXSerializeAs("Flag")]
        public short I_02 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("UseUnk2")]
        public bool UseUnk2 { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("I_28")]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ", ")]
        public int[] I_28 { get; set; } // size 2

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Bone")]
        [YAXDontSerializeIfNull]
        public ObservableCollection<ESK_Bone> ESKBones { get; set; }
        [YAXDontSerializeIfNull]
        public ESK_Unk1 Unk1 { get; set; }

        public void AddBones(string parent, List<ESK_Bone> bonesToAdd)
        {
            foreach(var bone in bonesToAdd)
            {
                AddBone(parent, bone);
            }
        }


        public bool AddBone(string parent, ESK_Bone boneToAdd)
        {
            if(parent == String.Empty)
            {
                ESKBones.Add(boneToAdd.Clone());
                return true;
            }

            for(int i = 0; i < ESKBones.Count; i++)
            {
                if(ESKBones[i].Name == parent)
                {
                    ESKBones[i].ESK_Bones.Add(boneToAdd.Clone());
                    return true;
                }

                if(ESKBones[i].ESK_Bones != null)
                {
                    bool result = AddBoneRecursive(parent, boneToAdd, ESKBones[i].ESK_Bones);

                    if (result == true) return true;
                }

            }

            return false;
        }

        private bool AddBoneRecursive(string parent, ESK_Bone boneToAdd, ObservableCollection<ESK_Bone> eskBones)
        {

            for (int i = 0; i < eskBones.Count; i++)
            {
                if (eskBones[i].Name == parent)
                {
                    eskBones[i].ESK_Bones.Add(boneToAdd.Clone());
                    return true;
                }

                if (eskBones[i].ESK_Bones != null)
                {
                    bool result = AddBoneRecursive(parent, boneToAdd, eskBones[i].ESK_Bones);

                    if (result == true) return true;
                }

            }

            return false;

        }

        public static int IndexOf(string bone, List<ESK_BoneNonHierarchal> bones)
        {
            if (bones != null)
            {
                for (int i = 0; i < bones.Count; i++)
                {
                    if (bones[i].Name == bone)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        public string NameOf(int index)
        {
            if (ESKBones == null) throw new Exception(String.Format("Could not get the name of the bone at index {0} because ESKBones is null.", index));
            if (index > ESKBones.Count - 1) throw new Exception(String.Format("Could not get the name of the bone at index {0} because a bone does not exist there.", index));

            if (index == -1)
            {
                return string.Empty;
            }
            else
            {
                return ESKBones[index].Name;
            }
        }

        public string GetSibling(string bone)
        {
            for(int i = 0; i < ESKBones.Count; i++)
            {
                if(ESKBones[i].Name == bone)
                {
                    if(i != ESKBones.Count - 1)
                    {
                        return ESKBones[i + 1].Name;
                    }
                    else
                    {
                        break;
                    }
                }

                if(ESKBones[i].ESK_Bones != null)
                {
                    string result = GetSiblingRecursive(bone, ESKBones[i].ESK_Bones);

                    if (result != String.Empty)
                    {
                        return result;
                    }
                }
            }

            return String.Empty;
        }

        private string GetSiblingRecursive(string bone, ObservableCollection<ESK_Bone> eskBones)
        {
            for (int i = 0; i < eskBones.Count; i++)
            {
                if (eskBones[i].Name == bone)
                {
                    if (i != eskBones.Count - 1)
                    {
                        return eskBones[i + 1].Name;
                    }
                    else
                    {
                        break;
                    }
                }

                if (eskBones[i].ESK_Bones != null)
                {
                    string result = GetSiblingRecursive(bone, eskBones[i].ESK_Bones);

                    if (result != String.Empty)
                    {
                        return result;
                    }
                }
            }

            return String.Empty;
        }

        public string GetChild(string bone)
        {
            for (int i = 0; i < ESKBones.Count; i++)
            {
                if (ESKBones[i].Name == bone)
                {
                    if (ESKBones[i].ESK_Bones != null)
                    {
                        if(ESKBones[i].ESK_Bones.Count > 0)
                        {
                            return ESKBones[i].ESK_Bones[0].Name;
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                if (ESKBones[i].ESK_Bones != null)
                {
                    string result = GetChildRecursive(bone, ESKBones[i].ESK_Bones);

                    if (result != String.Empty)
                    {
                        return result;
                    }
                }
            }

            return String.Empty;
        }

        private string GetChildRecursive(string bone, ObservableCollection<ESK_Bone> eskBones)
        {
            for (int i = 0; i < eskBones.Count; i++)
            {
                if (eskBones[i].Name == bone)
                {
                    if (eskBones[i].ESK_Bones != null)
                    {
                        if (eskBones[i].ESK_Bones.Count > 0)
                        {
                            return eskBones[i].ESK_Bones[0].Name;
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                if (eskBones[i].ESK_Bones != null)
                {
                    string result = GetChildRecursive(bone, eskBones[i].ESK_Bones);

                    if (result != String.Empty)
                    {
                        return result;
                    }
                }
            }

            return String.Empty;
        }

        public string GetParent(string bone)
        {
            for (int i = 0; i < ESKBones.Count; i++)
            {
                if (ESKBones[i].Name == bone)
                {
                    break;
                }

                if (ESKBones[i].ESK_Bones != null)
                {
                    string result = GetParentRecursive(bone, ESKBones[i].Name, ESKBones[i].ESK_Bones);

                    if (result != String.Empty)
                    {
                        return result;
                    }
                }
            }

            return String.Empty;
        }

        private string GetParentRecursive(string bone, string parentBone, ObservableCollection<ESK_Bone> eskBones)
        {
            for (int i = 0; i < eskBones.Count; i++)
            {
                if (eskBones[i].Name == bone)
                {
                    return parentBone;
                }

                if (eskBones[i].ESK_Bones != null)
                {
                    string result = GetParentRecursive(bone, eskBones[i].Name, eskBones[i].ESK_Bones);

                    if (result != String.Empty)
                    {
                        return result;
                    }
                }
            }

            return String.Empty;
        }

        public ESK_Skeleton Clone()
        {
            ObservableCollection<ESK_Bone> bones = new ObservableCollection<ESK_Bone>();

            foreach (var e in ESKBones)
            {
                bones.Add(e.Clone());
            }

            return new ESK_Skeleton()
            {
                I_02 = I_02,
                I_28 = I_28,
                Unk1 = Unk1,
                UseUnk2 = UseUnk2,
                ESKBones = bones
            };
        }

        public List<ESK_BoneNonHierarchal> GetNonHierarchalBoneList()
        {
            List<ESK_BoneNonHierarchal> bones = new List<ESK_BoneNonHierarchal>();

            //Generating the list
            foreach(var bone in ESKBones)
            {
                bones.Add(new ESK_BoneNonHierarchal()
                {
                    AbsoluteTransform = (bone.AbsoluteTransform != null) ? bone.AbsoluteTransform.Clone() : new ESK_AbsoluteTransform(),
                    RelativeTransform = (bone.RelativeTransform != null) ? bone.RelativeTransform.Clone() : new ESK_RelativeTransform(),
                    Name = bone.Name,
                    Index1_Name = GetParent(bone.Name),
                    Index2_Name = GetChild(bone.Name),
                    Index3_Name = GetSibling(bone.Name),
                    Index4 = bone.Index4
                });

                if(bone.ESK_Bones != null)
                {
                    bones.AddRange(GetNonHierarchalBoneListRecursive(bone.ESK_Bones));
                }

            }

            //Setting index numbers
            for(int i = 0; i < bones.Count; i++)
            {
                bones[i].Index1 = (short)IndexOf(bones[i].Index1_Name, bones);
                bones[i].Index2 = (short)IndexOf(bones[i].Index2_Name, bones);
                bones[i].Index3 = (short)IndexOf(bones[i].Index3_Name, bones);
            }

            //Returning the list
            return bones;
        }

        private List<ESK_BoneNonHierarchal> GetNonHierarchalBoneListRecursive(ObservableCollection<ESK_Bone> eskBones)
        {
            List<ESK_BoneNonHierarchal> bones = new List<ESK_BoneNonHierarchal>();

            foreach (var bone in eskBones)
            {
                bones.Add(new ESK_BoneNonHierarchal()
                {
                    AbsoluteTransform = (bone.AbsoluteTransform != null) ? bone.AbsoluteTransform.Clone() : new ESK_AbsoluteTransform(),
                    RelativeTransform = (bone.RelativeTransform != null) ? bone.RelativeTransform.Clone() : new ESK_RelativeTransform(),
                    Name = bone.Name,
                    Index1_Name = GetParent(bone.Name),
                    Index2_Name = GetChild(bone.Name),
                    Index3_Name = GetSibling(bone.Name),
                    Index4 = bone.Index4
                });

                if (bone.ESK_Bones != null)
                {
                    bones.AddRange(GetNonHierarchalBoneListRecursive(bone.ESK_Bones));
                }
            }

            return bones;
        }

        public ESK_BoneNonHierarchal[] GetAbsoluteBones()
        {
            var bones = GetNonHierarchalBoneList();
            ESK_BoneNonHierarchal[] newBones = new ESK_BoneNonHierarchal[bones.Count];

            for(int i = 0; i < bones.Count; i++)
            {
                newBones[i] = bones[i].Clone();

                int parent = newBones[i].Index1;

                while (parent != -1)
                {
                    //Position
                    newBones[i].RelativeTransform.F_00 += bones[parent].RelativeTransform.F_00;
                    newBones[i].RelativeTransform.F_04 += bones[parent].RelativeTransform.F_04;
                    newBones[i].RelativeTransform.F_08 += bones[parent].RelativeTransform.F_08;
                    newBones[i].RelativeTransform.F_12 += bones[parent].RelativeTransform.F_12;

                    //Rotation
                    newBones[i].RelativeTransform.F_16 += bones[parent].RelativeTransform.F_16;
                    newBones[i].RelativeTransform.F_20 += bones[parent].RelativeTransform.F_20;
                    newBones[i].RelativeTransform.F_24 += bones[parent].RelativeTransform.F_24;
                    newBones[i].RelativeTransform.F_28 += bones[parent].RelativeTransform.F_28;

                    parent = bones[parent].Index1;
                }
            }

            return newBones;
        }
    }

    [YAXSerializeAs("Bone")]
    public class ESK_Bone : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // This method is called by the Set accessor of each property.
        // The CallerMemberName attribute that is applied to the optional propertyName
        // parameter causes the property name of the caller to be substituted as an argument.
        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private bool isSelectedValue = true;
        [YAXDontSerialize]
        public bool isSelected
        {
            get
            {
                return this.isSelectedValue;
            }

            set
            {
                if (value != this.isSelectedValue)
                {
                    this.isSelectedValue = value;
                    NotifyPropertyChanged("isSelected");
                }
            }
        }
        
        [YAXAttributeForClass]
        public string Name { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Index")]
        public short Index { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("UnkIndex")]
        public short Index4 { get; set; }

        //Transforms
        public ESK_RelativeTransform RelativeTransform { get; set; }
        public ESK_AbsoluteTransform AbsoluteTransform { get; set; }

        //Parent reference
        [YAXDontSerialize]
        public ESK_Bone Parent { get; set; }

        //Props
        [YAXDontSerialize]
        public int NumChildren
        {
            get
            {
                if (ESK_Bones == null) return 0;
                return ESK_Bones.Count;
            }
        }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Bone")]
        public ObservableCollection<ESK_Bone> ESK_Bones { get; set; }
        
        public ESK_Bone Clone()
        {
            return new ESK_Bone()
            {
                Name = (string)Name.Clone(),
                Index4 = Index4,
                RelativeTransform = RelativeTransform.Clone(),
                AbsoluteTransform = AbsoluteTransform.Clone(),
                ESK_Bones = CloneChildrenRecursive(ESK_Bones),
                isSelected = true
            };
        }

        private ObservableCollection<ESK_Bone> CloneChildrenRecursive(ObservableCollection<ESK_Bone> bones)
        {
            //Not yet implmented. This will clone all children bones.
            return new ObservableCollection<ESK_Bone>();
        }

        public static ESK_Bone Read(List<byte> listBytes, byte[] bytes, int[] offsets, int idx, ESK_Bone parent = null)
        {
            int boneIndexOffset = offsets[0];
            int nameOffset = offsets[1];
            int skinningMatrixOffset = offsets[2];
            int transformMatrixOffset = offsets[3];

            return new ESK_Bone()
            {
                Index4 = BitConverter.ToInt16(bytes, boneIndexOffset + 6),
                Index = (short)idx,
                Name = Utils.GetString(listBytes, nameOffset),
                RelativeTransform = ESK_RelativeTransform.Read(bytes, skinningMatrixOffset),
                AbsoluteTransform = ESK_AbsoluteTransform.Read(bytes, transformMatrixOffset),
                Parent = parent
            };
        }
        
        public ESK_Bone GetBoneWithIndex(int index)
        {
            if (Index == index) return this;
            if (ESK_Bones == null) return null;

            foreach(var child in ESK_Bones)
            {
                if (child.Index == index) return child;

                ESK_Bone _children = child.GetBoneWithIndex(index);

                if (_children != null) return _children;
                
            }

            return null;
        }
        
        public ESK_Bone GetBoneWithName(string name)
        {
            if (Name == name) return this;
            if (ESK_Bones == null) return null;

            foreach (var child in ESK_Bones)
            {
                if (child.Name == name) return child;

                ESK_Bone _children = child.GetBoneWithName(name);

                if (_children != null) return _children;
            }

            return null;
        }
    }

    [YAXSerializeAs("RelativeTransform")]
    public class ESK_RelativeTransform
    {
        [YAXAttributeFor("Position")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0##########")]
        public float F_00 { get; set; }
        [YAXAttributeFor("Position")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0##########")]
        public float F_04 { get; set; }
        [YAXAttributeFor("Position")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0##########")]
        public float F_08 { get; set; }
        [YAXAttributeFor("Position")]
        [YAXSerializeAs("W")]
        [YAXFormat("0.0##########")]
        public float F_12 { get; set; }
        [YAXAttributeFor("Rotation")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0##########")]
        public float F_16 { get; set; }
        [YAXAttributeFor("Rotation")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0##########")]
        public float F_20 { get; set; }
        [YAXAttributeFor("Rotation")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0##########")]
        public float F_24 { get; set; }
        [YAXAttributeFor("Rotation")]
        [YAXSerializeAs("W")]
        [YAXFormat("0.0##########")]
        public float F_28 { get; set; }
        [YAXAttributeFor("Scale")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0##########")]
        public float F_32 { get; set; }
        [YAXAttributeFor("Scale")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0##########")]
        public float F_36 { get; set; }
        [YAXAttributeFor("Scale")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0##########")]
        public float F_40 { get; set; }
        [YAXAttributeFor("Scale")]
        [YAXSerializeAs("W")]
        [YAXFormat("0.0##########")]
        public float F_44 { get; set; }

        public ESK_RelativeTransform Clone()
        {
            return new ESK_RelativeTransform()
            {
                F_00 = F_00,
                F_04 = F_04,
                F_08 = F_08,
                F_12 = F_12,
                F_16 = F_16,
                F_20 = F_20,
                F_24 = F_24,
                F_28 = F_28,
                F_32 = F_32,
                F_36 = F_36,
                F_40 = F_40,
                F_44 = F_44
            };
        }
        
        public static ESK_RelativeTransform Read(byte[] bytes, int offset)
        {
            if (offset == 0) return null;

            return new ESK_RelativeTransform()
            {
                F_00 = BitConverter.ToSingle(bytes, offset + 0),
                F_04 = BitConverter.ToSingle(bytes, offset + 4),
                F_08 = BitConverter.ToSingle(bytes, offset + 8),
                F_12 = BitConverter.ToSingle(bytes, offset + 12),
                F_16 = BitConverter.ToSingle(bytes, offset + 16),
                F_20 = BitConverter.ToSingle(bytes, offset + 20),
                F_24 = BitConverter.ToSingle(bytes, offset + 24),
                F_28 = BitConverter.ToSingle(bytes, offset + 28),
                F_32 = BitConverter.ToSingle(bytes, offset + 32),
                F_36 = BitConverter.ToSingle(bytes, offset + 36),
                F_40 = BitConverter.ToSingle(bytes, offset + 40),
                F_44 = BitConverter.ToSingle(bytes, offset + 44)
            };
        }
    }

    [YAXSerializeAs("AbsoluteTransform")]
    public class ESK_AbsoluteTransform
    {
        [YAXAttributeFor("Line1")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0##########")]
        public float F_00 { get; set; }
        [YAXAttributeFor("Line1")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0##########")]
        public float F_04 { get; set; }
        [YAXAttributeFor("Line1")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0##########")]
        public float F_08 { get; set; }
        [YAXAttributeFor("Line1")]
        [YAXSerializeAs("W")]
        [YAXFormat("0.0##########")]
        public float F_12 { get; set; }
        [YAXAttributeFor("Line2")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0##########")]
        public float F_16 { get; set; }
        [YAXAttributeFor("Line2")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0##########")]
        public float F_20 { get; set; }
        [YAXAttributeFor("Line2")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0##########")]
        public float F_24 { get; set; }
        [YAXAttributeFor("Line2")]
        [YAXSerializeAs("W")]
        [YAXFormat("0.0##########")]
        public float F_28 { get; set; }
        [YAXAttributeFor("Line3")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0##########")]
        public float F_32 { get; set; }
        [YAXAttributeFor("Line3")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0##########")]
        public float F_36 { get; set; }
        [YAXAttributeFor("Line3")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0##########")]
        public float F_40 { get; set; }
        [YAXAttributeFor("Line3")]
        [YAXSerializeAs("W")]
        [YAXFormat("0.0##########")]
        public float F_44 { get; set; }

        [YAXAttributeFor("Line4")]
        [YAXSerializeAs("X")]
        [YAXFormat("0.0##########")]
        public float F_48 { get; set; }
        [YAXAttributeFor("Line4")]
        [YAXSerializeAs("Y")]
        [YAXFormat("0.0##########")]
        public float F_52 { get; set; }
        [YAXAttributeFor("Line4")]
        [YAXSerializeAs("Z")]
        [YAXFormat("0.0##########")]
        public float F_56 { get; set; }
        [YAXAttributeFor("Line4")]
        [YAXSerializeAs("W")]
        [YAXFormat("0.0##########")]
        public float F_60 { get; set; }

        public ESK_AbsoluteTransform Clone()
        {
            return new ESK_AbsoluteTransform()
            {
                F_00 = F_00,
                F_04 = F_04,
                F_08 = F_08,
                F_12 = F_12,
                F_16 = F_16,
                F_20 = F_20,
                F_24 = F_24,
                F_28 = F_28,
                F_32 = F_32,
                F_36 = F_36,
                F_40 = F_40,
                F_44 = F_44,
                F_48 = F_48,
                F_52 = F_52,
                F_56 = F_56,
                F_60 = F_60
            };
        }

        public static ESK_AbsoluteTransform Read(byte[] bytes, int offset)
        {
            if (offset == 0) return null;

            return new ESK_AbsoluteTransform()
            {
                F_00 = BitConverter.ToSingle(bytes, offset + 0),
                F_04 = BitConverter.ToSingle(bytes, offset + 4),
                F_08 = BitConverter.ToSingle(bytes, offset + 8),
                F_12 = BitConverter.ToSingle(bytes, offset + 12),
                F_16 = BitConverter.ToSingle(bytes, offset + 16),
                F_20 = BitConverter.ToSingle(bytes, offset + 20),
                F_24 = BitConverter.ToSingle(bytes, offset + 24),
                F_28 = BitConverter.ToSingle(bytes, offset + 28),
                F_32 = BitConverter.ToSingle(bytes, offset + 32),
                F_36 = BitConverter.ToSingle(bytes, offset + 36),
                F_40 = BitConverter.ToSingle(bytes, offset + 40),
                F_44 = BitConverter.ToSingle(bytes, offset + 44),
                F_48 = BitConverter.ToSingle(bytes, offset + 48),
                F_52 = BitConverter.ToSingle(bytes, offset + 52),
                F_56 = BitConverter.ToSingle(bytes, offset + 56),
                F_60 = BitConverter.ToSingle(bytes, offset + 60),
            };
        }

    }
    
    [YAXSerializeAs("Unk1")]
    public class ESK_Unk1
    {
        //All are Int32!
        [YAXAttributeFor("I_00")]
        [YAXSerializeAs("value")]
        public int I_00 { get; set; }
        [YAXAttributeFor("I_04")]
        [YAXSerializeAs("value")]
        public int I_04 { get; set; }
        [YAXAttributeFor("I_08")]
        [YAXSerializeAs("value")]
        public int I_08 { get; set; }
        [YAXAttributeFor("I_12")]
        [YAXSerializeAs("value")]
        public int I_12 { get; set; }
        [YAXAttributeFor("I_16")]
        [YAXSerializeAs("value")]
        public int I_16 { get; set; }
        [YAXAttributeFor("I_20")]
        [YAXSerializeAs("value")]
        public int I_20 { get; set; }
        [YAXAttributeFor("I_24")]
        [YAXSerializeAs("value")]
        public int I_24 { get; set; }
        [YAXAttributeFor("I_28")]
        [YAXSerializeAs("value")]
        public int I_28 { get; set; }
        [YAXAttributeFor("I_32")]
        [YAXSerializeAs("value")]
        public int I_32 { get; set; }
        [YAXAttributeFor("I_36")]
        [YAXSerializeAs("value")]
        public int I_36 { get; set; }
        [YAXAttributeFor("I_40")]
        [YAXSerializeAs("value")]
        public int I_40 { get; set; }
        [YAXAttributeFor("I_44")]
        [YAXSerializeAs("value")]
        public int I_44 { get; set; }
        [YAXAttributeFor("I_48")]
        [YAXSerializeAs("value")]
        public int I_48 { get; set; }
        [YAXAttributeFor("I_52")]
        [YAXSerializeAs("value")]
        public int I_52 { get; set; }
        [YAXAttributeFor("I_56")]
        [YAXSerializeAs("value")]
        public int I_56 { get; set; }
        [YAXAttributeFor("I_60")]
        [YAXSerializeAs("value")]
        public int I_60 { get; set; }
        [YAXAttributeFor("I_64")]
        [YAXSerializeAs("value")]
        public int I_64 { get; set; }
        [YAXAttributeFor("I_68")]
        [YAXSerializeAs("value")]
        public int I_68 { get; set; }
        [YAXAttributeFor("I_72")]
        [YAXSerializeAs("value")]
        public int I_72 { get; set; }
        [YAXAttributeFor("I_76")]
        [YAXSerializeAs("value")]
        public int I_76 { get; set; }
        [YAXAttributeFor("I_80")]
        [YAXSerializeAs("value")]
        public int I_80 { get; set; }
        [YAXAttributeFor("I_84")]
        [YAXSerializeAs("value")]
        public int I_84 { get; set; }
        [YAXAttributeFor("I_88")]
        [YAXSerializeAs("value")]
        public int I_88 { get; set; }
        [YAXAttributeFor("I_92")]
        [YAXSerializeAs("value")]
        public int I_92 { get; set; }
        [YAXAttributeFor("I_96")]
        [YAXSerializeAs("value")]
        public int I_96 { get; set; }
        [YAXAttributeFor("I_100")]
        [YAXSerializeAs("value")]
        public int I_100 { get; set; }
        [YAXAttributeFor("I_104")]
        [YAXSerializeAs("value")]
        public int I_104 { get; set; }
        [YAXAttributeFor("I_108")]
        [YAXSerializeAs("value")]
        public int I_108 { get; set; }
        [YAXAttributeFor("I_112")]
        [YAXSerializeAs("value")]
        public int I_112 { get; set; }
        [YAXAttributeFor("I_116")]
        [YAXSerializeAs("value")]
        public int I_116 { get; set; }
        [YAXAttributeFor("I_120")]
        [YAXSerializeAs("value")]
        public int I_120 { get; set; }

        public static ESK_Unk1 Read(byte[] bytes, int offset)
        {
            var unk1 = new ESK_Unk1();
            try
            {
                unk1.I_00 = BitConverter.ToInt32(bytes, offset + 0);
                unk1.I_04 = BitConverter.ToInt32(bytes, offset + 4);
                unk1.I_08 = BitConverter.ToInt32(bytes, offset + 8);
                unk1.I_12 = BitConverter.ToInt32(bytes, offset + 12);
                unk1.I_16 = BitConverter.ToInt32(bytes, offset + 16);
                unk1.I_20 = BitConverter.ToInt32(bytes, offset + 20);
                unk1.I_24 = BitConverter.ToInt32(bytes, offset + 24);
                unk1.I_28 = BitConverter.ToInt32(bytes, offset + 28);
                unk1.I_32 = BitConverter.ToInt32(bytes, offset + 32);
                unk1.I_36 = BitConverter.ToInt32(bytes, offset + 36);
                unk1.I_40 = BitConverter.ToInt32(bytes, offset + 40);
                unk1.I_44 = BitConverter.ToInt32(bytes, offset + 44);
                unk1.I_48 = BitConverter.ToInt32(bytes, offset + 48);
                unk1.I_52 = BitConverter.ToInt32(bytes, offset + 52);
                unk1.I_56 = BitConverter.ToInt32(bytes, offset + 56);
                unk1.I_60 = BitConverter.ToInt32(bytes, offset + 60);
                unk1.I_64 = BitConverter.ToInt32(bytes, offset + 64);
                unk1.I_68 = BitConverter.ToInt32(bytes, offset + 68);
                unk1.I_72 = BitConverter.ToInt32(bytes, offset + 72);
                unk1.I_76 = BitConverter.ToInt32(bytes, offset + 76);
                unk1.I_80 = BitConverter.ToInt32(bytes, offset + 80);
                unk1.I_84 = BitConverter.ToInt32(bytes, offset + 84);
                unk1.I_88 = BitConverter.ToInt32(bytes, offset + 88);
                unk1.I_92 = BitConverter.ToInt32(bytes, offset + 92);
                unk1.I_96 = BitConverter.ToInt32(bytes, offset + 96);
                unk1.I_100 = BitConverter.ToInt32(bytes, offset + 100);
                unk1.I_104 = BitConverter.ToInt32(bytes, offset + 104);
                unk1.I_108 = BitConverter.ToInt32(bytes, offset + 108);
                unk1.I_112 = BitConverter.ToInt32(bytes, offset + 112);
                unk1.I_116 = BitConverter.ToInt32(bytes, offset + 116);
                unk1.I_120 = BitConverter.ToInt32(bytes, offset + 120);
                return unk1;
                
            }
            catch
            {
                return unk1;
            }
        }

    }


    //Special, for rewriting binary file
    public class ESK_BoneNonHierarchal
    {
        public string Name { get; set; }
        public short Index1 { get; set; }
        public short Index2 { get; set; }
        public short Index3 { get; set; }
        public short Index4 { get; set; }


        public string Index1_Name { get; set; }
        public string Index2_Name { get; set; }
        public string Index3_Name { get; set; }

        //Transforms
        public ESK_RelativeTransform RelativeTransform { get; set; }
        public ESK_AbsoluteTransform AbsoluteTransform { get; set; }

        public ESK_BoneNonHierarchal Clone()
        {
            return new ESK_BoneNonHierarchal()
            {
                Name = Name,
                Index1 = Index1,
                Index1_Name = Index1_Name,
                Index2 = Index2,
                Index2_Name = Index2_Name,
                Index3 =Index3,
                Index3_Name = Index3_Name,
                Index4 = Index4,
                AbsoluteTransform = AbsoluteTransform.Clone(),
                RelativeTransform = RelativeTransform.Clone()
            };
        }
    }

}
