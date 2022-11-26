using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using YAXLib;

namespace Xv2CoreLib.QED
{
    public class Deserializer
    {
        string saveLocation;
        QED_File qed_File;
        public List<byte> bytes { get; private set; } = new List<byte>() { 35, 81, 69, 68, 254, 255, 0, 0 };

        public Deserializer(string location)
        {
            saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(location), Path.GetFileNameWithoutExtension(location));
            YAXSerializer serializer = new YAXSerializer(typeof(QED_File), YAXSerializationOptions.DontSerializeNullObjects);
            qed_File = (QED_File)serializer.DeserializeFromFile(location);
            ValidateFile();
            WriteBinaryFile();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public Deserializer(QED_File qedFile)
        {
            qed_File = qedFile;
            ValidateFile();
            WriteBinaryFile();
        }

        private void ValidateFile()
        {
            if (qed_File.Events == null)
                return;

            for (int i = 0; i < qed_File.Events.Count; i++) 
            {
                if(qed_File.Events.Any(x => x.ID == qed_File.Events[i].ID && x.SubIndex == qed_File.Events[i].SubIndex && x != qed_File.Events[i]))
                {
                    throw new Exception("QED: An Event with a Index of " + qed_File.Events[i].ID + " and a subIndex of " + qed_File.Events[i].SubIndex + " already exists. There cannot be duplicates.");
                }
            }
        }

        private void WriteBinaryFile()
        {
            if(qed_File.Events == null)
            {
                bytes.AddRange(new byte[4]);
                bytes.AddRange(BitConverter.GetBytes(16));
                return;
            }

            bytes.AddRange(BitConverter.GetBytes(1));
            bytes.AddRange(BitConverter.GetBytes(16));
            bytes.AddRange(new byte[4]);

            bytes.AddRange(BitConverter.GetBytes(CountOfEvents()));
            bytes.AddRange(BitConverter.GetBytes(40));
            bytes.AddRange(BitConverter.GetBytes(CountOfActions()));
            bytes.AddRange(new byte[8]);

            //stored offsets / Write Main Data
            List<int> event1Offsets = WriteEvents1();
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), 32);
            List<int> event2Offsets = WriteEvents2();

            //Write Condition Types
            int offsetAccess = 0;
            for (int i = 0; i < qed_File.Events.Count; i++)
            {
                if (qed_File.Events[i].Conditions != null) {
                    for (int a = 0; a < qed_File.Events[i].Conditions.Count(); a++)
                    {
                        bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), event1Offsets[offsetAccess]);
                        WriteConditionType(qed_File.Events[i].Conditions[a]);
                        offsetAccess++;
                    }
                }
            }

            offsetAccess = 0;

            //Write Action Types
            for (int i = 0; i < qed_File.Events.Count(); i++)
            {
                if (qed_File.Events[i].Actions != null)
                {
                    for (int a = 0; a < qed_File.Events[i].Actions.Count(); a++)
                    {
                        bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), event2Offsets[offsetAccess]);
                        WriteActionType(qed_File.Events[i].Actions[a]);
                        offsetAccess++;
                    }
                }
            }
            
        }

        //Counts Events and Actions, since a simple count check wont work (due to the special grouping)

        private int CountOfEvents() {
            int count = 0;

            for (int i = 0; i < qed_File.Events.Count(); i++) {
                if (qed_File.Events[i].Conditions != null) {
                    count += qed_File.Events[i].Conditions.Count();
                }
            }

            return count;
        }

        private int CountOfActions() {
            int count = 0;

            for (int i = 0; i < qed_File.Events.Count(); i++) {
                if (qed_File.Events[i].Actions != null)
                {
                    for (int a = 0; a < qed_File.Events[i].Actions.Count(); a++)
                    {
                        count++;
                    }
                }
            }
            return count;
        }


        //Write Events1 and Events2. Return a List with Offset positions (to insert the offset into later)

        private List<int> WriteEvents1() {
            List<int> offsetList = new List<int>();

            for (int i = 0; i < qed_File.Events.Count(); i++) {
                if (qed_File.Events[i].Conditions != null) {
                    for (int a = 0; a < qed_File.Events[i].Conditions.Count(); a++)
                    {
                        bytes.AddRange(BitConverter.GetBytes(qed_File.Events[i].Conditions[a].Type));
                        bytes.AddRange(BitConverter.GetBytes(qed_File.Events[i].SubIndex));
                        bytes.AddRange(BitConverter.GetBytes(qed_File.Events[i].ID));
                        bytes.AddRange(BitConverter.GetBytes(qed_File.Events[i].Conditions[a].I_06));
                        offsetList.Add(bytes.Count());
                        bytes.AddRange(new byte[4]);
                    }
                }
            }

            return offsetList;
        }

        private List<int> WriteEvents2() {
            List<int> offsetList = new List<int>();

            for (int i = 0; i < qed_File.Events.Count(); i++)
            {
                if (qed_File.Events[i].Actions != null)
                {
                    for (int a = 0; a < qed_File.Events[i].Actions.Count(); a++)
                    {
                        bytes.AddRange(BitConverter.GetBytes(qed_File.Events[i].Actions[a].Type));
                        bytes.AddRange(BitConverter.GetBytes(qed_File.Events[i].SubIndex));
                        bytes.AddRange(BitConverter.GetBytes(qed_File.Events[i].ID));
                        bytes.AddRange(BitConverter.GetBytes(qed_File.Events[i].Actions[a].I_06));
                        offsetList.Add(bytes.Count());
                        bytes.AddRange(new byte[4]);
                    }
                }
            }

            return offsetList;

        }



        // Type Writer Manager methods

        private void WriteActionType(Action action) 
        {
            int type = action.Type;
            
            switch (type) {
                case 2:
                    WriteTemplate1(action.Template_1);
                    break;
                case 6:
                    WriteTemplate2(action.LOAD_DEMO);
                    break;
                case 8:
                    WriteGeneric(action.PLAY_DEMO);
                    break;
                case 13:
                    WriteTemplate2(action.Template_2);
                    break;
                case 16:
                    WriteGeneric(action.CHARA_LEAVE16);
                    break;
                case 19:
                    WriteGeneric(action.QUEST_CLEAR);
                    break;
                case 21:
                    WriteGeneric(action.QUEST_START);
                    break;
                case 23:
                    WriteGeneric(action.ADVANCE_INDEX);
                    break;
                case 25:
                    WriteGeneric(action.PORTAL);
                    break;
                case 26:
                    WriteGeneric(action.PLAY_AUDIO);
                    break;
                case 27:
                    WriteGeneric(action.CHARA_SPAWN);
                    break;
                case 33:
                    WriteGeneric(action.QUEST_FINISH_STATE);
                    break;
                case 34:
                    WriteTemplate5(action.BOOLEAN_SET);
                    break;
                case 35:
                    WriteGeneric(action.ULT_FINISH_START);
                    break;
                case 36:
                    WriteTemplate4(action.Template_4);
                    break;
                case 37:
                    WriteTemplate3(action.HEALTH_CAP);
                    break;
                case 39:
                    WriteGeneric(action.STATS);
                    break;
                case 41:
                    WriteGeneric(action.USE_SKILL);
                    break;
                case 45:
                    WriteGeneric(action.MULTI_CHARA_SPAWN);
                    break;
                case 46:
                    WriteGeneric(action.CHARA_LEAVE);
                    break;
                case 49:
                    WriteGeneric(action.TRANSFORM);
                    break;
                case 52:
                    WriteTemplate2(action.Template_2);
                    break;
                case 54:
                    WriteGeneric(action.TRANSFORM_CHARA_SWAP);
                    break;
                case 55:
                    WriteGeneric(action.PRELOAD_CHARA);
                    break;
                case 57:
                    WriteTemplate1(action.WAIT);
                    break;
                case 58:
                    WriteGeneric(action.CHANGE_STAGE);
                    break;
                case 67:
                    WriteTemplate2(action.LOAD_BEV);
                    break;
                case 71:
                    WriteGeneric(action.TUT_POPUP);
                    break;
                case 83:
                    WriteGeneric(action.TRANSFORM_CHARA_SWAP1);
                    break;
                case 85:
                    WriteTemplate4(action.ADD_TIME);
                    break;
                case 88:
                    WriteGeneric(action.SKILLS_EQUIP);
                    break;
                case 96:
                    WriteTemplate2(action.Template_2);
                    break;
                default:
                    WriteGeneric(action.TemplateGeneric);
                    break;
            }
        }

        private void WriteConditionType(Condition condition)
        {
            int type = condition.Type;
            
            switch (type)
            {
                case 3:
                    WriteGeneric(condition.KO_TRIGGER);
                    break;
                case 4:
                    WriteTemplate1(condition.TIME_PASSED_LOCAL);
                    break;
                case 8:
                    WriteTemplate4(condition.TIME_PASSED_GLOBAL);
                    break;
                case 9:
                    WriteTemplate3(condition.HEALTH_TRIGGER);
                    break;
                case 16:
                    WriteTemplate4(condition.Template_4);
                    break;
                case 20:
                    WriteTemplate5(condition.BOOLEAN_CHECK);
                    break;
                case 25:
                    WriteGeneric(condition.HIT_WITH_SKILL);
                    break;
                case 26:
                    WriteGeneric(condition.KO_WITH_SKILL);
                    break;
                case 27:
                    WriteGeneric(condition.ON_AUDIO_FINISH_TRIGGER);
                    break;
                case 33:
                    WriteGeneric(condition.DELAYED_KO_TRIGGER);
                    break;
                case 42:
                    WriteGeneric(condition.AFTER_EVENT);
                    break;
                case 48:
                    WriteTemplate4(condition.Template_4);
                    break;
                default:
                    WriteGeneric(condition.TemplateGeneric);
                    break;
            }
        }




        //Everything below is just for writing types. 

        private void WriteGeneric<T>(T template) where T : IGenericTemplate, new()
        {
            bytes.AddRange(BitConverter.GetBytes(template.I_00));
            bytes.AddRange(BitConverter.GetBytes(template.I_04));
            bytes.AddRange(BitConverter.GetBytes(template.I_08));
            bytes.AddRange(BitConverter.GetBytes(template.I_12));
            bytes.AddRange(BitConverter.GetBytes(template.I_16));
            bytes.AddRange(BitConverter.GetBytes(template.I_20));
            bytes.AddRange(BitConverter.GetBytes(template.I_24));
            bytes.AddRange(BitConverter.GetBytes(template.I_28));
        }

        private void WriteTemplate1<T>(T template) where T : ITemplate1, new()
        {
            bytes.AddRange(BitConverter.GetBytes(template.F_00));
            bytes.AddRange(BitConverter.GetBytes(template.I_04));
            bytes.AddRange(BitConverter.GetBytes(template.I_08));
            bytes.AddRange(BitConverter.GetBytes(template.I_12));
            bytes.AddRange(BitConverter.GetBytes(template.I_16));
            bytes.AddRange(BitConverter.GetBytes(template.I_20));
            bytes.AddRange(BitConverter.GetBytes(template.I_24));
            bytes.AddRange(BitConverter.GetBytes(template.I_28));
        }

        private void WriteTemplate2<T>(T template) where T : ITemplate2, new()
        {
            bytes.AddRange(Encoding.ASCII.GetBytes(template.Str_00));
            int remainingSpace = 20 - template.Str_00.Count();
            for (int i = 0; i < remainingSpace; i++)
            {
                bytes.Add(0);
            }
            bytes.AddRange(BitConverter.GetBytes(template.I_20));
            bytes.AddRange(BitConverter.GetBytes(template.I_24));
            bytes.AddRange(BitConverter.GetBytes(template.I_28));
        }

        private void WriteTemplate3<T>(T template) where T : ITemplate3, new()
        {
            bytes.AddRange(BitConverter.GetBytes(template.I_00));
            bytes.AddRange(BitConverter.GetBytes(template.I_04));
            bytes.AddRange(BitConverter.GetBytes(template.F_08));
            bytes.AddRange(BitConverter.GetBytes(template.I_12));
            bytes.AddRange(BitConverter.GetBytes(template.I_16));
            bytes.AddRange(BitConverter.GetBytes(template.I_20));
            bytes.AddRange(BitConverter.GetBytes(template.I_24));
            bytes.AddRange(BitConverter.GetBytes(template.I_28));
        }

        private void WriteTemplate4<T>(T template) where T : ITemplate4, new()
        {
            bytes.AddRange(BitConverter.GetBytes(template.I_00));
            bytes.AddRange(BitConverter.GetBytes(template.F_04));
            bytes.AddRange(BitConverter.GetBytes(template.I_08));
            bytes.AddRange(BitConverter.GetBytes(template.I_12));
            bytes.AddRange(BitConverter.GetBytes(template.I_16));
            bytes.AddRange(BitConverter.GetBytes(template.I_20));
            bytes.AddRange(BitConverter.GetBytes(template.I_24));
            bytes.AddRange(BitConverter.GetBytes(template.I_28));
        }

        private void WriteTemplate5<T>(T template) where T : ITemplate5, new()
        {
            bytes.AddRange(BitConverter.GetBytes(template.I_00));
            if (template.FLAG == false)
            {
                bytes.AddRange(BitConverter.GetBytes(0));
            }
            else
            {
                bytes.AddRange(BitConverter.GetBytes(1));
            }
            bytes.AddRange(BitConverter.GetBytes(template.I_08));
            bytes.AddRange(BitConverter.GetBytes(template.I_12));
            bytes.AddRange(BitConverter.GetBytes(template.I_16));
            bytes.AddRange(BitConverter.GetBytes(template.I_20));
            bytes.AddRange(BitConverter.GetBytes(template.I_24));
            bytes.AddRange(BitConverter.GetBytes(template.I_28));
        }

    }
}
