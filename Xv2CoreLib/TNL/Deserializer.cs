using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace Xv2CoreLib.TNL
{
    public class Deserializer
    {
        const Int64 magicNumber = -8526495041129795056;
        private string saveLocation;
        public TNL_File tnl_File { get; private set; }
        public List<byte> bytes = new List<byte>();

        public Deserializer(string location)
        {
            saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(location), Path.GetFileNameWithoutExtension(location));
            tnl_File = (TNL_File)new YAXSerializer(typeof(TNL_File)).DeserializeFromFile(location);
            WriteFile();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public Deserializer(TNL_File _tnl_File, string _saveLocation)
        {
            saveLocation = _saveLocation;
            tnl_File = _tnl_File;
            WriteFile();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public Deserializer(TNL_File _tnl_File)
        {
            tnl_File = _tnl_File;
            WriteFile();
        }

        private void WriteFile()
        {
            //Section 1 (Character)
            bytes.Add(1);

            for (int i = 0; i < tnl_File.Characters.Count(); i++)
            {
                bytes.AddRange(BitConverter.GetBytes(int.Parse(tnl_File.Characters[i].I32_1)));
                bytes.Add(tnl_File.Characters[i].I8_1);
                bytes.Add(tnl_File.Characters[i].I8_2);
                bytes.Add(tnl_File.Characters[i].I8_3);
                bytes.AddRange(BitConverter.GetBytes(tnl_File.Characters[i].Str1.Count()));
                bytes.AddRange(Encoding.ASCII.GetBytes(tnl_File.Characters[i].Str1));
                bytes.AddRange(BitConverter.GetBytes(tnl_File.Characters[i].I16_1));
                bytes.AddRange(BitConverter.GetBytes(tnl_File.Characters[i].I16_2));
                bytes.AddRange(BitConverter.GetBytes(tnl_File.Characters[i].Str2.Count()));
                bytes.AddRange(Encoding.ASCII.GetBytes(tnl_File.Characters[i].Str2));
                bytes.AddRange(BitConverter.GetBytes(int.Parse(tnl_File.Characters[i].I32_2)));
                bytes.AddRange(BitConverter.GetBytes(tnl_File.Characters[i].Str3.Count()));
                bytes.AddRange(Encoding.ASCII.GetBytes(tnl_File.Characters[i].Str3));
                bytes.AddRange(BitConverter.GetBytes(tnl_File.Characters[i].Str4.Count()));
                bytes.AddRange(Encoding.ASCII.GetBytes(tnl_File.Characters[i].Str4));
                bytes.AddRange(BitConverter.GetBytes(int.Parse(tnl_File.Characters[i].I32_3)));
            }


            //Section 2 (Master)
            bytes.AddRange(BitConverter.GetBytes(magicNumber));
            bytes.Add(2);

            for (int i = 0; i < tnl_File.Teachers.Count(); i++)
            {
                bytes.AddRange(BitConverter.GetBytes(int.Parse(tnl_File.Teachers[i].I32_1)));
                bytes.Add(tnl_File.Teachers[i].I8_1);
                bytes.Add(tnl_File.Teachers[i].I8_2);
                bytes.Add(tnl_File.Teachers[i].I8_3);
                bytes.AddRange(BitConverter.GetBytes(tnl_File.Teachers[i].Str1.Count()));
                bytes.AddRange(Encoding.ASCII.GetBytes(tnl_File.Teachers[i].Str1));
                bytes.AddRange(BitConverter.GetBytes(tnl_File.Teachers[i].I16_1));
                bytes.AddRange(BitConverter.GetBytes(tnl_File.Teachers[i].I16_2));
                bytes.AddRange(BitConverter.GetBytes(tnl_File.Teachers[i].Str2.Count()));
                bytes.AddRange(Encoding.ASCII.GetBytes(tnl_File.Teachers[i].Str2));
                bytes.AddRange(BitConverter.GetBytes(int.Parse(tnl_File.Teachers[i].I32_2)));
                bytes.AddRange(BitConverter.GetBytes(tnl_File.Teachers[i].Str3.Count()));
                bytes.AddRange(Encoding.ASCII.GetBytes(tnl_File.Teachers[i].Str3));
                bytes.AddRange(BitConverter.GetBytes(tnl_File.Teachers[i].Str4.Count()));
                bytes.AddRange(Encoding.ASCII.GetBytes(tnl_File.Teachers[i].Str4));
                bytes.AddRange(BitConverter.GetBytes(int.Parse(tnl_File.Teachers[i].I32_3)));
            }

            //Section 3 (Object)
            bytes.AddRange(BitConverter.GetBytes(magicNumber));
            bytes.Add(4);

            for (int i = 0; i < tnl_File.Objects.Count(); i++)
            {
                bytes.AddRange(BitConverter.GetBytes(int.Parse(tnl_File.Objects[i].Index)));
                bytes.AddRange(BitConverter.GetBytes(tnl_File.Objects[i].Str1.Count()));
                bytes.AddRange(Encoding.ASCII.GetBytes(tnl_File.Objects[i].Str1));
                bytes.AddRange(BitConverter.GetBytes(int.Parse(tnl_File.Objects[i].I32_2)));
                bytes.AddRange(BitConverter.GetBytes(tnl_File.Objects[i].Str2.Count()));
                bytes.AddRange(Encoding.ASCII.GetBytes(tnl_File.Objects[i].Str2));
                bytes.AddRange(BitConverter.GetBytes(tnl_File.Objects[i].Str3.Count()));
                bytes.AddRange(Encoding.ASCII.GetBytes(tnl_File.Objects[i].Str3));
                bytes.AddRange(BitConverter.GetBytes(tnl_File.Objects[i].Str4.Count()));
                bytes.AddRange(Encoding.ASCII.GetBytes(tnl_File.Objects[i].Str4));
                bytes.AddRange(BitConverter.GetBytes(int.Parse(tnl_File.Objects[i].I32_3)));
                bytes.AddRange(BitConverter.GetBytes(int.Parse(tnl_File.Objects[i].I32_4)));
                bytes.AddRange(BitConverter.GetBytes(int.Parse(tnl_File.Objects[i].I32_5)));
            }

            //Section 4 (Script)
            bytes.AddRange(BitConverter.GetBytes(magicNumber));
            bytes.Add(3);

            for (int i = 0; i < tnl_File.Actions.Count(); i++)
            {
                bytes.AddRange(BitConverter.GetBytes(int.Parse(tnl_File.Actions[i].Index)));
                bytes.AddRange(BitConverter.GetBytes(tnl_File.Actions[i].Str1.Count()));
                bytes.AddRange(Encoding.ASCII.GetBytes(tnl_File.Actions[i].Str1));
                bytes.AddRange(BitConverter.GetBytes(tnl_File.Actions[i].Str2.Count()));
                bytes.AddRange(Encoding.ASCII.GetBytes(tnl_File.Actions[i].Str2));
                bytes.AddRange(BitConverter.GetBytes(tnl_File.Actions[i].Str3.Count()));
                bytes.AddRange(Encoding.ASCII.GetBytes(tnl_File.Actions[i].Str3));

                string args = tnl_File.Actions[i].Arguments.Write();
                bytes.AddRange(BitConverter.GetBytes(args.Length));
                bytes.AddRange(Encoding.ASCII.GetBytes(args));
            }

            //Last Breaker
            bytes.AddRange(BitConverter.GetBytes(magicNumber));

        }
    }
}
