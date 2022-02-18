using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YAXLib;
using System.IO;

namespace Xv2CoreLib.MSG
{
    public class Deserializer
    {
        MSG_File msg_File;
        string saveLocation;
        public List<byte> bytes = new List<byte>() { 35, 77, 83, 71, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        bool unicode_names = false;
        bool unicode_msg = false;

        public Deserializer(string location)
        {
            saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(location), Path.GetFileNameWithoutExtension(location));
            YAXSerializer serializer = new YAXSerializer(typeof(MSG_File), YAXSerializationOptions.DontSerializeNullObjects);
            msg_File = (MSG_File)serializer.DeserializeFromFile(location);
            UnicodeCheck();
            WriteData();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public Deserializer(MSG_File _msgFile, string location)
        {
            saveLocation = location;
            msg_File = _msgFile;
            UnicodeCheck();
            WriteData();
            File.WriteAllBytes(saveLocation, bytes.ToArray());
        }

        public Deserializer(MSG_File _msgFile)
        {
            msg_File = _msgFile;
            UnicodeCheck();
            WriteData();
        }

        void UnicodeCheck()
        {
            if (msg_File.unicode_names == true) 
            {
                unicode_names = true;
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes((short)256), 4);
            }
            if (msg_File.unicode_msg == true)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes((short)1), 6);
                unicode_msg = true;
            }
        }

        void WriteData()
        {
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(msg_File.MSG_Entries.Count()), 8);
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(msg_File.StringCount()), 24);

            //offsets
            List<int> offsetToNameString = new List<int>(); 
            List<int> offsetToStringSection = new List<int>();
            List<int> offsetToMsgString = new List<int>();

            //Name Section
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), 12);

            for (int i = 0; i < msg_File.MSG_Entries.Count(); i++)
            {
                offsetToNameString.Add(bytes.Count);
                bytes.AddRange(new byte[4]);
                bytes.AddRange(BitConverter.GetBytes(msg_File.MSG_Entries[i].Name.Length));
                if (unicode_names == true)
                {
                    bytes.AddRange(BitConverter.GetBytes(msg_File.MSG_Entries[i].Name.Length * 2 + 2));
                }
                else
                {
                    bytes.AddRange(BitConverter.GetBytes(msg_File.MSG_Entries[i].Name.Length + 1));
                }
                bytes.AddRange(BitConverter.GetBytes(msg_File.MSG_Entries[i].I_12));
                
            }

            //ID Section
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), 16);

            for (int i = 0; i < msg_File.MSG_Entries.Count(); i++)
            {
                bytes.AddRange(BitConverter.GetBytes(int.Parse(msg_File.MSG_Entries[i].Index)));
            }

            //Lines Section
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), 20);

            for (int i = 0; i < msg_File.MSG_Entries.Count(); i++)
            {
                bytes.AddRange(BitConverter.GetBytes(msg_File.MSG_Entries[i].Msg_Content.Count));
                offsetToStringSection.Add(bytes.Count());
                bytes.AddRange(new byte[4]);
            }

            //String Section
            bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), 28);

            for (int i = 0; i < msg_File.MSG_Entries.Count(); i++)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), offsetToStringSection[i]);

                for(int a = 0; a < msg_File.MSG_Entries[i].Msg_Content.Count; a++)
                {
                    offsetToMsgString.Add(bytes.Count());
                    bytes.AddRange(new byte[4]);

                    if (msg_File.MSG_Entries[i].Msg_Content[a].Text == null) msg_File.MSG_Entries[i].Msg_Content[a].Text = string.Empty;

                    bytes.AddRange(BitConverter.GetBytes(msg_File.MSG_Entries[i].Msg_Content[a].Text.Length));

                    if (unicode_msg == true)
                    {
                        bytes.AddRange(BitConverter.GetBytes(msg_File.MSG_Entries[i].Msg_Content[a].Text.Length * 2 + 2));
                    }
                    else
                    {
                        bytes.AddRange(BitConverter.GetBytes(msg_File.MSG_Entries[i].Msg_Content[a].Text.Length + 1));
                    }
                    bytes.AddRange(BitConverter.GetBytes(msg_File.MSG_Entries[i].Msg_Content[a].I_12));
                }
            }

            //Name and Msg strings

            for (int i = 0; i < msg_File.MSG_Entries.Count(); i++)
            {
                bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), offsetToNameString[i]);

                if (unicode_names == true)
                {
                    bytes.AddRange(Encoding.Unicode.GetBytes(msg_File.MSG_Entries[i].Name));
                    bytes.AddRange(new byte[2]);
                }
                else
                {
                    bytes.AddRange(Encoding.ASCII.GetBytes(msg_File.MSG_Entries[i].Name));
                    bytes.Add(0);
                }
            }

            int currentString = 0;
            for (int i = 0; i < msg_File.MSG_Entries.Count(); i++)
            {

                for(int a = 0; a < msg_File.MSG_Entries[i].Msg_Content.Count; a++)
                {
                    bytes = Utils.ReplaceRange(bytes, BitConverter.GetBytes(bytes.Count()), offsetToMsgString[currentString]);

                    if (unicode_msg == true)
                    {
                        bytes.AddRange(Encoding.Unicode.GetBytes(msg_File.MSG_Entries[i].Msg_Content[a].Text));
                        bytes.AddRange(new byte[2]);
                    }
                    else
                    {
                        bytes.AddRange(Encoding.ASCII.GetBytes(msg_File.MSG_Entries[i].Msg_Content[a].Text));
                        bytes.Add(0);
                    }

                    currentString++;
                }
                
            }
            
        }
    }
}
