using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using YAXLib;

namespace Xv2CoreLib.QBT
{
    public class Deserializer
    {
        private string saveLocation;
        public QBT_File qbt_File { get; private set; }
        public List<byte> bytes { get; private set; } = new List<byte>() { 35, 81, 66, 84, 254, 255, 36, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 36, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        public Deserializer(string location)
        {
            saveLocation = string.Format("{0}/{1}", Path.GetDirectoryName(location), Path.GetFileNameWithoutExtension(location));
            YAXSerializer serializer = new YAXSerializer(typeof(QBT_File), YAXSerializationOptions.DontSerializeNullObjects);
            qbt_File = (QBT_File)serializer.DeserializeFromFile(location);
            WriteBinaryFile();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public Deserializer(QBT_File _qbtFile)
        {
            qbt_File = _qbtFile;
            WriteBinaryFile();
        }

        private void WriteBinaryFile()
        {
            int type0Count = CountTypes(qbt_File.NormalDialogues);
            int type1Count = CountTypes(qbt_File.InteractiveDialogues);
            int type2Count = CountTypes(qbt_File.SpecialDialogues);


            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(CountAllTableEntries()), 12);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(CountAllDialogueEntries()), 14);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(CountTypes(qbt_File.NormalDialogues)), 16);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(CountTypes(qbt_File.InteractiveDialogues)), 18);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(CountTypes(qbt_File.SpecialDialogues)), 20);

            int[] qbtEntryOffsets = CalculateTableOffsets();

            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(qbtEntryOffsets[0]), 24);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(qbtEntryOffsets[1]), 28);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(qbtEntryOffsets[2]), 32);

            int indexOfQuoteEntries = 0;

            indexOfQuoteEntries = WriteQbtEntry(qbt_File.NormalDialogues, indexOfQuoteEntries);
            indexOfQuoteEntries = WriteQbtEntry(qbt_File.InteractiveDialogues, indexOfQuoteEntries);
            indexOfQuoteEntries = WriteQbtEntry(qbt_File.SpecialDialogues, indexOfQuoteEntries);


            //Dialogue Section
            int stringIndex = 0;


            for (int i = 0; i < type0Count; i++)
            {
                stringIndex = WriteDialogieEntries(qbt_File.NormalDialogues[i].DialogueEntries, stringIndex, qbt_File.NormalDialogues[i].ID);
            }

            for (int i = 0; i < type1Count; i++)
            {
                stringIndex = WriteDialogieEntries(qbt_File.InteractiveDialogues[i].DialogueEntries, stringIndex, qbt_File.InteractiveDialogues[i].ID);
            }

            for (int i = 0; i < type2Count; i++)
            {
                stringIndex = WriteDialogieEntries(qbt_File.SpecialDialogues[i].DialogueEntries, stringIndex, qbt_File.SpecialDialogues[i].ID);
            }


            //String Section

            for (int i = 0; i < type0Count; i++)
            {
                WriteStrings(qbt_File.NormalDialogues[i].DialogueEntries);
            }

            for (int i = 0; i < type1Count; i++)
            {
                WriteStrings(qbt_File.InteractiveDialogues[i].DialogueEntries);
            }

            for (int i = 0; i < type2Count; i++)
            {
                WriteStrings(qbt_File.SpecialDialogues[i].DialogueEntries);
            }

        }

        private int WriteQbtEntry(List<DialogueEntry> qbtEntries, int index)
        {
            if (qbtEntries != null)
            {
                for (int i = 0; i < qbtEntries.Count; i++)
                {
                    int sizeBefore = bytes.Count;

                    bytes.AddRange(BitConverter.GetBytes(qbtEntries[i].I_00));
                    bytes.AddRange(BitConverter.GetBytes(qbtEntries[i].I_02));
                    bytes.AddRange(BitConverter.GetBytes(index));
                    bytes.AddRange(BitConverter.GetBytes(qbtEntries[i].DialogueEntries.Count));
                    bytes.AddRange(BitConverter.GetBytes((int)qbtEntries[i].InteractionType));
                    bytes.AddRange(BitConverter.GetBytes(qbtEntries[i].InteractionParam));
                    bytes.AddRange(BitConverter.GetBytes((int)qbtEntries[i].SpecialEvent));
                    bytes.AddRange(BitConverter.GetBytes(qbtEntries[i].SpecialOnEventEnd));
                    bytes.AddRange(BitConverter.GetBytes(qbtEntries[i].I_28));
                    bytes.AddRange(BitConverter.GetBytes(qbtEntries[i].CharaID));
                    bytes.AddRange(BitConverter.GetBytes(qbtEntries[i].I_34));
                    bytes.AddRange(BitConverter_Ex.GetBytes(qbtEntries[i].I_36, 7));

                    index += qbtEntries[i].DialogueEntries.Count;

                    if (bytes.Count - sizeBefore != 64)
                        throw new InvalidDataException("QBT_File.Save: QBT entry size is invalid!");
                }
            }

            return index;
        }

        private int WriteDialogieEntries(List<QuoteData> quoteEntries, int index, int qbtID)
        {

            for (int a = 0; a < quoteEntries.Count(); a++)
            {
                bytes.AddRange(BitConverter.GetBytes((short)qbtID));
                bytes.AddRange(BitConverter.GetBytes(quoteEntries[a].I_02));
                bytes.AddRange(BitConverter.GetBytes(quoteEntries[a].I_04));
                bytes.AddRange(BitConverter.GetBytes(quoteEntries[a].I_06));
                bytes.AddRange(BitConverter.GetBytes(quoteEntries[a].I_08));
                bytes.AddRange(BitConverter.GetBytes(quoteEntries[a].I_10));
                bytes.AddRange(BitConverter.GetBytes(quoteEntries[a].I_12));
                bytes.AddRange(BitConverter.GetBytes(quoteEntries[a].I_14));
                bytes.AddRange(BitConverter.GetBytes(quoteEntries[a].I_16));
                bytes.AddRange(BitConverter.GetBytes((short)index));
                index++;
            }
            return index;

        }

        private void WriteStrings(List<QuoteData> quoteEntries)
        {
            for (int i = 0; i < quoteEntries.Count(); i++)
            {
                bytes.AddRange(StringEx.WriteFixedSizeString(quoteEntries[i].MSG_Name, 32));
            }
        }

        private short CountAllDialogueEntries()
        {
            short count = 0;

            if (qbt_File.NormalDialogues != null)
            {
                for (int i = 0; i < qbt_File.NormalDialogues.Count(); i++)
                {
                    count += (short)qbt_File.NormalDialogues[i].DialogueEntries.Count();
                }
            }


            if (qbt_File.InteractiveDialogues != null)
            {
                for (int i = 0; i < qbt_File.InteractiveDialogues.Count(); i++)
                {
                    count += (short)qbt_File.InteractiveDialogues[i].DialogueEntries.Count();
                }
            }

            if (qbt_File.SpecialDialogues != null)
            {
                for (int i = 0; i < qbt_File.SpecialDialogues.Count(); i++)
                {
                    count += (short)qbt_File.SpecialDialogues[i].DialogueEntries.Count();
                }
            }


            return count;

        }

        private short CountAllTableEntries()
        {
            short count = 0;

            if (qbt_File.NormalDialogues != null)
            {
                count += (short)qbt_File.NormalDialogues.Count();
            }


            if (qbt_File.InteractiveDialogues != null)
            {
                count += (short)qbt_File.InteractiveDialogues.Count();
            }

            if (qbt_File.SpecialDialogues != null)
            {
                count += (short)qbt_File.SpecialDialogues.Count();
            }


            return count;

        }

        private int[] CalculateTableOffsets()
        {
            int[] offsets = new int[3] { 36, 0, 0 };

            if (qbt_File.NormalDialogues != null)
            {
                offsets[1] = qbt_File.NormalDialogues.Count() * 64 + offsets[0];
            }
            else
            {
                offsets[1] = offsets[0];
            }

            if (qbt_File.InteractiveDialogues != null)
            {
                offsets[2] = qbt_File.InteractiveDialogues.Count() * 64 + offsets[1];
            }
            else
            {
                offsets[2] = offsets[1];
            }

            return offsets;


        }

        private short CountTypes(List<DialogueEntry> tableEntry)
        {
            if (tableEntry != null)
            {
                return (short)tableEntry.Count();
            }
            else
            {
                return 0;
            }
        }
    }
}
