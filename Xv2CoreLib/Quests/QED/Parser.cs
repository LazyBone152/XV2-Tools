using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using YAXLib;

namespace Xv2CoreLib.QED
{
    public class Parser
    {
        string saveLocation;
        byte[] rawBytes;
        QED_File qed_File = new QED_File();
        bool writeXml = false;
        bool isFinished = false;

        public Parser(string location, bool _writeXml)
        {
            writeXml = _writeXml;
            rawBytes = File.ReadAllBytes(location);
            saveLocation = location;
            ParseFile();
            isFinished = true;
            if (writeXml == true) 
            {
                WriteXmlFile();
            }
        }

        void ParseFile() 
        {
            if (BitConverter.ToInt32(rawBytes, 8) == 0)
            {
                return;
            }

            int event1Offset = BitConverter.ToInt32(rawBytes, 24);
            int event2Offset = BitConverter.ToInt32(rawBytes, 32);
            int event1Count = BitConverter.ToInt32(rawBytes, 20);
            int event2Count = BitConverter.ToInt32(rawBytes, 28);

            ParseEvents1(event1Offset, event1Count);
            ParseEvents2(event2Offset, event2Count);
        }

        void WriteXmlFile()
        {
            YAXSerializer serializer = new YAXSerializer(typeof(QED_File));
            serializer.SerializeToFile(qed_File, saveLocation + ".xml");
        }


        void ParseEvents2 (int offset, int count) {
            int eventIndex = 0; //index of Event object list
            int previousIndex = -532532;
            for (int i = 0; i < count; i++) {
                int index = BitConverter.ToInt16(rawBytes, offset + 4);
                int subIndex = BitConverter.ToInt16(rawBytes, offset + 2);
                
                if (qed_File.Events[eventIndex].Index == index && qed_File.Events[eventIndex].SubIndex == subIndex)
                {
                    //same index. Put these Actions in this Event
                    if (qed_File.Events[eventIndex].Actions == null) {
                        qed_File.Events[eventIndex].Actions = new List<Action>();
                    }
                    qed_File.Events[eventIndex].Actions.Add(GetAction(offset));
                    offset += 12;
                    previousIndex = index;
                }
                else {
                    //different index numbers. Logic to set the eventIndex to the correct one (can scan backwards, in case the index belongs to a previous one)
                    bool foundIndex = false;

                    for (int a = 0; a < qed_File.Events.Count(); a++) {
                        if (qed_File.Events[a].Index == index && qed_File.Events[a].SubIndex == subIndex) {
                            eventIndex = a;
                            foundIndex = true;
                            break;
                        }
                    }

                    if (foundIndex == false) {
                        //No corresponding index can be found, so create a "condition-less" one

                        for (int a = 0; a < qed_File.Events.Count(); a++) {
                            if (qed_File.Events[a].Index > index) {
                                eventIndex = a;
                                break;
                            }
                        }

                        qed_File.Events.Insert(eventIndex, new Event() {
                            Index = (short)index,
                            SubIndex = (short)subIndex,
                            CONDITIONS = new List<string>() { "NONE"}
                        });
                    }

                    i--;
                }

                
            }
        }

        void ParseEvents1 (int offset, int count) {
            qed_File.Events = new List<Event>();
            int access = -1;
            int prevIndex = -554;
            int prevSubIndex = -554;
            for (int i = 0; i < count; i++) {
                int nowSubIndx = BitConverter.ToInt16(rawBytes, offset + 2);
                int nowIndx = BitConverter.ToInt16(rawBytes, offset + 4);
                

                if (prevIndex == nowIndx && prevSubIndex == nowSubIndx)
                {
                    //same index level as previous
                    if (qed_File.Events[access].CONDITIONS == null) {
                        qed_File.Events[access].CONDITIONS = new List<string>();
                    }
                    Condition condition = GetCondition(offset);
                    qed_File.Events[access].CONDITIONS.Add(condition.CONDITION);
                    qed_File.Events[access].Conditions.Add(condition);
                    offset += 12;

                }
                else {

                    //new index level. Create new Event and decreace i by 1 (to go through above code with this entry)
                    access++;
                    qed_File.Events.Add(new Event());
                    qed_File.Events[access].Conditions = new List<Condition>();
                    qed_File.Events[access].Index = (short)nowIndx;
                    qed_File.Events[access].SubIndex = (short)nowSubIndx;
                    prevIndex = nowIndx;
                    prevSubIndex = nowSubIndx;
                    i--;

                }
            }
        }

        Action GetAction(int offset) {
            Action action = new Action();
            action.Type = BitConverter.ToInt16(rawBytes, offset + 0);
            action.I_06 = BitConverter.ToInt16(rawBytes, offset + 6);
            int offsetToData = BitConverter.ToInt32(rawBytes, offset + 8);

            switch (action.Type)
            {
                case 2:
                    action.Template_1 = GetTemplate1<QED_Types.Template1>(offsetToData);
                    action.ACTION = "UNKNOWN";
                    break;
                case 6:
                    action.LOAD_DEMO = GetTemplate2<QED_Types.TYPE6_LOAD_DEMO>(offsetToData);
                    action.ACTION = action.LOAD_DEMO.DESCRIPTION;
                    break;
                case 8:
                    action.PLAY_DEMO = GetGenericTemplate<QED_Types.TYPE8_PLAY_DEMO>(offsetToData);
                    action.ACTION = action.PLAY_DEMO.DESCRIPTION;
                    break;
                case 13:
                    action.Template_2 = GetTemplate2<QED_Types.Template2>(offsetToData);
                    action.ACTION = "UNKNOWN";
                    break;
                case 16:
                    action.CHARA_LEAVE16 = GetGenericTemplate<QED_Types.TYPE16_CHARA_LEAVE>(offsetToData);
                    action.ACTION = "CHARA_LEAVE_0";
                    break;
                case 19:
                    action.QUEST_CLEAR = GetGenericTemplate<QED_Types.TYPE19_QUEST_CLEAR>(offsetToData);
                    action.ACTION = "QUEST_CLEAR";
                    break;
                case 21:
                    action.QUEST_START = GetGenericTemplate<QED_Types.TYPE21_QUEST_START>(offsetToData);
                    action.ACTION = "QUEST_START";
                    break;
                case 23:
                    action.ADVANCE_INDEX = GetGenericTemplate<QED_Types.TYPE23_ADVANCE_INDEX>(offsetToData);
                    action.ACTION = "ADVANCE_INDEX";
                    break;
                case 25:
                    action.PORTAL = GetGenericTemplate<QED_Types.TYPE25_PORTAL>(offsetToData);
                    action.ACTION = "PORTAL";
                    break;
                case 26:
                    action.PLAY_AUDIO = GetGenericTemplate<QED_Types.TYPE26_PLAY_AUDIO>(offsetToData);
                    action.ACTION = "PLAY_AUDIO";
                    break;
                case 27:
                    action.CHARA_SPAWN = GetGenericTemplate<QED_Types.TYPE27_CHARA_SPAWN>(offsetToData);
                    action.ACTION = "CHARA_SPAWN";
                    break;
                case 33:
                    action.QUEST_FINISH_STATE = GetGenericTemplate<QED_Types.TYPE33_FINISH_STATE>(offsetToData);
                    action.ACTION = "QUEST_FINISH_STATE";
                    break;
                case 34:
                    action.BOOLEAN_SET = GetTemplate5<QED_Types.TYPE34_GLOBAL_TRIGGER_SEND>(offsetToData);
                    action.ACTION = "BOOLEAN_SET";
                    break;
                case 35:
                    action.ULT_FINISH_START = GetGenericTemplate<QED_Types.TYPE35_ULT_FINISH_START>(offsetToData);
                    action.ACTION = "ULTIMATE_FINISH_START";
                    break;
                case 36:
                    action.Template_4 = GetTemplate4<QED_Types.Template4>(offsetToData);
                    action.ACTION = "UNKNOWN";
                    break;
                case 37:
                    action.HEALTH_CAP = GetTemplate3<QED_Types.TYPE37_HEALTH_CAP>(offsetToData);
                    action.ACTION = "HEALTH_CAP";
                    break;
                case 39:
                    action.STATS = GetGenericTemplate<QED_Types.TYPE39_STATS>(offsetToData);
                    action.ACTION = action.STATS.DESCRIPTION;
                    break;
                case 41:
                    action.USE_SKILL = GetGenericTemplate<QED_Types.TYPE41_USE_SKILL>(offsetToData);
                    action.ACTION = action.USE_SKILL.DESCRIPTION;
                    break;
                case 45:
                    action.MULTI_CHARA_SPAWN = GetGenericTemplate<QED_Types.TYPE45_MULTI_CHARA_SPAWN>(offsetToData);
                    action.ACTION = action.MULTI_CHARA_SPAWN.DESCRIPTION;
                    break;
                case 46:
                    action.CHARA_LEAVE = GetGenericTemplate<QED_Types.TYPE46_CHARA_LEAVE>(offsetToData);
                    action.ACTION = "CHARA_LEAVE";
                    break;
                case 49:
                    action.TRANSFORM = GetGenericTemplate<QED_Types.TYPE49_TRANSFORM>(offsetToData);
                    action.ACTION = "TRANSFORM";
                    break;
                case 52:
                    action.Template_2 = GetTemplate2<QED_Types.Template2>(offsetToData);
                    action.ACTION = "UNKNOWN";
                    break;
                case 54:
                    action.TRANSFORM_CHARA_SWAP = GetGenericTemplate<QED_Types.TYPE54_TRANSFORM_CHARA_SWAP>(offsetToData);
                    action.ACTION = "TRANSFORM_CHARA_SWAP";
                    break;
                case 55:
                    action.PRELOAD_CHARA = GetGenericTemplate < QED_Types.TYPE55_PRELOAD_CHARA>(offsetToData);
                    action.ACTION = "PRELOAD_CHARA";
                    break;
                case 57:
                    action.WAIT = GetTemplate1<QED_Types.TYPE57_WAIT>(offsetToData);
                    action.ACTION = action.WAIT.DESCRIPTION;
                    break;
                case 58:
                    action.CHANGE_STAGE = GetGenericTemplate < QED_Types.TYPE58_CHANGE_STAGE>(offsetToData);
                    action.ACTION = action.CHANGE_STAGE.DESCRIPTION;
                    break;
                case 71:
                    action.TUT_POPUP = GetGenericTemplate < QED_Types.TYPE71_TUT_POPUP>(offsetToData);
                    action.ACTION = action.TUT_POPUP.DESCRIPTION;
                    break;
                case 67:
                    action.LOAD_BEV = GetTemplate2 < QED_Types.TYPE67_LOAD_BEV>(offsetToData);
                    action.ACTION = action.LOAD_BEV.DESCRIPTION;
                    break;
                case 83:
                    action.TRANSFORM_CHARA_SWAP1 = GetGenericTemplate < QED_Types.TYPE83_TRANSFORM_CHARA_SWAP_1>(offsetToData);
                    action.ACTION = action.TRANSFORM_CHARA_SWAP1.DESCRIPTION;
                    break;
                case 85:
                    action.ADD_TIME = GetTemplate4 < QED_Types.TYPE85_ADD_TIME>(offsetToData);
                    action.ACTION = action.ADD_TIME.DESCRIPTION;
                    break;
                case 88:
                    action.SKILLS_EQUIP = GetGenericTemplate < QED_Types.TYPE88_SKILLS_EQUIP>(offsetToData);
                    action.ACTION = "SKILLS_EQUIP";
                    break;
                case 96:
                    action.Template_2 = GetTemplate2<QED_Types.Template2>(offsetToData);
                    action.ACTION = "UNKNOWN";
                    break;
                default:
                    action.TemplateGeneric = GetGenericTemplate< QED_Types.Template0>(offsetToData);
                    action.ACTION = "UNKNOWN";
                    break;
            }
            return action;
            
            
        }

        Condition GetCondition(int offset) {
            Condition condition = new Condition();
            condition.Type = BitConverter.ToInt16(rawBytes, offset + 0);
            condition.I_06 = BitConverter.ToInt16(rawBytes, offset + 6);
            int offsetToData = BitConverter.ToInt32(rawBytes, offset + 8);

            switch (condition.Type)
            {
                case 1:
                    condition.TemplateGeneric = GetGenericTemplate<QED_Types.Template0>(offsetToData);
                    condition.CONDITION = "ALWAYS";
                    break;
                case 3:
                    condition.KO_TRIGGER = GetGenericTemplate<QED_Types.TYPE3_KO_TRIGGER>(offsetToData);
                    condition.CONDITION = condition.KO_TRIGGER.DESCRIPTION;
                    break;
                case 4:
                    condition.TIME_PASSED_LOCAL = GetTemplate1<QED_Types.TYPE_4_TIME_PASSED_LOCAL>(offsetToData);
                    condition.CONDITION = "TIME_PASSED_LOCAL";
                    break;
                case 8:
                    condition.TIME_PASSED_GLOBAL = GetTemplate4<QED_Types.TYPE8_TIME_PASSED_GLOBAL>(offsetToData);
                    condition.CONDITION = "TIME_PASSED_GLOBAL";
                    break;
                case 9:
                    condition.HEALTH_TRIGGER = GetTemplate3<QED_Types.TYPE9_HEALTH_TRIGGER>(offsetToData);
                    condition.CONDITION = condition.HEALTH_TRIGGER.DESCRIPTION;
                    break;
                case 16:
                    condition.Template_4 = GetTemplate4<QED_Types.Template4>(offsetToData);
                    condition.CONDITION = "UNKNOWN_" + condition.Type.ToString();
                    break;
                case 20:
                    condition.BOOLEAN_CHECK = GetTemplate5<QED_Types.TYPE20_BOOLEAN_CHECK>(offsetToData);
                    condition.CONDITION = condition.BOOLEAN_CHECK.DESCRIPTION;
                    break;
                case 25:
                    condition.HIT_WITH_SKILL = GetGenericTemplate<QED_Types.TYPE25_HIT_WITH_SKILL>(offsetToData);
                    condition.CONDITION = condition.HIT_WITH_SKILL.DESCRIPTION;
                    break;
                case 26:
                    condition.KO_WITH_SKILL = GetGenericTemplate<QED_Types.TYPE26_KO_WITH_SKILL>(offsetToData);
                    condition.CONDITION = condition.KO_WITH_SKILL.DESCRIPTION;
                    break;
                case 27:
                    condition.ON_AUDIO_FINISH_TRIGGER = GetGenericTemplate<QED_Types.TYPE27_ON_AUDIO_FINISH_TRIGGER>(offsetToData);
                    condition.CONDITION = condition.ON_AUDIO_FINISH_TRIGGER.DESCRIPTION;
                    break;
                case 33:
                    condition.DELAYED_KO_TRIGGER = GetGenericTemplate<QED_Types.TYPE33_DELAYED_KO_TRIGGER>(offsetToData);
                    condition.CONDITION = condition.DELAYED_KO_TRIGGER.DESCRIPTION;
                    break;
                case 42:
                    condition.AFTER_EVENT = GetGenericTemplate<QED_Types.TYPE42_AFTER_EVENT>(offsetToData);
                    condition.CONDITION = condition.AFTER_EVENT.DESCRIPTION;
                    break;
                case 48:
                    condition.Template_4 = GetTemplate4<QED_Types.Template4>(offsetToData);
                    condition.CONDITION = "UNKNOWN_" + condition.Type.ToString();
                    break;
                default:
                    condition.TemplateGeneric = GetGenericTemplate< QED_Types.Template0>(offsetToData);
                    condition.CONDITION = "UNKNOWN_" + condition.Type.ToString();
                    break;
            }

            return condition;
            
            
        }


        //Generic type readers

        T GetGenericTemplate<T>(int offset) where T : IGenericTemplate, new() {
            T Generic_Temp = new T();
            Generic_Temp.I_00 = BitConverter.ToInt32(rawBytes, offset + 0);
            Generic_Temp.I_04 = BitConverter.ToInt32(rawBytes, offset + 4);
            Generic_Temp.I_08 = BitConverter.ToInt32(rawBytes, offset + 8);
            Generic_Temp.I_12 = BitConverter.ToInt32(rawBytes, offset + 12);
            Generic_Temp.I_16 = BitConverter.ToInt32(rawBytes, offset + 16);
            Generic_Temp.I_20 = BitConverter.ToInt32(rawBytes, offset + 20);
            Generic_Temp.I_24 = BitConverter.ToInt32(rawBytes, offset + 24);
            Generic_Temp.I_28 = BitConverter.ToInt32(rawBytes, offset + 28);
            return Generic_Temp;
        }

        T GetTemplate1<T>(int offset) where T : ITemplate1, new()
        {
            T Template1 = new T();
            Template1.F_00 = BitConverter.ToSingle(rawBytes, offset + 0);
            Template1.I_04 = BitConverter.ToInt32(rawBytes, offset + 4);
            Template1.I_08 = BitConverter.ToInt32(rawBytes, offset + 8);
            Template1.I_12 = BitConverter.ToInt32(rawBytes, offset + 12);
            Template1.I_16 = BitConverter.ToInt32(rawBytes, offset + 16);
            Template1.I_20 = BitConverter.ToInt32(rawBytes, offset + 20);
            Template1.I_24 = BitConverter.ToInt32(rawBytes, offset + 24);
            Template1.I_28 = BitConverter.ToInt32(rawBytes, offset + 28);
            return Template1;
        }

        T GetTemplate2<T>(int offset) where T : ITemplate2, new()
        {
            T Template2 = new T();
            Template2.Str_00 = StringEx.GetString(rawBytes, offset);
            Template2.I_20 = BitConverter.ToInt32(rawBytes, offset + 20);
            Template2.I_24 = BitConverter.ToInt32(rawBytes, offset + 24);
            Template2.I_28 = BitConverter.ToInt32(rawBytes, offset + 28);
            return Template2;
        }

        T GetTemplate3<T>(int offset) where T : ITemplate3, new()
        {
            T Template = new T();
            Template.I_00 = BitConverter.ToInt32(rawBytes, offset + 0);
            Template.I_04 = BitConverter.ToInt32(rawBytes, offset + 4);
            Template.F_08 = BitConverter.ToSingle(rawBytes, offset + 8);
            Template.I_12 = BitConverter.ToInt32(rawBytes, offset + 12);
            Template.I_16 = BitConverter.ToInt32(rawBytes, offset + 16);
            Template.I_20 = BitConverter.ToInt32(rawBytes, offset + 20);
            Template.I_24 = BitConverter.ToInt32(rawBytes, offset + 24);
            Template.I_28 = BitConverter.ToInt32(rawBytes, offset + 28);
            return Template;
        }

        T GetTemplate4<T>(int offset) where T : ITemplate4, new()
        {
            T Template = new T();
            Template.I_00 = BitConverter.ToInt32(rawBytes, offset + 0);
            Template.F_04 = BitConverter.ToSingle(rawBytes, offset + 4);
            Template.I_08 = BitConverter.ToInt32(rawBytes, offset + 8);
            Template.I_12 = BitConverter.ToInt32(rawBytes, offset + 12);
            Template.I_16 = BitConverter.ToInt32(rawBytes, offset + 16);
            Template.I_20 = BitConverter.ToInt32(rawBytes, offset + 20);
            Template.I_24 = BitConverter.ToInt32(rawBytes, offset + 24);
            Template.I_28 = BitConverter.ToInt32(rawBytes, offset + 28);
            return Template;
        }

        T GetTemplate5<T>(int offset) where T : ITemplate5, new()
        {
            T Template = new T();
            Template.I_00 = BitConverter.ToInt32(rawBytes, offset + 0);
            if (BitConverter.ToInt32(rawBytes, offset + 4) == 0)
            {
                Template.FLAG = false;
            }
            else if (BitConverter.ToInt32(rawBytes, offset + 4) == 1)
            {
                Template.FLAG = true;
            }
            else
            {
                Template.FLAG = true;
                Console.WriteLine("BOOLEAN_CHECK: value is not equal to either 0 or 1. Actual value: " + BitConverter.ToInt32(rawBytes, offset + 4));
                Console.ReadLine();
            }

            Template.I_08 = BitConverter.ToInt32(rawBytes, offset + 8);
            Template.I_12 = BitConverter.ToInt32(rawBytes, offset + 12);
            Template.I_16 = BitConverter.ToInt32(rawBytes, offset + 16);
            Template.I_20 = BitConverter.ToInt32(rawBytes, offset + 20);
            Template.I_24 = BitConverter.ToInt32(rawBytes, offset + 24);
            Template.I_28 = BitConverter.ToInt32(rawBytes, offset + 28);

            return Template;
        }

    }
}
