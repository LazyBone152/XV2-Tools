using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.QBT
{
    public class Deserializer
    {
        string saveLocation;
        QBT_File qbt_File;
        List<byte> bytes = new List<byte>() {35,81,66,84,254,255,36,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,36,0,0,0,0,0,0,0,0,0,0,0 };

        public Deserializer(string location)
        {
            saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(location), Path.GetFileNameWithoutExtension(location));
            YAXSerializer serializer = new YAXSerializer(typeof(QBT_File), YAXSerializationOptions.DontSerializeNullObjects);
            qbt_File = (QBT_File)serializer.DeserializeFromFile(location);
            Validation();
            WriteBinaryFile();
        }

        public Deserializer(QBT_File _qbtFile, string location)
        {
            saveLocation = location;
            qbt_File = _qbtFile;
            WriteBinaryFile();
        }

        void Validation() {
            int type0Count = CountTypes(qbt_File.Type0);
            int type1Count = CountTypes(qbt_File.Type1);
            int type2Count = CountTypes(qbt_File.Type2);

            for (int i = 0; i < type0Count; i++)
            {
                Assertion.AssertArraySize(qbt_File.Type0[i].I_00, 30, "Normal_Dialogue", "Table");
                if(qbt_File.Type0[i].DialogueEntries!= null)
                {
                    for (int a = 0; a < qbt_File.Type0[i].DialogueEntries.Count(); a++)
                    {
                        Assertion.AssertStringSize(qbt_File.Type0[i].DialogueEntries[a].Str_18, 32, "Normal_Dialogue", "MSG");
                    }
                }
            }
            for (int i = 0; i < type1Count; i++)
            {
                Assertion.AssertArraySize(qbt_File.Type1[i].I_00, 30, "Interactive_Dialogue", "Table");
                if (qbt_File.Type1[i].DialogueEntries != null)
                {
                    for (int a = 0; a < qbt_File.Type1[i].DialogueEntries.Count(); a++)
                    {
                        Assertion.AssertStringSize(qbt_File.Type1[i].DialogueEntries[a].Str_18, 32, "Interactive_Dialogue", "MSG");
                    }
                }
            }
            for (int i = 0; i < type2Count; i++)
            {
                Assertion.AssertArraySize(qbt_File.Type2[i].I_00, 30, "Type2_Unknown", "Table");
                if (qbt_File.Type2[i].DialogueEntries != null)
                {
                    for (int a = 0; a < qbt_File.Type2[i].DialogueEntries.Count(); a++)
                    {
                        Assertion.AssertStringSize(qbt_File.Type2[i].DialogueEntries[a].Str_18, 32, "Type2_Unknown", "MSG");
                    }
                }
            }
        }

        void WriteBinaryFile()
        {
            int type0Count = CountTypes(qbt_File.Type0);
            int type1Count = CountTypes(qbt_File.Type1);
            int type2Count = CountTypes(qbt_File.Type2);


            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(CountAllTableEntries()), 12);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(CountAllDialogueEntries()), 14);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(CountTypes(qbt_File.Type0)), 16);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(CountTypes(qbt_File.Type1)), 18);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(CountTypes(qbt_File.Type2)), 20);

            int[] tableOffsets = CalculateTableOffsets();

            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(tableOffsets[0]), 24);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(tableOffsets[1]), 28);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(tableOffsets[2]), 32);

            if (qbt_File.I_04 == false)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes((short)0), 4);
            }

            int indexOfQuoteEntries = 0;

            indexOfQuoteEntries = WriteTableEntries(qbt_File.Type0, indexOfQuoteEntries);
            indexOfQuoteEntries = WriteTableEntries(qbt_File.Type1, indexOfQuoteEntries);
            indexOfQuoteEntries = WriteTableEntries(qbt_File.Type2, indexOfQuoteEntries);


            //Dialogue Section
            int stringIndex = 0;

            
            for (int i = 0; i < type0Count; i++) {
                stringIndex = WriteDialogieEntries(qbt_File.Type0[i].DialogueEntries, stringIndex, qbt_File.Type0[i].QBT_ID);
            }

            for (int i = 0; i < type1Count; i++)
            {
                stringIndex = WriteDialogieEntries(qbt_File.Type1[i].DialogueEntries, stringIndex, qbt_File.Type1[i].QBT_ID);
            }

            for (int i = 0; i < type2Count; i++)
            {
                stringIndex = WriteDialogieEntries(qbt_File.Type2[i].DialogueEntries, stringIndex, qbt_File.Type2[i].QBT_ID);
            }


            //String Section

            for (int i = 0; i < type0Count; i++)
            {
                WriteStrings(qbt_File.Type0[i].DialogueEntries);
            }

            for (int i = 0; i < type1Count; i++)
            {
                WriteStrings(qbt_File.Type1[i].DialogueEntries);
            }

            for (int i = 0; i < type2Count; i++)
            {
                WriteStrings(qbt_File.Type2[i].DialogueEntries);
            }

            File.WriteAllBytes(saveLocation, bytes.ToArray());

        }

        int WriteTableEntries(List<TableEntry> tableEntry, int index) {

            if (tableEntry != null) {
                for (int i = 0; i < tableEntry.Count(); i++)
                {
                    bytes.AddRange(BitConverter.GetBytes(tableEntry[i].I_00[0]));
                    bytes.AddRange(BitConverter.GetBytes(tableEntry[i].I_00[1]));
                    bytes.AddRange(BitConverter.GetBytes((short)index));
                    bytes.AddRange(BitConverter.GetBytes(tableEntry[i].I_00[2]));
                    bytes.AddRange(BitConverter.GetBytes((short)tableEntry[i].DialogueEntries.Count()));
                    bytes.AddRange(BitConverter.GetBytes(tableEntry[i].I_00[3]));
                    bytes.AddRange(BitConverter.GetBytes(tableEntry[i].I_00[4]));
                    bytes.AddRange(BitConverter.GetBytes(tableEntry[i].I_00[5]));
                    bytes.AddRange(BitConverter.GetBytes(tableEntry[i].I_00[6]));
                    bytes.AddRange(BitConverter.GetBytes(tableEntry[i].I_00[7]));
                    bytes.AddRange(BitConverter.GetBytes(tableEntry[i].I_00[8]));
                    bytes.AddRange(BitConverter.GetBytes(tableEntry[i].I_00[9]));
                    bytes.AddRange(BitConverter.GetBytes(tableEntry[i].I_00[10]));
                    bytes.AddRange(BitConverter.GetBytes(tableEntry[i].I_00[11]));
                    bytes.AddRange(BitConverter.GetBytes(tableEntry[i].I_00[12]));
                    bytes.AddRange(BitConverter.GetBytes(tableEntry[i].I_00[13]));
                    bytes.AddRange(BitConverter.GetBytes(tableEntry[i].I_00[14]));
                    bytes.AddRange(BitConverter.GetBytes(tableEntry[i].I_00[15]));
                    bytes.AddRange(BitConverter.GetBytes(tableEntry[i].I_00[16]));
                    bytes.AddRange(BitConverter.GetBytes(tableEntry[i].I_00[17]));
                    bytes.AddRange(BitConverter.GetBytes(tableEntry[i].I_00[18]));
                    bytes.AddRange(BitConverter.GetBytes(tableEntry[i].I_00[19]));
                    bytes.AddRange(BitConverter.GetBytes(tableEntry[i].I_00[20]));
                    bytes.AddRange(BitConverter.GetBytes(tableEntry[i].I_00[21]));
                    bytes.AddRange(BitConverter.GetBytes(tableEntry[i].I_00[22]));
                    bytes.AddRange(BitConverter.GetBytes(tableEntry[i].I_00[23]));
                    bytes.AddRange(BitConverter.GetBytes(tableEntry[i].I_00[24]));
                    bytes.AddRange(BitConverter.GetBytes(tableEntry[i].I_00[25]));
                    bytes.AddRange(BitConverter.GetBytes(tableEntry[i].I_00[26]));
                    bytes.AddRange(BitConverter.GetBytes(tableEntry[i].I_00[27]));
                    bytes.AddRange(BitConverter.GetBytes(tableEntry[i].I_00[28]));
                    bytes.AddRange(BitConverter.GetBytes(tableEntry[i].I_00[29]));

                    index += tableEntry[i].DialogueEntries.Count();

                }
            }
            

            return index;

        }

        int WriteDialogieEntries(List<QuoteData> quoteEntries, int index, int qbtID) {
            
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

        void WriteStrings(List<QuoteData> quoteEntries) {

            for (int i = 0; i < quoteEntries.Count(); i++) {
                bytes.AddRange(Encoding.ASCII.GetBytes(quoteEntries[i].Str_18));
                int remainingSpace = 32 - quoteEntries[i].Str_18.Count();
                for (int a = 0; a < remainingSpace; a++) {
                    bytes.Add(0);
                }
            }
        }

        short CountAllDialogueEntries() {
            short count = 0;

            if (qbt_File.Type0 != null) {
                for (int i = 0; i < qbt_File.Type0.Count(); i++)
                {
                    count += (short)qbt_File.Type0[i].DialogueEntries.Count();
                }
            }
            

            if (qbt_File.Type1 != null) {
                for (int i = 0; i < qbt_File.Type1.Count(); i++)
                {
                    count += (short)qbt_File.Type1[i].DialogueEntries.Count();
                }
            }

            if (qbt_File.Type2 != null) {
                for (int i = 0; i < qbt_File.Type2.Count(); i++)
                {
                    count += (short)qbt_File.Type2[i].DialogueEntries.Count();
                }
            }
            

            return count;

        }

        short CountAllTableEntries()
        {
            short count = 0;

            if (qbt_File.Type0 != null)
            {
                count += (short)qbt_File.Type0.Count();
            }


            if (qbt_File.Type1 != null)
            {
                count += (short)qbt_File.Type1.Count();
            }

            if (qbt_File.Type2 != null)
            {
                count += (short)qbt_File.Type2.Count();
            }


            return count;

        }

        int[] CalculateTableOffsets() {
            int[] offsets = new int[3] { 36, 0, 0 };

            if (qbt_File.Type0 != null)
            {
                offsets[1] = qbt_File.Type0.Count() * 64 + offsets[0];
            }
            else {
                offsets[1] = offsets[0];
            }

            if (qbt_File.Type1 != null)
            {
                offsets[2] = qbt_File.Type1.Count() * 64 + offsets[1];
            }
            else {
                offsets[2] = offsets[1];
            }

            return offsets;


        }

        short CountTypes(List<TableEntry> tableEntry) {
            if (tableEntry != null)
            {
                return (short)tableEntry.Count();
            }
            else {
                return 0;
            }
        }
    }
}
