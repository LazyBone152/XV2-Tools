using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.TSD
{
    public class Deserializer
    {

        const Int64 magicNumber = -8526495041129795056;
        private string saveLocation;
        public TSD_File tsd_File { get; private set; }
        public List<byte> bytes = new List<byte>();

        public Deserializer(string location)
        {
            saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(location), Path.GetFileNameWithoutExtension(location));
            tsd_File = (TSD_File)new YAXSerializer(typeof(TSD_File)).DeserializeFromFile(location);
            WriteFile();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public Deserializer(TSD_File _tsd_File, string _saveLocation)
        {
            saveLocation = _saveLocation;
            tsd_File = _tsd_File;
            WriteFile();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public Deserializer(TSD_File _tsd_File)
        {
            tsd_File = _tsd_File;
            WriteFile();
        }

        private void WriteFile() {

            //Section 1
            bytes.Add(1);

            for (int i = 0; i < tsd_File.Triggers.Count(); i++)
            {
                bytes.AddRange(BitConverter.GetBytes(int.Parse(tsd_File.Triggers[i].Index)));
                bytes.AddRange(BitConverter.GetBytes(tsd_File.Triggers[i].I_04));
                bytes.AddRange(BitConverter.GetBytes(tsd_File.Triggers[i].I_08));
                bytes.AddRange(BitConverter.GetBytes(tsd_File.Triggers[i].I_12));
                bytes.AddRange(BitConverter.GetBytes(int.Parse(tsd_File.Triggers[i].I_16)));
                bytes.AddRange(BitConverter.GetBytes(tsd_File.Triggers[i].I_20));
                
                bytes.AddRange(BitConverter.GetBytes(int.Parse(tsd_File.Triggers[i].I_24)));
                bytes.AddRange(BitConverter.GetBytes(tsd_File.Triggers[i].I_28));
                bytes.AddRange(BitConverter.GetBytes(tsd_File.Triggers[i].Condition.Length));
                bytes.AddRange(Encoding.ASCII.GetBytes(tsd_File.Triggers[i].Condition));
            }



            //Section 2
            bytes.AddRange(BitConverter.GetBytes(magicNumber));
            bytes.Add(2);
            for (int i = 0; i < tsd_File.Events.Count(); i++)
            {
                bytes.AddRange(BitConverter.GetBytes(int.Parse(tsd_File.Events[i].Index)));
                bytes.AddRange(BitConverter.GetBytes(tsd_File.Events[i].I_04));
                
                bytes.AddRange(BitConverter.GetBytes(tsd_File.Events[i].Str1.Length));
                bytes.AddRange(Encoding.ASCII.GetBytes(tsd_File.Events[i].Str1));
                bytes.AddRange(BitConverter.GetBytes(tsd_File.Events[i].Str2.Length));
                bytes.AddRange(Encoding.ASCII.GetBytes(tsd_File.Events[i].Str2));
                bytes.AddRange(BitConverter.GetBytes(tsd_File.Events[i].Str3.Length));
                bytes.AddRange(Encoding.ASCII.GetBytes(tsd_File.Events[i].Str3));
                bytes.AddRange(BitConverter.GetBytes(tsd_File.Events[i].Str4.Length));
                bytes.AddRange(Encoding.ASCII.GetBytes(tsd_File.Events[i].Str4));

                string args = tsd_File.Events[i].Arguments.Write();
                bytes.AddRange(BitConverter.GetBytes(args.Length));
                bytes.AddRange(Encoding.ASCII.GetBytes(args));

                bytes.AddRange(BitConverter.GetBytes(tsd_File.Events[i].TNL_IDs.Count()));
                List<int> tnlIds = ArrayConvert.ConvertToInt32List(tsd_File.Events[i].TNL_IDs);
                bytes.AddRange(BitConverter_Ex.GetBytes(tnlIds.ToArray()));
            }


            //Section 3
            bytes.AddRange(BitConverter.GetBytes(magicNumber));
            bytes.Add(3);
            for (int i = 0; i < tsd_File.Globals.Count(); i++)
            {
                bytes.AddRange(BitConverter.GetBytes(tsd_File.Globals[i].Index.Length));
                bytes.AddRange(Encoding.ASCII.GetBytes(tsd_File.Globals[i].Index));
                bytes.AddRange(BitConverter.GetBytes(tsd_File.Globals[i].Type));
                bytes.AddRange(BitConverter.GetBytes(tsd_File.Globals[i].Str.Length));
                bytes.AddRange(Encoding.ASCII.GetBytes(tsd_File.Globals[i].Str));
            }


            //Section 4
            bytes.AddRange(BitConverter.GetBytes(magicNumber));
            bytes.Add(4);
            for (int i = 0; i < tsd_File.Constants.Count(); i++)
            {
                bytes.AddRange(BitConverter.GetBytes(tsd_File.Constants[i].Index.Length));
                bytes.AddRange(Encoding.ASCII.GetBytes(tsd_File.Constants[i].Index));
                bytes.AddRange(BitConverter.GetBytes(tsd_File.Constants[i].Type));
                bytes.AddRange(BitConverter.GetBytes(tsd_File.Constants[i].Str.Length));
                bytes.AddRange(Encoding.ASCII.GetBytes(tsd_File.Constants[i].Str));
            }


            //Section 5
            bytes.AddRange(BitConverter.GetBytes(magicNumber));
            bytes.Add(5);
            for (int i = 0; i < tsd_File.Zones.Count(); i++)
            {
                bytes.AddRange(BitConverter.GetBytes(int.Parse(tsd_File.Zones[i].Index)));
                bytes.AddRange(BitConverter.GetBytes(tsd_File.Zones[i].I_04));
                bytes.AddRange(BitConverter.GetBytes(tsd_File.Zones[i].Str.Length));
                bytes.AddRange(Encoding.ASCII.GetBytes(tsd_File.Zones[i].Str));
            }

        }

    }
}
