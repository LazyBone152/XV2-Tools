using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Xv2CoreLib.ESK;
using Xv2CoreLib.Resource;
using YAXLib;
using static Xv2CoreLib.FMP.CollisionCreator;

namespace Xv2CoreLib.FMP
{
    [YAXSerializeAs("Object")]
    public class FMP_Object : IInstallable
    {
        [YAXAttributeForClass]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        public int Index { get; set; }

        [YAXAttributeForClass]
        public string Name { get; set; }

        [YAXDontSerialize]
        public int SortID => Index;

        string IInstallable.Index
        {
            get => Index.ToString();
            set => Index = int.Parse(value);
        }

        [CustomSerialize(isHex: true)]
        public ushort I_04 { get; set; }

        [CustomSerialize]
        public ushort InitialEntityIndex { get; set; } //I_06
        [CustomSerialize]
        public ObjectFlags Flags { get; set; } //I_10

        [CustomSerialize(isFloat: true)]
        public float F_32 { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.Recursive, EachElementName = "Entity")]
        [YAXSerializeAs("EntityList")]
        [YAXDontSerializeIfNull]
        public List<FMP_Entity> Entities { get; set; }
        [YAXDontSerializeIfNull]
        public FMP_CollisionGroupInstance CollisionGroupInstance { get; set; }
        [YAXDontSerializeIfNull]
        public FMP_ActionBlock Action { get; set; }
        [YAXDontSerializeIfNull]
        public FMP_InstanceData InstanceData { get; set; }
        public FMP_Transform Transform { get; set; }

        internal static void WriteAll(List<FMP_Object> objects, List<FMP_CollisionGroup> hitboxGroups, List<byte> bytes, List<StringWriter.StringInfo> stringWriter)
        {
            int objectStart = bytes.Count;
            //Write main Object entries
            for (int i = 0; i < objects.Count; i++)
            {
                stringWriter.Add(new StringWriter.StringInfo() { Offset = bytes.Count, StringToWrite = objects[i].Name });
                bytes.AddRange(new byte[4]);
                bytes.AddRange(BitConverter.GetBytes(objects[i].I_04));
                bytes.AddRange(BitConverter.GetBytes(objects[i].InitialEntityIndex));
                bytes.AddRange(BitConverter.GetBytes(objects[i].CollisionGroupInstance != null ? objects[i].CollisionGroupInstance.CollisionGroupIndex : ushort.MaxValue));
                bytes.AddRange(BitConverter.GetBytes((ushort)objects[i].Flags));
                bytes.AddRange(new byte[8]); //VirtualSubPart and Action offsets
                bytes.AddRange(BitConverter.GetBytes(objects[i].Entities != null ? objects[i].Entities.Count : 0));
                bytes.AddRange(new byte[8]); //Entity and Hiearchy offsets
                bytes.AddRange(BitConverter.GetBytes(objects[i].F_32));
                bytes.AddRange(objects[i].Transform.Write());
            }

            //Write sub data, per object
            for (int i = 0; i < objects.Count; i++)
            {
                int entityCount = objects[i].Entities?.Count ?? 0;
                int hitboxInstancesStart = bytes.Count;
                List<FMP_ColliderInstance> hitboxInstances = objects[i].CollisionGroupInstance?.WriteHitboxTree(hitboxGroups, objects[i]);
                int hitboxInstanceCount = hitboxInstances != null ? hitboxInstances.Count : 0;

                //Write main bodies of virtualSubObject
                if (hitboxInstanceCount > 0)
                {
                    Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), objectStart + 12 + (84 * i));

                    for (int a = 0; a < hitboxInstanceCount; a++)
                    {
                        FMP_ColliderInstance hitboxInstance = hitboxInstances[a];
                        bytes.AddRange(BitConverter.GetBytes(hitboxInstance.HavokGroupParameters?.Count ?? 0));
                        bytes.AddRange(new byte[16]); //IndexPair, ObjectSubPart1, ObjectSubPart2, Action offsets
                        bytes.AddRange(BitConverter.GetBytes(hitboxInstance.I_20));
                        bytes.AddRange(BitConverter.GetBytes(hitboxInstance.I_22));
                        bytes.AddRange(BitConverter.GetBytes(hitboxInstance.F_24));
                        bytes.AddRange(BitConverter.GetBytes(hitboxInstance.BreakableStateDelay));
                        bytes.AddRange(hitboxInstance.Matrix.Write());
                    }
                }

                //Write Action (comes before the rest of VirtualSubObject data)
                if (objects[i].Action != null)
                {
                    Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), objectStart + 16 + (84 * i));
                    objects[i].Action.Write(bytes, stringWriter);
                }

                //Write HitboxInstance subdata
                if (hitboxInstanceCount > 0)
                {
                    for (int a = 0; a < hitboxInstanceCount; a++)
                    {
                        FMP_ColliderInstance hitboxInstance = hitboxInstances[a];

                        //First SubPart
                        if (hitboxInstance.PhysicsDescriptor != null)
                        {
                            Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), hitboxInstancesStart + 8 + (a * 80));
                            bytes.AddRange(hitboxInstance.PhysicsDescriptor.Write());
                        }

                        //Second SubPart (unsure on the order of this one - assume its after SubPart1 for now)
                        if (hitboxInstance.BreakableStateDescriptor != null)
                        {
                            Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), hitboxInstancesStart + 12 + (a * 80));
                            bytes.AddRange(hitboxInstance.BreakableStateDescriptor.Write());
                        }

                        //Index pairs come next
                        int indexPairs = hitboxInstance.HavokGroupParameters?.Count ?? 0;

                        if (indexPairs > 0)
                        {
                            Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), hitboxInstancesStart + 4 + (a * 80));

                            for (int p = 0; p < indexPairs; p++)
                            {
                                bytes.AddRange(BitConverter.GetBytes(hitboxInstance.HavokGroupParameters[p].Param1));
                                bytes.AddRange(BitConverter.GetBytes(hitboxInstance.HavokGroupParameters[p].Param2));
                            }
                        }

                        //Action
                        if (hitboxInstance.Action != null)
                        {
                            Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), hitboxInstancesStart + 16 + (a * 80));
                            hitboxInstance.Action.Write(bytes, stringWriter);
                        }
                    }
                }

                //Entities
                if (entityCount > 0)
                {
                    Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), objectStart + 24 + (84 * i));
                    FMP_Entity.Write(objects[i].Entities, bytes, stringWriter);
                }

                //Hierarchy
                if (objects[i].InstanceData != null)
                {
                    Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), objectStart + 28 + (84 * i));
                    objects[i].InstanceData.Write(bytes);
                }
            }

        }

        public static List<FMP_Object> ReadAll(byte[] bytes, int offset, int count, string[] depot1, string[] depot2, string[] depot3, string[] depot4, FMP_File fmpFile)
        {
            List<FMP_Object> entries = new List<FMP_Object>();

            for (int i = 0; i < count; i++)
            {
                entries.Add(Read(i, bytes, offset + (84 * i), depot1, depot2, depot3, depot4, fmpFile));
            }

            return entries;
        }

        public static FMP_Object Read(int index, byte[] bytes, int offset, string[] depot1, string[] depot2, string[] depot3, string[] depot4, FMP_File fmpFile)
        {
            FMP_Object obj = new FMP_Object();
            obj.Index = index;
            obj.Name = StringEx.GetString(bytes, BitConverter.ToInt32(bytes, offset));
            obj.I_04 = BitConverter.ToUInt16(bytes, offset + 4);
            obj.InitialEntityIndex = BitConverter.ToUInt16(bytes, offset + 6);
            obj.Flags = (ObjectFlags)BitConverter.ToUInt16(bytes, offset + 10);
            obj.F_32 = BitConverter.ToSingle(bytes, offset + 32);
            obj.Transform = FMP_Transform.Read(bytes, offset + 36);

            ushort hitboxGroupIndex = BitConverter.ToUInt16(bytes, offset + 8);
            int hitboxInstancesOffset = BitConverter.ToInt32(bytes, offset + 12);
            int actionOffset = BitConverter.ToInt32(bytes, offset + 16);
            int entityCount = BitConverter.ToInt32(bytes, offset + 20);
            int entityOffset = BitConverter.ToInt32(bytes, offset + 24);
            int hierarchyOffset = BitConverter.ToInt32(bytes, offset + 28);

            if (hitboxGroupIndex < fmpFile.CollisionGroups.Count && hitboxGroupIndex >= 0)
            {
                FMP_CollisionGroup hitboxGroup = fmpFile.CollisionGroups[hitboxGroupIndex];
                obj.CollisionGroupInstance = FMP_CollisionGroupInstance.Read(bytes, hitboxInstancesOffset, hitboxGroup);
            }

            if (entityCount > 0)
                obj.Entities = FMP_Entity.ReadAll(bytes, entityOffset, entityCount, depot1, depot2, depot3, depot4);

            if (actionOffset > 0)
                obj.Action = FMP_ActionBlock.Read(bytes, actionOffset);

            if (hierarchyOffset > 0)
                obj.InstanceData = FMP_InstanceData.Read(bytes, hierarchyOffset);

            return obj;
        }

        public bool HasCommand(CommandType commandType)
        {
            var command = Action?.GetCommand(commandType);

            return command != null;
        }

        public override string ToString()
        {
            return $"{Name}";
        }
    }

    [YAXSerializeAs("Action")]
    public class FMP_ActionBlock
    {
        [YAXAttributeForClass]
        public int I_00 { get; set; }
        [YAXAttributeForClass]
        [YAXHexValue]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        public uint Flags { get; set; }

        #region LegacyCompatibility
        //Value aliases for legacy XML compat
        //Unfortunately YAXAltAlias does not work with class attributes or split values
        [YAXAttributeForClass]
        [YAXSerializeAs("I_04")]
        [YAXDontSerializeIfNull]
        public string XML_I_04
        {
            get => null;
            set
            {
                if (uint.TryParse(value, out uint sValue))
                    Flags |= sValue;
            }
        }
        [YAXAttributeForClass]
        [YAXSerializeAs("I_05")]
        [YAXDontSerializeIfNull]
        public string XML_I_05
        {
            get => null;
            set
            {
                if (uint.TryParse(value, out uint sValue))
                    Flags |= (sValue << 8);
            }
        }
        [YAXAttributeForClass]
        [YAXSerializeAs("I_06")]
        [YAXDontSerializeIfNull]
        public string XML_I_06
        {
            get => null;
            set
            {
                if (uint.TryParse(value, out uint sValue))
                    Flags |= (sValue << 16);
            }
        }
        [YAXAttributeForClass]
        [YAXSerializeAs("I_07")]
        [YAXDontSerializeIfNull]
        public string XML_I_07
        {
            get => null;
            set
            {
                if (uint.TryParse(value, out uint sValue))
                    Flags |= sValue << 24;
            }
        }

        #endregion

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Command")]
        [YAXDontSerializeIfNull]
        public List<FMP_Action> Commands { get; set; }

        internal void Write(List<byte> bytes, List<StringWriter.StringInfo> stringWriter)
        {
            int commandCount = Commands?.Count ?? 0;
            int actionStart = bytes.Count;

            //Calculate the mask
            uint mask = Flags & ~(0xFFFFu << 12); //Clear the TriggerFlag bits
            ushort combinedActionMask = 0;

            for (int i = 0; i < commandCount; i++)
            {
                combinedActionMask |= (ushort)Commands[i].TriggerFlags;
            }

            mask |= ((uint)combinedActionMask << 12); //Set the TriggerFlag bits to the combined mask of all actions

            bytes.AddRange(BitConverter.GetBytes(I_00));
            bytes.AddRange(BitConverter.GetBytes(mask));
            bytes.AddRange(BitConverter.GetBytes(commandCount));
            bytes.AddRange(BitConverter.GetBytes(commandCount > 0 ? (actionStart + 16) : 0)); //Comes straight after Action block

            //Write commands
            int commandStart = bytes.Count;

            for (int i = 0; i < commandCount; i++)
            {
                string commandName = Commands[i].Name ?? "DEFAULT"; //The game doesn't use the name, but will crash if it is empty

                if (FMP_Action.CommandNamesDic.TryGetValue(Commands[i].Type, out string knownCommandName))
                    commandName = knownCommandName;

                stringWriter.Add(new StringWriter.StringInfo() { Offset = bytes.Count, StringToWrite = commandName });
                bytes.AddRange(new byte[4]);
                bytes.AddRange(BitConverter.GetBytes((ushort)Commands[i].TriggerFlags));
                bytes.AddRange(BitConverter.GetBytes((short)Commands[i].Type));
                bytes.AddRange(BitConverter.GetBytes(Commands[i].Parameters?.Count ?? 0));
                bytes.AddRange(new byte[4]); //Offset to parameters
            }

            //Write parameters
            for (int i = 0; i < commandCount; i++)
            {
                int parameterStart = bytes.Count;
                int parameterCount = Commands[i].Parameters?.Count ?? 0;

                if (parameterCount > 0)
                {
                    Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), commandStart + 12 + (16 * i));

                    for (int a = 0; a < parameterCount; a++)
                    {
                        stringWriter.Add(new StringWriter.StringInfo() { Offset = bytes.Count, StringToWrite = Commands[i].Parameters[a].Name });
                        bytes.AddRange(new byte[4]);
                        bytes.AddRange(BitConverter.GetBytes((int)Commands[i].Parameters[a].Type));

                        if (Commands[i].Parameters[a].Type == FMP_Parameter.ParameterType.String)
                        {
                            stringWriter.Add(new StringWriter.StringInfo() { Offset = bytes.Count, StringToWrite = Commands[i].Parameters[a].Value as string });
                        }

                        bytes.AddRange(new byte[4]);
                    }

                    //Write values for this commands parameters
                    for (int a = 0; a < parameterCount; a++)
                    {
                        if (Commands[i].Parameters[a].Type == FMP_Parameter.ParameterType.String) continue; //String is handled by StringWriter

                        Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), parameterStart + 8 + (12 * a));

                        switch (Commands[i].Parameters[a].Type)
                        {
                            case FMP_Parameter.ParameterType.Int:
                                int val = (int)Commands[i].Parameters[a].Value;
                                bytes.AddRange(BitConverter.GetBytes(val));
                                break;
                            case FMP_Parameter.ParameterType.UInt:
                                bytes.AddRange(BitConverter.GetBytes((uint)Commands[i].Parameters[a].Value));
                                break;
                            case FMP_Parameter.ParameterType.Float:
                                bytes.AddRange(BitConverter.GetBytes((float)Commands[i].Parameters[a].Value));
                                break;
                            case FMP_Parameter.ParameterType.Direction:
                            case FMP_Parameter.ParameterType.Position:
                                if (Commands[i].Parameters[a].Value is float[] values && values.Length == 4)
                                {
                                    bytes.AddRange(BitConverter_Ex.GetBytes(values));
                                }
                                break;
                            case FMP_Parameter.ParameterType.Bool:
                                bool value = ((bool)Commands[i].Parameters[a].Value);
                                bytes.AddRange(BitConverter.GetBytes(value ? 1 : 0)); //Write as a 32 bit value
                                break;
                        }
                    }
                }
            }

            bytes.AddRange(new byte[Utils.CalculatePadding(bytes.Count, 4)]);
        }

        public static FMP_ActionBlock Read(byte[] bytes, int offset)
        {
            FMP_ActionBlock action = new FMP_ActionBlock();
            action.I_00 = BitConverter.ToInt32(bytes, offset);
            action.Flags = BitConverter.ToUInt32(bytes, offset + 4) & ~(0xFFFFu << 12); //Clear the TriggerFlag bits. These will be auto-set when saving the action, as they are the combined mask of all the actions.

            int commandCount = BitConverter.ToInt32(bytes, offset + 8);
            int commandOffset = BitConverter.ToInt32(bytes, offset + 12);
            action.Commands = FMP_Action.ReadAll(bytes, commandOffset, commandCount);

            return action;
        }

        public bool IsEqual(FMP_ActionBlock other)
        {
            if (I_00 != other.I_00) return false;
            if (Flags != other.Flags) return false;
            if ((Commands != null && other.Commands == null) || (Commands == null && other.Commands != null)) return false;

            if (Commands != null)
            {
                for (int i = 0; i < Commands.Count; i++)
                {
                    if (!Commands[i].IsEqual(other.Commands[i])) return false;
                }
            }

            return true;
        }

        public FMP_Action GetCommand(CommandType commandType)
        {
            for (int i = 0; i < Commands.Count; i++)
            {
                if (Commands[i].Type == commandType)
                    return Commands[i];
            }

            return null;
        }
    }

    [YAXSerializeAs("Command")]
    public class FMP_Action
    {

        [YAXDontSerializeIfNull]
        public string Name { get; set; }
        [YAXAttributeForClass]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        public CommandType Type { get; set; }
        [YAXAttributeForClass]
        [YAXHexValue]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        public TriggerFlags TriggerFlags { get; set; }

        #region LegacyCompatibility
        //Value aliases for legacy XML compat
        //Unfortunately YAXAltAlias does not work with class attributes or split values
        [YAXAttributeForClass]
        [YAXSerializeAs("I_04")]
        [YAXDontSerializeIfNull]
        public string XML_I_04
        {
            get => null;
            set
            {
                if (ushort.TryParse(value, out ushort sValue))
                    TriggerFlags |= (TriggerFlags)sValue;
            }
        }
        [YAXAttributeForClass]
        [YAXSerializeAs("I_05")]
        [YAXDontSerializeIfNull]
        public string XML_I_05
        {
            get => null;
            set
            {
                if (ushort.TryParse(value, out ushort sValue))
                    TriggerFlags |= (TriggerFlags)(sValue << 8);
            }
        }
        [YAXAttributeForClass]
        [YAXSerializeAs("I_06")]
        [YAXDontSerializeIfNull]
        public string XML_I_06
        {
            get => null;
            set
            {
                if (short.TryParse(value, out short sValue))
                    Type |= (CommandType)sValue;
            }
        }
        [YAXAttributeForClass]
        [YAXSerializeAs("I_07")]
        [YAXDontSerializeIfNull]
        public string XML_I_07
        {
            get => null;
            set
            {
                if (short.TryParse(value, out short sValue))
                    Type |= (CommandType)(sValue << 8);
            }
        }
        #endregion

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Parameter")]
        [YAXDontSerializeIfNull]
        public List<FMP_Parameter> Parameters { get; set; }

        public static List<FMP_Action> ReadAll(byte[] bytes, int offset, int count)
        {
            List<FMP_Action> entries = new List<FMP_Action>();

            for (int i = 0; i < count; i++)
            {
                entries.Add(Read(bytes, offset + (16 * i)));
            }

            return entries;
        }

        public static FMP_Action Read(byte[] bytes, int offset)
        {
            FMP_Action command = new FMP_Action();

            int nameOffset = BitConverter.ToInt32(bytes, offset);

            command.TriggerFlags = (TriggerFlags)BitConverter.ToUInt16(bytes, offset + 4);
            command.Type = (CommandType)BitConverter.ToInt16(bytes, offset + 6);

            //Only read the name if it is an unknown type
            if (!CommandNamesDic.ContainsKey(command.Type) && nameOffset > 0)
                command.Name = StringEx.GetString(bytes, nameOffset);

            int parameterCount = BitConverter.ToInt32(bytes, offset + 8);
            int parameterOffset = BitConverter.ToInt32(bytes, offset + 12);

            command.Parameters = FMP_Parameter.ReadAll(bytes, parameterOffset, parameterCount);

            return command;
        }

        public bool IsEqual(FMP_Action command)
        {
            if (Name != command.Name) return false;
            if (TriggerFlags != command.TriggerFlags) return false;
            if (Type != command.Type) return false;
            if ((Parameters != null && command.Parameters == null) || (Parameters == null && command.Parameters != null)) return false;

            if (Parameters != null)
            {
                for (int i = 0; i < Parameters.Count; i++)
                {
                    if (Parameters[i].Type != command.Parameters[i].Type) return false;
                    if (Parameters[i].XmlValue != command.Parameters[i].XmlValue) return false;
                    if (Parameters[i].Name != command.Parameters[i].Name) return false;
                }
            }

            return true;
        }

        public static readonly Dictionary<CommandType, string> CommandNamesDic = new Dictionary<CommandType, string>()
        {
            { CommandType.TimerSet, "TIMER SET" },
            { CommandType.OccluderDel, "OCCLUDER DEL" },
            { CommandType.OccluderAdd, "OCCLUDER ADD" },
            { CommandType.MirrorObject, "MIRROR OBJECT" },
            { CommandType.MotionSet, "MOTION SET" },
            { CommandType.MotionState, "MOTION STATE" },
            { CommandType.EventCall, "EVENT CALL" },
            { CommandType.NoParameter, "[NO PARAMETER]" },
            { CommandType.SetSky, "SET SKY" },
            { CommandType.SetSun, "SET SUN" },
            { CommandType.AddSound, "ADD SOUND" },
            { CommandType.PlaySound3D, "PLAY SOUND 3D" },
            { CommandType.PlayEffect, "PLAY EFFECT" },
            { CommandType.Water, "Water" },
            { CommandType.ChangeArea, "CHANGE AREA" },
            { CommandType.SetLct, "SET LCT" },
            { CommandType.WaterFall, "Water Fall" },
            { CommandType.SmallScene, "Small Scene" },
            { CommandType.SetBreakPartsEffect, "SET BREAK PARTS EFFECT" },
            { CommandType.SetShadowArea, "SET SHADOW AREA" },
            { CommandType.SetFlagUseCmnBreakPartsEffect, "SET FLAG USE CMN BREAK PARTS EFFECT" },
            { CommandType.SetBreakPartsInvisible, "SET BREAK PARTS INVISIBLE" },
            { CommandType.SetMotion, "SET MOTION" },
            { CommandType.LimitMoveInvisible, "LIMIT MOVE INVISIBLE" },
            { CommandType.HideObject, "HideObject" },
            { CommandType.HitCapsule, "HitCapsule" },
            { CommandType.LockonEnable, "LockonEnable" },
            { CommandType.HenshinImpossible, "Henshin Impossible" },
            { CommandType.PlayMassiveParticles, "Play Massive Particles" },
            { CommandType.ScatterFragment, "ScatterFragment" },
            { CommandType.ScatterFragmentCustom, "ScatterFragmentCustom" },
            { CommandType.Camera, "Camera" },
            { CommandType.ChaseStart, "Chase Start" },
            { CommandType.LockCamera, "Lock Camera" },
            { CommandType.UnlockCamera, "Unlock Camera" },
            { CommandType.ChangeFootEffect, "Change Foot Effect" },
            { CommandType.ChangeInteraction, "Change Interaction" },
            { CommandType.AddPlaneSound, "Add Plane Sound" },
            { CommandType.ChangeEnvSound, "Change Env Sound" }
        };

    }

    public class FMP_Parameter
    {
        public enum ParameterType
        {
            Bool = 0,
            Int = 1,
            Float = 2,
            Direction = 3, //normalized vector4
            Position = 4, //position vector4
            String = 6,
            UInt = 8
        }

        [YAXDontSerialize]
        public object Value { get; set; }

        [YAXAttributeForClass]
        public string Name { get; set; }
        [YAXAttributeForClass]
        public ParameterType Type { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Value")]
        public string XmlValue
        {
            get => GetXmlFormattedValue();
            set => SetValueFromXmlFormattedValue(value);
        }

        public static List<FMP_Parameter> ReadAll(byte[] bytes, int offset, int count)
        {
            List<FMP_Parameter> entries = new List<FMP_Parameter>();

            for (int i = 0; i < count; i++)
            {
                entries.Add(Read(bytes, offset + (12 * i)));
            }

            return entries;
        }

        public static FMP_Parameter Read(byte[] bytes, int offset)
        {
            FMP_Parameter parameter = new FMP_Parameter();

            int nameOffset = BitConverter.ToInt32(bytes, offset);
            parameter.Name = nameOffset > 0 ? StringEx.GetString(bytes, nameOffset) : null;
            parameter.Type = (ParameterType)BitConverter.ToInt32(bytes, offset + 4);
            int valueOffset = BitConverter.ToInt32(bytes, offset + 8);

            //if ==6, unk1_offset is a nameOffset. so it's a type. 0: bool , 1: uint, 2  float)

            switch (parameter.Type)
            {
                case ParameterType.Int:
                    parameter.Value = BitConverter.ToInt32(bytes, valueOffset);
                    break;
                case ParameterType.Bool:
                    parameter.Value = BitConverter.ToInt32(bytes, valueOffset) == 1;
                    break;
                case ParameterType.Float:
                    parameter.Value = BitConverter.ToSingle(bytes, valueOffset);
                    break;
                case ParameterType.Direction:
                case ParameterType.Position:
                    parameter.Value = BitConverter_Ex.ToFloat32Array(bytes, valueOffset, 4);
                    break;
                case ParameterType.String:
                    parameter.Value = StringEx.GetString(bytes, valueOffset);
                    break;
                case ParameterType.UInt:
                    parameter.Value = BitConverter.ToUInt32(bytes, valueOffset);
                    break;
                default:
                    throw new Exception("FMP_Parameter: unknown parameter type " + parameter.Type);
            }

            return parameter;
        }

        private void SetValueFromXmlFormattedValue(string value)
        {
            switch (Type)
            {
                case ParameterType.Int:
                    Value = int.Parse(value);
                    break;
                case ParameterType.Bool:
                    Value = value.Equals("true", StringComparison.OrdinalIgnoreCase);
                    break;
                case ParameterType.UInt:
                    Value = uint.Parse(value);
                    break;
                case ParameterType.Float:
                    Value = float.Parse(value);
                    break;
                case ParameterType.String:
                    Value = value;
                    break;
                case ParameterType.Direction:
                case ParameterType.Position:
                    string[] values = value.Replace(" ", "").Split(',');

                    if (values.Length != 4)
                    {
                        throw new InvalidDataException($"FMP_Parameter: Type {Type} must have 4 values.");
                    }

                    float[] vector4 = new float[4];
                    for (int i = 0; i < 4; i++)
                    {
                        float val;
                        if (!float.TryParse(values[i], out val))
                            throw new InvalidDataException($"FMP_Parameter: Couldn't parse the values on type {Type}");

                        vector4[i] = val;
                    }

                    Value = vector4;
                    break;
                default:
                    throw new Exception("FMP_Parameter: Unknown parameter type " + Type);
            }
        }

        private string GetXmlFormattedValue()
        {
            switch (Type)
            {
                case ParameterType.Direction:
                case ParameterType.Position:
                    if (Value is float[] vector4)
                    {
                        if (vector4.Length != 4)
                            throw new InvalidDataException($"FMP_Parameter: Type {Type} must have 4 values.");

                        return $"{vector4[0]}, {vector4[1]}, {vector4[2]}, {vector4[3]}";
                    }
                    else
                    {
                        throw new InvalidDataException("FMP_Parameter.GetXmlFormattedValue: Type does not match the internal value.");
                    }
                case ParameterType.Int:
                case ParameterType.Bool:
                case ParameterType.Float:
                case ParameterType.String:
                case ParameterType.UInt:
                default:
                    return Value.ToString();
            }
        }

    }

    [YAXSerializeAs("Entity")]
    public class FMP_Entity
    {
        [CustomSerialize]
        public int I_04 { get; set; }
        public FMP_Visual Visual { get; set; }
        public FMP_Transform Transform { get; set; }

        internal static void Write(List<FMP_Entity> entities, List<byte> bytes, List<StringWriter.StringInfo> stringWriter)
        {
            int entityStart = bytes.Count;

            for (int i = 0; i < entities.Count; i++)
            {
                bytes.AddRange(new byte[4]);
                bytes.AddRange(BitConverter.GetBytes(entities[i].I_04));
                bytes.AddRange(entities[i].Transform.Write());
            }

            //Write visual subcomponent
            int visualStart = bytes.Count;

            for (int i = 0; i < entities.Count; i++)
            {
                if (entities[i].Visual == null) continue;

                Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), entityStart + (56 * i));

                if (entities[i].Visual.LODs.Count == 0)
                    throw new Exception("FMP_Visual: cannot have zero LODs");

                stringWriter.Add(new StringWriter.StringInfo() { StringToWrite = entities[i].Visual.Name, Offset = bytes.Count });
                bytes.AddRange(new byte[4]);
                bytes.AddRange(BitConverter.GetBytes(entities[i].Visual.I_04));
                bytes.AddRange(BitConverter.GetBytes(entities[i].Visual.LODs.Count));
                bytes.AddRange(BitConverter.GetBytes(entities[i].Visual.LODs.Count > 1 ? 0 : entities[i].Visual.LODs[0].DepotNskIndex));
                bytes.AddRange(BitConverter.GetBytes(entities[i].Visual.DepotEmbIndex));
                bytes.AddRange(BitConverter.GetBytes(entities[i].Visual.LODs.Count > 1 ? 0 : entities[i].Visual.LODs[0].DepotEmmIndex));
                bytes.AddRange(BitConverter.GetBytes(entities[i].Visual.DepotNskIndex));
                bytes.AddRange(BitConverter.GetBytes(-1)); //Unknown 5th depot index, always -1 in vanilla FMPs
                bytes.AddRange(BitConverter.GetBytes(entities[i].Visual.DepotEmaIndex));
                bytes.AddRange(BitConverter.GetBytes(entities[i].Visual.DepotEmaIndex2));
                bytes.AddRange(BitConverter.GetBytes(entities[i].Visual.AnimationUpdateRate));
                bytes.AddRange(BitConverter.GetBytes(entities[i].Visual.MaterialAnimationUpdateRate));

                if (entities[i].Visual.LODs.Count > 1)
                {
                    bytes.AddRange(BitConverter.GetBytes(0));
                }
                else
                {
                    bytes.AddRange(BitConverter.GetBytes(entities[i].Visual.LODs[0].Distance));
                }
            }

            for (int i = 0; i < entities.Count; i++)
            {
                if (entities[i].Visual == null) continue;

                if (entities[i].Visual.LODs.Count > 1)
                {
                    //Distances
                    Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), visualStart + 48 + (52 * i));

                    foreach (var lod in entities[i].Visual.LODs)
                    {
                        bytes.AddRange(BitConverter.GetBytes(lod.Distance));
                    }

                    //NSKs
                    Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), visualStart + 12 + (52 * i));

                    foreach (var lod in entities[i].Visual.LODs)
                    {
                        bytes.AddRange(BitConverter.GetBytes(lod.DepotNskIndex));
                    }

                    //EMMs
                    Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count), visualStart + 20 + (52 * i));

                    foreach (var lod in entities[i].Visual.LODs)
                    {
                        bytes.AddRange(BitConverter.GetBytes(lod.DepotEmmIndex));
                    }

                }
            }
        }

        public static List<FMP_Entity> ReadAll(byte[] bytes, int offset, int count, string[] depot1, string[] depot2, string[] depot3, string[] depot4)
        {
            List<FMP_Entity> entries = new List<FMP_Entity>();

            for (int i = 0; i < count; i++)
            {
                entries.Add(Read(bytes, offset + (56 * i), depot1, depot2, depot3, depot4));
            }

            return entries;
        }

        public static FMP_Entity Read(byte[] bytes, int offset, string[] depot1, string[] depot2, string[] depot3, string[] depot4)
        {
            int visualOffset = BitConverter.ToInt32(bytes, offset);

            return new FMP_Entity()
            {
                I_04 = BitConverter.ToInt32(bytes, offset + 4),
                Visual = visualOffset > 0 ? FMP_Visual.Read(bytes, visualOffset, depot1, depot2, depot3, depot4) : null,
                Transform = FMP_Transform.Read(bytes, offset + 8)
            };
        }

    }

    public class FMP_Visual
    {
        [YAXAttributeForClass]
        public string Name { get; set; }
        [CustomSerialize(isHex: true)]
        public int I_04 { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "LOD")]
        public List<FMP_Lod> LODs { get; set; } = new List<FMP_Lod>();

        [CustomSerialize]
        public string EmbFile { get; set; }
        [CustomSerialize]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        public string ShadowNskFile { get; set; }
        [CustomSerialize]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        public string EmaFile { get; set; }
        [CustomSerialize]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        public string MaterialEmaFile { get; set; }
        [CustomSerialize(isFloat: true)]
        [YAXAltAliases("F_40/value")]
        public float AnimationUpdateRate { get; set; } = 60f;
        [CustomSerialize(isFloat: true)]
        [YAXAltAliases("F_44/value")]
        public float MaterialAnimationUpdateRate { get; set; } = 60f;

        [YAXDontSerialize]
        internal int DepotNskIndex { get; private set; }
        [YAXDontSerialize]
        internal int DepotEmbIndex { get; private set; }
        [YAXDontSerialize]
        internal int DepotEmaIndex { get; private set; }
        [YAXDontSerialize]
        internal int DepotEmaIndex2 { get; private set; }

        public static FMP_Visual Read(byte[] bytes, int offset, string[] depot1, string[] depot2, string[] depot3, string[] depot4)
        {
            FMP_Visual visual = new FMP_Visual();

            visual.Name = StringEx.GetString(bytes, BitConverter.ToInt32(bytes, offset));
            visual.I_04 = BitConverter.ToInt32(bytes, offset + 4);
            visual.AnimationUpdateRate = BitConverter.ToSingle(bytes, offset + 40);
            visual.MaterialAnimationUpdateRate = BitConverter.ToSingle(bytes, offset + 44);

            int embIndex = BitConverter.ToInt32(bytes, offset + 16);
            int rootNskIndex = BitConverter.ToInt32(bytes, offset + 24);
            //int unkIdx = BitConverter.ToInt32(bytes, offset + 28); //Index into the unknown 5th depot (unused by the game)
            int emaIndex = BitConverter.ToInt32(bytes, offset + 32);
            int emaIndex2 = BitConverter.ToInt32(bytes, offset + 36);

            visual.EmbFile = embIndex != -1 ? depot2[embIndex] : null;
            visual.ShadowNskFile = rootNskIndex != -1 ? depot1[rootNskIndex] : null;
            visual.EmaFile = emaIndex != -1 ? depot4[emaIndex] : null;
            visual.MaterialEmaFile = emaIndex2 != -1 ? depot4[emaIndex2] : null;

            //Read lods
            int lodCount = BitConverter.ToInt32(bytes, offset + 8);

            if (lodCount > 1)
            {
                int nskOffset = BitConverter.ToInt32(bytes, offset + 12);
                int emmOffset = BitConverter.ToInt32(bytes, offset + 20);
                int lodDistanceOffset = BitConverter.ToInt32(bytes, offset + 48);

                for (int i = 0; i < lodCount; i++)
                {
                    FMP_Lod lod = new FMP_Lod();

                    int nskIndex = BitConverter.ToInt32(bytes, nskOffset + (4 * i));
                    int emmIndex = BitConverter.ToInt32(bytes, emmOffset + (4 * i));

                    lod.NskFile = nskIndex != -1 ? depot1[nskIndex] : null;
                    lod.EmmFile = emmIndex != -1 ? depot3[emmIndex] : null;
                    lod.Distance = BitConverter.ToSingle(bytes, lodDistanceOffset + (4 * i));

                    visual.LODs.Add(lod);
                }
            }
            else
            {
                visual.LODs.Add(new FMP_Lod());
                int nskIndex = BitConverter.ToInt32(bytes, offset + 12);
                int emmIndex = BitConverter.ToInt32(bytes, offset + 20);

                visual.LODs[0].NskFile = nskIndex != -1 ? depot1[nskIndex] : null;
                visual.LODs[0].EmmFile = emmIndex != -1 ? depot3[emmIndex] : null;
                visual.LODs[0].Distance = BitConverter.ToSingle(bytes, offset + 48);
            }

            return visual;
        }

        public void CreateDepots(List<string> depot1, List<string> depot2, List<string> depot3, List<string> depot4)
        {
            foreach (var lod in LODs)
            {
                lod.CreateDepots(depot1, depot3);
            }

            DepotNskIndex = -1;
            DepotEmbIndex = -1;
            DepotEmaIndex = -1;
            DepotEmaIndex2 = -1;

            if (!string.IsNullOrWhiteSpace(ShadowNskFile))
            {
                DepotNskIndex = depot1.IndexOf(ShadowNskFile);

                if (DepotNskIndex == -1)
                {
                    DepotNskIndex = depot1.Count;
                    depot1.Add(ShadowNskFile);
                }
            }

            if (!string.IsNullOrWhiteSpace(EmbFile))
            {
                DepotEmbIndex = depot2.IndexOf(EmbFile);

                if (DepotEmbIndex == -1)
                {
                    DepotEmbIndex = depot2.Count;
                    depot2.Add(EmbFile);
                }
            }

            if (!string.IsNullOrWhiteSpace(EmaFile))
            {
                DepotEmaIndex = depot4.IndexOf(EmaFile);

                if (DepotEmaIndex == -1)
                {
                    DepotEmaIndex = depot4.Count;
                    depot4.Add(EmaFile);
                }
            }

            if (!string.IsNullOrWhiteSpace(MaterialEmaFile))
            {
                DepotEmaIndex2 = depot4.IndexOf(MaterialEmaFile);

                if (DepotEmaIndex2 == -1)
                {
                    DepotEmaIndex2 = depot4.Count;
                    depot4.Add(MaterialEmaFile);
                }
            }
        }
    }

    [YAXSerializeAs("LOD")]
    public class FMP_Lod
    {
        [YAXAttributeForClass]
        public float Distance { get; set; }
        [YAXAttributeForClass]
        public string NskFile { get; set; }
        [YAXAttributeForClass]
        public string EmmFile { get; set; }

        [YAXDontSerialize]
        internal int DepotNskIndex { get; private set; }
        [YAXDontSerialize]
        internal int DepotEmmIndex { get; private set; }

        public void CreateDepots(List<string> depot1, List<string> depot3)
        {
            DepotNskIndex = -1;
            DepotEmmIndex = -1;

            if (!string.IsNullOrWhiteSpace(EmmFile))
            {
                DepotEmmIndex = depot3.IndexOf(EmmFile);

                if (DepotEmmIndex == -1)
                {
                    DepotEmmIndex = depot3.Count;
                    depot3.Add(EmmFile);
                }
            }

            if (!string.IsNullOrWhiteSpace(NskFile))
            {
                DepotNskIndex = depot1.IndexOf(NskFile);

                if (DepotNskIndex == -1)
                {
                    DepotNskIndex = depot1.Count;
                    depot1.Add(NskFile);
                }
            }
        }
    }

    [YAXSerializeAs("Transform")]
    public class FMP_Transform
    {
        [CustomSerialize(parent: "Position", serializeAs: "X", isFloat: true)]
        public float PosX { get; set; }
        [CustomSerialize(parent: "Position", serializeAs: "Y", isFloat: true)]
        public float PosY { get; set; }
        [CustomSerialize(parent: "Position", serializeAs: "Z", isFloat: true)]
        public float PosZ { get; set; }
        [CustomSerialize(parent: "Rotation", serializeAs: "X", isFloat: true)]
        public float RotX { get; set; }
        [CustomSerialize(parent: "Rotation", serializeAs: "Y", isFloat: true)]
        public float RotY { get; set; }
        [CustomSerialize(parent: "Rotation", serializeAs: "Z", isFloat: true)]
        public float RotZ { get; set; }
        [CustomSerialize(parent: "Scale", serializeAs: "X", isFloat: true)]
        public float ScaleX { get; set; } = 1f;
        [CustomSerialize(parent: "Scale", serializeAs: "Y", isFloat: true)]
        public float ScaleY { get; set; } = 1f;
        [CustomSerialize(parent: "Scale", serializeAs: "Z", isFloat: true)]
        public float ScaleZ { get; set; } = 1f;


        [YAXDontSerialize]
        public bool IsDecomposed => RawMatrixValues == null || RawMatrixValues?.Length != 12;
        [CustomSerialize(parent: "RawMatrix")]
        [YAXDontSerializeIfNull]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        public float[] RawMatrixValues { get; set; }

        public byte[] Write()
        {
            List<byte> bytes = new List<byte>(48);

            float[] L0, L1, L2, L3;

            if (IsDecomposed)
            {
                Matrix4x4 matrix = ToMatrix();

                L0 = new float[3] { matrix.M11, matrix.M12, matrix.M13 };
                L1 = new float[3] { matrix.M21, matrix.M22, matrix.M23 };
                L2 = new float[3] { matrix.M31, matrix.M32, matrix.M33 };
                L3 = new float[3] { matrix.M41, matrix.M42, matrix.M43 };
            }
            else
            {
                L0 = new float[3] { RawMatrixValues[0], RawMatrixValues[1], RawMatrixValues[2] };
                L1 = new float[3] { RawMatrixValues[3], RawMatrixValues[4], RawMatrixValues[5] };
                L2 = new float[3] { RawMatrixValues[6], RawMatrixValues[7], RawMatrixValues[8] };
                L3 = new float[3] { RawMatrixValues[9], RawMatrixValues[10], RawMatrixValues[11] };
            }


            if (L0.Length != 3 || L1.Length != 3 || L2.Length != 3 || L3.Length != 3)
            {
                throw new Exception("FMP_Matrix: invalid number of elements in array. There must be 3 values per line.");
            }

            bytes.AddRange(BitConverter_Ex.GetBytes(L0));
            bytes.AddRange(BitConverter_Ex.GetBytes(L1));
            bytes.AddRange(BitConverter_Ex.GetBytes(L2));
            bytes.AddRange(BitConverter_Ex.GetBytes(L3));

            if (bytes.Count != 48)
                throw new Exception("FMP_Matrix: invalid size");

            return bytes.ToArray();
        }

        public static FMP_Transform Read(byte[] bytes, int offset)
        {
            float[] L0 = BitConverter_Ex.ToFloat32Array(bytes, offset, 3);
            float[] L1 = BitConverter_Ex.ToFloat32Array(bytes, offset + 12, 3);
            float[] L2 = BitConverter_Ex.ToFloat32Array(bytes, offset + 24, 3);
            float[] L3 = BitConverter_Ex.ToFloat32Array(bytes, offset + 36, 3);

            Matrix4x4 matrix = new Matrix4x4(L0[0], L0[1], L0[2], 0f, L1[0], L1[1], L1[2], 0f, L2[0], L2[1], L2[2], 0f, L3[0], L3[1], L3[2], 1f);

            return CreateFromMatrix(ref matrix, L0, L1, L2, L3);
        }

        public bool IsEqual(FMP_Transform matrix)
        {
            if (!IsDecomposed) return false;
            if (!MathHelpers.FloatEquals(matrix.PosX, PosX) || !MathHelpers.FloatEquals(matrix.PosY, PosY) || !MathHelpers.FloatEquals(matrix.PosZ, PosZ)) return false;
            if (!MathHelpers.FloatEquals(matrix.RotX, RotX) || !MathHelpers.FloatEquals(matrix.RotY, RotY) || !MathHelpers.FloatEquals(matrix.RotZ, RotZ)) return false;
            if (!MathHelpers.FloatEquals(matrix.ScaleX, ScaleX) || !MathHelpers.FloatEquals(matrix.ScaleY, ScaleY) || !MathHelpers.FloatEquals(matrix.ScaleZ, ScaleZ)) return false;

            return true;
        }

        /// <summary>
        /// Returns an instance of <see cref="FMP_Transform"/> set to identity values.
        /// </summary>
        /// <returns></returns>
        public static FMP_Transform GetDefault()
        {
            return new FMP_Transform();
        }

        public static FMP_Transform CreateFromBone(ESK_RelativeTransform transform)
        {
            return new FMP_Transform()
            {
                PosX = transform.PositionX * transform.PositionW,
                PosY = transform.PositionY * transform.PositionW,
                PosZ = transform.PositionZ * transform.PositionW
            };
        }

        public static FMP_Transform CreateFromMatrix(ref Matrix4x4 matrix, float[] L0, float[] L1, float[] L2, float[] L3)
        {
            if (Matrix4x4.Decompose(matrix, out Vector3 scale, out Quaternion rot, out Vector3 translation))
            {
                Vector3 rotationEuler = MathHelpers.QuaternionToEuler(rot);

                return new FMP_Transform()
                {
                    PosX = translation.X,
                    PosY = translation.Y,
                    PosZ = translation.Z,
                    RotX = rotationEuler.X,
                    RotY = rotationEuler.Y,
                    RotZ = rotationEuler.Z,
                    ScaleX = scale.X,
                    ScaleY = scale.Y,
                    ScaleZ = scale.Z,
                };
            }
            else
            {
                return new FMP_Transform()
                {
                    RawMatrixValues = new float[12] { L0[0], L0[1], L0[2], L1[0], L1[1], L1[2], L2[0], L2[1], L2[2], L3[0], L3[1], L3[2] }
                };
            }
        }

        public Matrix4x4 ToMatrix()
        {
            if (IsDecomposed)
            {
                Matrix4x4 matrix = Matrix4x4.CreateScale(ScaleX, ScaleY, ScaleZ);
                matrix *= Matrix4x4.CreateFromQuaternion(MathHelpers.EulerToQuaternion(new Vector3(RotX, RotY, RotZ)));
                matrix *= Matrix4x4.CreateTranslation(PosX, PosY, PosZ);
                return matrix;
            }
            else
            {
                return new Matrix4x4(RawMatrixValues[0], RawMatrixValues[1], RawMatrixValues[2], 0f, RawMatrixValues[3], RawMatrixValues[4], RawMatrixValues[5], 0f,
                                    RawMatrixValues[6], RawMatrixValues[7], RawMatrixValues[8], 0f, RawMatrixValues[9], RawMatrixValues[10], RawMatrixValues[11], 1f);
            }
        }
    }

    [YAXSerializeAs("CollisionGroupInstance")]
    public class FMP_CollisionGroupInstance
    {
        [YAXAttributeForClass]
        public ushort CollisionGroupIndex { get; set; } = ushort.MaxValue;
        [YAXDontSerialize]
        public FMP_CollisionGroup CollisionGroup { get; set; }

        [YAXComment("Names are for reference only. The order of entries is all that matters for linking a ColliderInstance with a Collider in the CollisionGroup")]
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "ColliderInstance")]
        public List<FMP_ColliderInstance> ColliderInstances { get; set; } = new List<FMP_ColliderInstance>();

        internal static FMP_CollisionGroupInstance Read(byte[] bytes, int offset, FMP_CollisionGroup hitboxGroup)
        {
            FMP_CollisionGroupInstance hitboxGroupInstance = new FMP_CollisionGroupInstance();
            hitboxGroupInstance.CollisionGroupIndex = (ushort)hitboxGroup.Index;

            List<FMP_ColliderInstance> hitboxInstances = FMP_ColliderInstance.ReadAll(bytes, offset, hitboxGroup.UnorderedHitboxList.Count);
            hitboxGroupInstance.ColliderInstances = CreateHitboxTree(hitboxGroup, hitboxInstances, 0);

            return hitboxGroupInstance;
        }

        private static List<FMP_ColliderInstance> CreateHitboxTree(FMP_CollisionGroup hitboxGroup, List<FMP_ColliderInstance> hitboxes, int index)
        {
            List<FMP_ColliderInstance> hitboxTree = new List<FMP_ColliderInstance>();

            int next = index;

            while (next != -1)
            {
                if (next == -1 || next >= hitboxes.Count)
                    throw new ArgumentException($"FMP_HitboxGroup.CreateHitboxTree: Hitbox index was out of range");

                hitboxes[next].Name = hitboxGroup.UnorderedHitboxList[next].Name;
                hitboxTree.Add(hitboxes[next]);

                if (hitboxGroup.UnorderedHitboxList[next].ChildIdx != -1)
                    hitboxes[next].ColliderInstances.AddRange(CreateHitboxTree(hitboxGroup, hitboxes, hitboxGroup.UnorderedHitboxList[next].ChildIdx));

                next = hitboxGroup.UnorderedHitboxList[next].SiblingIdx;
            }

            return hitboxTree;
        }

        private static List<FMP_ColliderInstance> WriteDefaultTree(List<FMP_Collider> colliders, int defaultParam1 = 1, int defaultParam2 = 4)
        {
            List<FMP_ColliderInstance> hitboxes = new List<FMP_ColliderInstance>();

            for (int i = 0; i < colliders.Count; i++)
            {
                FMP_ColliderInstance colliderInstance = new FMP_ColliderInstance();
                colliderInstance.Matrix = FMP_Transform.GetDefault();

                var numHavokGroups = colliders[i].HavokColliders.GroupBy(x => x.Group).Select(g => new { Name = g.Key, Count = g.Count() });
                colliderInstance.HavokGroupParameters = new List<FMP_HavokGroupParameters>();

                foreach (var havokGroup in numHavokGroups)
                    colliderInstance.HavokGroupParameters.Add(new FMP_HavokGroupParameters() { Param1 = defaultParam1, Param2 = defaultParam2 });

                hitboxes.Add(colliderInstance);
            }

            return hitboxes;
        }

        internal List<FMP_ColliderInstance> WriteHitboxTree(List<FMP_CollisionGroup> hitboxGroups, FMP_Object parentObj)
        {
            List<FMP_ColliderInstance> hitboxes = new List<FMP_ColliderInstance>();
            if (CollisionGroupIndex == ushort.MaxValue) return hitboxes;

            FMP_CollisionGroup hitboxGroup = hitboxGroups.FirstOrDefault(x => x.Index == CollisionGroupIndex);
            int numColliders = hitboxGroup.ColliderCount;

            if ((ColliderInstances?.Count == 0 || ColliderInstances == null) && numColliders > 0)
            {
                //In the event where no collider instances are provided, we can automatically generate default ones
                return WriteDefaultTree(hitboxGroup.UnorderedHitboxList);
            }
            else
            {
                WriteHitboxTreeRecursive(hitboxes, ColliderInstances);


                if (hitboxGroup == null)
                    throw new Exception("HitboxInstanceGroup: Cannot find HitboxGroup with ID " + CollisionGroupIndex);

                if (numColliders != hitboxes.Count)
                    throw new Exception($"HitboxInstanceGroup: The amount of HitboxInstances does not match the number of Hitboxes in the connected HitboxGroup (Object: {parentObj.Name}, HitboxGroup: {hitboxGroup.Index})");

                return hitboxes;
            }
        }

        private static void WriteHitboxTreeRecursive(List<FMP_ColliderInstance> hitboxes, List<FMP_ColliderInstance> hitboxTree)
        {
            for (int i = 0; i < hitboxTree.Count; i++)
            {
                hitboxes.Add(hitboxTree[i]);

                if (hitboxTree[i].ColliderInstances?.Count > 0)
                    WriteHitboxTreeRecursive(hitboxes, hitboxTree[i].ColliderInstances);
            }
        }

        public void CreateHitboxTreeFromGeneratedCollision(FMP_CollisionGroup collisionGroup, Dictionary<int, FMP_Transform> matrices)
        {
            collisionGroup.CreateUnorderedHitboxList();
            ColliderInstances = WriteDefaultTree(collisionGroup.UnorderedHitboxList, 2, 3);

            for (int i = 0; i < ColliderInstances.Count; i++)
            {
                if (matrices.TryGetValue(i, out FMP_Transform matrix))
                {
                    ColliderInstances[i].Matrix = matrix;
                }
            }
        }

        internal void CreateCollisionInstanceTree(FMP_CollisionGroup collisionGroup, Dictionary<int, FMP_Transform> matrices, Dictionary<int, MeshFlags[]> flags)
        {
            ColliderInstances.Clear();
            CreateHitboxTreeFromGeneratedCollision(collisionGroup, matrices);

            //Set flag values
            //note: ColliderInstances wont be recursive when the tree was made with a CreateHitboxTreeFromGeneratedCollision call, so the following is okay
            for (int i = 0; i < ColliderInstances.Count; i++)
            {
                if (flags.TryGetValue(i, out MeshFlags[] options))
                {
                    if (options.Length != ColliderInstances[i].HavokGroupParameters.Count)
                        throw new Exception("CreateCollisionInstanceTree: Exported MeshOptions count is not the same as HavokGroupParameters");

                    for (int a = 0; a < options.Length; a++)
                    {
                        if (options[a].Param1_Custom != -1)
                        {
                            ColliderInstances[i].HavokGroupParameters[a].Param1 = options[a].Param1_Custom;
                        }
                        else if (options[a].Param1_EdgeVFX)
                        {
                            ColliderInstances[i].HavokGroupParameters[a].Param1 = 3;
                        }
                        else if (options[a].Param1_Float)
                        {
                            ColliderInstances[i].HavokGroupParameters[a].Param1 = 1;
                        }


                        if (options[a].Param2_Custom != -1)
                        {
                            ColliderInstances[i].HavokGroupParameters[a].Param2 = options[a].Param2_Custom;
                        }
                    }
                }
            }
        }
    }

    [YAXSerializeAs("ColliderInstance")]
    public class FMP_ColliderInstance
    {
        [YAXAttributeForClass]
        public string Name { get; set; }

        [CustomSerialize]
        public ushort I_20 { get; set; }
        [CustomSerialize]
        public ushort I_22 { get; set; } = ushort.MaxValue;
        [CustomSerialize(isFloat: true)]
        [YAXShouldSerialize(nameof(ShouldSerializeF24))]
        public float F_24 { get; set; } //Related to BreakableState, possibily health?
        [CustomSerialize(isFloat: true)]
        [YAXAltAliases("F_28/value")]
        [YAXShouldSerialize(nameof(ShouldSerializeBreakableStateDelay))]
        public float BreakableStateDelay { get; set; } //Seconds
        public FMP_Transform Matrix { get; set; }

        [YAXDontSerializeIfNull]
        public List<FMP_HavokGroupParameters> HavokGroupParameters { get; set; }
        [YAXDontSerializeIfNull]
        [YAXAltAliases("SubPart1")]
        public FMP_PhysicsDescriptor PhysicsDescriptor { get; set; }
        [YAXDontSerializeIfNull]
        [YAXAltAliases("SubPart2")]
        public FMP_PhysicsDescriptor BreakableStateDescriptor { get; set; }
        [YAXDontSerializeIfNull]
        public FMP_ActionBlock Action { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "ColliderInstance")]
        public List<FMP_ColliderInstance> ColliderInstances { get; set; } = new List<FMP_ColliderInstance>();

        public static List<FMP_ColliderInstance> ReadAll(byte[] bytes, int offset, int count)
        {
            List<FMP_ColliderInstance> entries = new List<FMP_ColliderInstance>();

            for (int i = 0; i < count; i++)
            {
                entries.Add(Read(bytes, offset + (80 * i)));
            }

            return entries;
        }

        public static FMP_ColliderInstance Read(byte[] bytes, int offset)
        {
            FMP_ColliderInstance colliderInstance = new FMP_ColliderInstance();
            colliderInstance.I_20 = BitConverter.ToUInt16(bytes, offset + 20);
            colliderInstance.I_22 = BitConverter.ToUInt16(bytes, offset + 22);
            colliderInstance.F_24 = BitConverter.ToSingle(bytes, offset + 24);
            colliderInstance.BreakableStateDelay = BitConverter.ToSingle(bytes, offset + 28);
            colliderInstance.Matrix = FMP_Transform.Read(bytes, offset + 32);

            int havokParamsCount = BitConverter.ToInt32(bytes, offset);
            int havokParamsOffset = BitConverter.ToInt32(bytes, offset + 4);
            int descriptorA = BitConverter.ToInt32(bytes, offset + 8);
            int descriptorB = BitConverter.ToInt32(bytes, offset + 12);
            int actionBlock_Offset = BitConverter.ToInt32(bytes, offset + 16);

            if(havokParamsCount > 0)
                colliderInstance.HavokGroupParameters = FMP_HavokGroupParameters.ReadAll(bytes, havokParamsOffset, havokParamsCount);

            if (descriptorA > 0)
                colliderInstance.PhysicsDescriptor = FMP_PhysicsDescriptor.Read(bytes, descriptorA);

            if (descriptorB > 0)
                colliderInstance.BreakableStateDescriptor = FMP_PhysicsDescriptor.Read(bytes, descriptorB);

            if (actionBlock_Offset > 0)
                colliderInstance.Action = FMP_ActionBlock.Read(bytes, actionBlock_Offset);


            return colliderInstance;
        }

        public bool IsEqual(FMP_ColliderInstance other)
        {
            if (I_20 != other.I_20) return false;
            if (I_22 != other.I_22) return false;
            if (F_24 != other.F_24) return false;
            if (BreakableStateDelay != other.BreakableStateDelay) return false;
            if (!Matrix.IsEqual(other.Matrix)) return false;
            if (HavokGroupParameters.Count != other.HavokGroupParameters.Count) return false;
            if ((PhysicsDescriptor == null && other.PhysicsDescriptor != null) || (PhysicsDescriptor != null && other.PhysicsDescriptor == null)) return false;
            if ((BreakableStateDescriptor == null && other.BreakableStateDescriptor != null) || (BreakableStateDescriptor != null && other.BreakableStateDescriptor == null)) return false;
            if ((Action == null && other.Action != null) || (Action != null && other.Action == null)) return false;

            if (HavokGroupParameters != null)
            {
                for (int i = 0; i < HavokGroupParameters.Count; i++)
                {
                    //if (IndexPairs[i].Index1 != other.IndexPairs[i].Index1) return false;
                    //if (IndexPairs[i].Index0 != other.IndexPairs[i].Index0) return false;
                }
            }

            if (PhysicsDescriptor != null)
            {
                if (!PhysicsDescriptor.IsEqual(other.PhysicsDescriptor)) return false;
            }

            if (BreakableStateDescriptor != null)
            {
                if (!BreakableStateDescriptor.IsEqual(other.BreakableStateDescriptor)) return false;
            }

            if (Action != null)
            {
                if (!Action.IsEqual(other.Action)) return false;
            }

            return true;

        }

        private bool ShouldSerializeF24() => BreakableStateDescriptor != null || F_24 != 0f;
        private bool ShouldSerializeBreakableStateDelay() => BreakableStateDescriptor != null || BreakableStateDelay != 0f;
    }

    [YAXSerializeAs("PhysicsDescriptor")]
    public class FMP_PhysicsDescriptor
    {

        [CustomSerialize]
        [YAXAltAliases("I_00/value:ushort")]
        public short PhysicsMaterialIndex { get; set; } = -1;
        [CustomSerialize]
        [YAXAltAliases("I_02/value:ushort")]
        public short MotionPresetIndex { get; set; } = -1;
        [CustomSerialize]
        public ushort I_04 { get; set; }
        [CustomSerialize]
        [YAXAltAliases("I_06/value")]
        public HavokMotionType MotionType { get; set; }
        [CustomSerialize]
        [YAXAltAliases("I_08/value")]
        [YAXShouldSerialize(nameof(ShouldSerializeCollisionLayer))]
        public int CollisionLayer { get; set; } = -1; //First 6 bits: CollisionLayer (range 0 - 63)

        [CustomSerialize(isFloat: true)]
        public float Mass { get; set; }
        [YAXComment("The value names below are unconfirmed")]
        [CustomSerialize(parent: "InitialLinearVelocity", serializeAs: "X", isFloat: true)]
        [YAXAltAliases("F_16/value")]
        public float InitialLinearVelocityX { get; set; }
        [CustomSerialize(parent: "InitialLinearVelocity", serializeAs: "Y", isFloat: true)]
        [YAXAltAliases("F_20/value")]
        public float InitialLinearVelocityY { get; set; }
        [CustomSerialize(parent: "InitialLinearVelocity", serializeAs: "Z", isFloat: true)]
        [YAXAltAliases("F_24/value")]
        public float InitialLinearVelocityZ { get; set; }
        [CustomSerialize(parent: "InitialAngularVelocity", serializeAs: "X", isFloat: true)]
        [YAXAltAliases("F_28/value")]
        public float InitialAngularVelocityX { get; set; }
        [CustomSerialize(parent: "InitialAngularVelocity", serializeAs: "Y", isFloat: true)]
        [YAXAltAliases("F_32/value")]
        public float InitialAngularVelocityY { get; set; }
        [CustomSerialize(parent: "InitialAngularVelocity", serializeAs: "Z", isFloat: true)]
        [YAXAltAliases("F_36/value")]
        public float InitialAngularVelocityZ { get; set; }
        [CustomSerialize(parent: "CenterOfMass", serializeAs: "X", isFloat: true)]
        [YAXAltAliases("F_40/value")]
        public float CenterOfMassX { get; set; }
        [CustomSerialize(parent: "CenterOfMass", serializeAs: "Y", isFloat: true)]
        [YAXAltAliases("F_44/value")]
        public float CenterOfMassY { get; set; }
        [CustomSerialize(parent: "CenterOfMass", serializeAs: "Z", isFloat: true)]
        [YAXAltAliases("F_48/value")]
        public float CenterOfMassZ { get; set; }
        [CustomSerialize(isFloat: true)]
        [YAXAltAliases("F_52/value")]
        public float Volume { get; set; }

        //Unit mass
        [CustomSerialize(parent: "Inertia", serializeAs: "X", isFloat: true)]
        [YAXAltAliases("Width/X")]
        public float InertiaX { get; set; }
        [CustomSerialize(parent: "Inertia", serializeAs: "Y", isFloat: true)]
        [YAXAltAliases("Width/Y")]
        public float InertiaY { get; set; }
        [CustomSerialize(parent: "Inertia", serializeAs: "Z", isFloat: true)]
        [YAXAltAliases("Width/Z")]
        public float InertiaZ { get; set; }
        [CustomSerialize(parent: "Quaternion", serializeAs: "X", isFloat: true)]
        [YAXAltAliases("Quaternion/X")]
        public float QuaternionX { get; set; }
        [CustomSerialize(parent: "Quaternion", serializeAs: "Y", isFloat: true)]
        [YAXAltAliases("Quaternion/Y")]
        public float QuaternionY { get; set; }
        [CustomSerialize(parent: "Quaternion", serializeAs: "Z", isFloat: true)]
        [YAXAltAliases("Quaternion/Z")]
        public float QuaternionZ { get; set; }
        [CustomSerialize(parent: "Quaternion", serializeAs: "W", isFloat: true)]
        [YAXAltAliases("Quaternion/W")]
        public float QuaternionW { get; set; }

        public byte[] Write()
        {
            List<byte> bytes = new List<byte>(84);

            bytes.AddRange(BitConverter.GetBytes(PhysicsMaterialIndex));
            bytes.AddRange(BitConverter.GetBytes(MotionPresetIndex));
            bytes.AddRange(BitConverter.GetBytes(I_04));
            bytes.AddRange(BitConverter.GetBytes((ushort)MotionType));
            bytes.AddRange(BitConverter.GetBytes(CollisionLayer));
            bytes.AddRange(BitConverter.GetBytes(Mass));
            bytes.AddRange(BitConverter.GetBytes(InitialLinearVelocityX));
            bytes.AddRange(BitConverter.GetBytes(InitialLinearVelocityY));
            bytes.AddRange(BitConverter.GetBytes(InitialLinearVelocityZ));
            bytes.AddRange(BitConverter.GetBytes(InitialAngularVelocityX));
            bytes.AddRange(BitConverter.GetBytes(InitialAngularVelocityY));
            bytes.AddRange(BitConverter.GetBytes(InitialAngularVelocityZ));
            bytes.AddRange(BitConverter.GetBytes(CenterOfMassX));
            bytes.AddRange(BitConverter.GetBytes(CenterOfMassY));
            bytes.AddRange(BitConverter.GetBytes(CenterOfMassZ));
            bytes.AddRange(BitConverter.GetBytes(Volume));
            bytes.AddRange(BitConverter.GetBytes(InertiaX));
            bytes.AddRange(BitConverter.GetBytes(InertiaY));
            bytes.AddRange(BitConverter.GetBytes(InertiaZ));
            bytes.AddRange(BitConverter.GetBytes(QuaternionX));
            bytes.AddRange(BitConverter.GetBytes(QuaternionY));
            bytes.AddRange(BitConverter.GetBytes(QuaternionZ));
            bytes.AddRange(BitConverter.GetBytes(QuaternionW));

            if (bytes.Count != 84)
                throw new Exception("FMP_FMP_ObjectSubPart.Write: Invalid size");

            return bytes.ToArray();
        }

        public static FMP_PhysicsDescriptor Read(byte[] bytes, int offset)
        {
            return new FMP_PhysicsDescriptor()
            {
                PhysicsMaterialIndex = BitConverter.ToInt16(bytes, offset),
                MotionPresetIndex = BitConverter.ToInt16(bytes, offset + 2),
                I_04 = BitConverter.ToUInt16(bytes, offset + 4),
                MotionType = (HavokMotionType)BitConverter.ToUInt16(bytes, offset + 6),
                CollisionLayer = BitConverter.ToInt32(bytes, offset + 8),
                Mass = BitConverter.ToSingle(bytes, offset + 12),
                InitialLinearVelocityX = BitConverter.ToSingle(bytes, offset + 16),
                InitialLinearVelocityY = BitConverter.ToSingle(bytes, offset + 20),
                InitialLinearVelocityZ = BitConverter.ToSingle(bytes, offset + 24),
                InitialAngularVelocityX = BitConverter.ToSingle(bytes, offset + 28),
                InitialAngularVelocityY = BitConverter.ToSingle(bytes, offset + 32),
                InitialAngularVelocityZ = BitConverter.ToSingle(bytes, offset + 36),
                CenterOfMassX = BitConverter.ToSingle(bytes, offset + 40),
                CenterOfMassY = BitConverter.ToSingle(bytes, offset + 44),
                CenterOfMassZ = BitConverter.ToSingle(bytes, offset + 48),
                Volume = BitConverter.ToSingle(bytes, offset + 52),
                InertiaX = BitConverter.ToSingle(bytes, offset + 56),
                InertiaY = BitConverter.ToSingle(bytes, offset + 60),
                InertiaZ = BitConverter.ToSingle(bytes, offset + 64),
                QuaternionX = BitConverter.ToSingle(bytes, offset + 68),
                QuaternionY = BitConverter.ToSingle(bytes, offset + 72),
                QuaternionZ = BitConverter.ToSingle(bytes, offset + 76),
                QuaternionW = BitConverter.ToSingle(bytes, offset + 80),
            };
        }

        public bool IsEqual(FMP_PhysicsDescriptor other)
        {
            if (InertiaX != other.InertiaX) return false;
            if (InertiaY != other.InertiaY) return false;
            if (InertiaZ != other.InertiaZ) return false;
            if (QuaternionX != other.QuaternionX) return false;
            if (QuaternionY != other.QuaternionY) return false;
            if (QuaternionZ != other.QuaternionZ) return false;
            if (QuaternionW != other.QuaternionW) return false;
            if (PhysicsMaterialIndex != other.PhysicsMaterialIndex) return false;
            if (MotionPresetIndex != other.MotionPresetIndex) return false;
            if (I_04 != other.I_04) return false;
            if (MotionType != other.MotionType) return false;
            if (CollisionLayer != other.CollisionLayer) return false;
            if (Mass != other.Mass) return false;
            if (InitialLinearVelocityX != other.InitialLinearVelocityX) return false;
            if (InitialLinearVelocityY != other.InitialLinearVelocityY) return false;
            if (InitialLinearVelocityZ != other.InitialLinearVelocityZ) return false;
            if (InitialAngularVelocityX != other.InitialAngularVelocityX) return false;
            if (InitialAngularVelocityY != other.InitialAngularVelocityY) return false;
            if (InitialAngularVelocityZ != other.InitialAngularVelocityZ) return false;
            if (CenterOfMassX != other.CenterOfMassX) return false;
            if (CenterOfMassY != other.CenterOfMassY) return false;
            if (CenterOfMassZ != other.CenterOfMassZ) return false;
            if (Volume != other.Volume) return false;

            return true;
        }

        private bool ShouldSerializeCollisionLayer() => CollisionLayer != -1;
    }

}
