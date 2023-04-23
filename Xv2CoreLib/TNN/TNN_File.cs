using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using YAXLib;

namespace Xv2CoreLib.TNN
{

    [YAXSerializeAs("TNN")]
    public class TNN_File
    {

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Tutorial")]
        public List<TNN_Tutorial> Tutorials { get; set; } = new List<TNN_Tutorial>();

        #region LoadSave
        public static TNN_File Parse(string path, bool writeXml)
        {
            TNN_File file = Parse(File.ReadAllBytes(path));

            if (writeXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(TNN_File));
                serializer.SerializeToFile(file, path + ".xml");
            }

            return file;
        }

        public static TNN_File Parse(byte[] bytes)
        {
            TNN_File tnn = new TNN_File();

            int tutorialCount = BitConverter.ToInt32(bytes, 0);
            int offset = 4;

            for(int i = 0; i < tutorialCount; i++)
            {
                TNN_Tutorial entry = new TNN_Tutorial();

                entry.ID = BitConverter.ToInt32(bytes, offset + 0);
                entry.I_04 = BitConverter.ToInt32(bytes, offset + 4);
                int pageCount = BitConverter.ToInt32(bytes, offset + 8);

                offset += 12;

                for(int a = 0; a < pageCount; a++)
                {
                    TNN_Page subEntry = new TNN_Page();

                    subEntry.Type = (TNN_Page.PageType)BitConverter.ToInt32(bytes, offset + 4);
                    subEntry.I_08 = BitConverter.ToInt32(bytes, offset + 8);

                    entry.Pages.Add(subEntry);

                    offset += 12;
                }

                tnn.Tutorials.Add(entry);
            }

            //Now read strings
            for(int i = 0; i < tutorialCount; i++)
            {
                for(int a = 0; a < tnn.Tutorials[i].Pages.Count; a++)
                {
                    //Read all 13 strings into an array first
                    string[] strings = new string[13];

                    for(int b = 0; b < 13; b++)
                    {
                        strings[b] = StringEx.GetString(bytes, offset, false, StringEx.EncodingType.Unicode);
                        offset += strings[b].Length * 2 + 2;
                    }

                    //Assign strings
                    tnn.Tutorials[i].Name = strings[0];
                    tnn.Tutorials[i].Pages[a].MainImage = strings[1];
                    tnn.Tutorials[i].Pages[a].ButtonImage = strings[2];
                    tnn.Tutorials[i].Pages[a].MsgName = strings[9];
                }
            }


            return tnn;
        }
    
        public byte[] Write()
        {
            List<byte> bytes = new List<byte>();
            List<byte> stringBytes = new List<byte>();

            bytes.AddRange(BitConverter.GetBytes(Tutorials.Count));

            foreach(TNN_Tutorial tutorial in Tutorials)
            {
                if (tutorial.Pages == null)
                    tutorial.Pages = new List<TNN_Page>();

                bytes.AddRange(BitConverter.GetBytes(tutorial.ID));
                bytes.AddRange(BitConverter.GetBytes(tutorial.I_04));
                bytes.AddRange(BitConverter.GetBytes(tutorial.Pages.Count));

                int index = 0;

                foreach(TNN_Page page in tutorial.Pages)
                {
                    bytes.AddRange(BitConverter.GetBytes(index));
                    bytes.AddRange(BitConverter.GetBytes((int)page.Type));
                    bytes.AddRange(BitConverter.GetBytes(page.I_08));

                    index++;

                    //Write main tutorial name
                    stringBytes.AddRange(Encoding.Unicode.GetBytes(tutorial.Name));
                    stringBytes.AddRange(new byte[2]);

                    //Page images
                    stringBytes.AddRange(Encoding.Unicode.GetBytes(page.MainImage));
                    stringBytes.AddRange(new byte[2]);
                    stringBytes.AddRange(Encoding.Unicode.GetBytes(page.ButtonImage));
                    stringBytes.AddRange(new byte[2]);

                    //3 more duplicates of the page images
                    stringBytes.AddRange(Encoding.Unicode.GetBytes(page.MainImage));
                    stringBytes.AddRange(new byte[2]);
                    stringBytes.AddRange(Encoding.Unicode.GetBytes(page.ButtonImage));
                    stringBytes.AddRange(new byte[2]);
                    stringBytes.AddRange(Encoding.Unicode.GetBytes(page.MainImage));
                    stringBytes.AddRange(new byte[2]);
                    stringBytes.AddRange(Encoding.Unicode.GetBytes(page.ButtonImage));
                    stringBytes.AddRange(new byte[2]);
                    stringBytes.AddRange(Encoding.Unicode.GetBytes(page.MainImage));
                    stringBytes.AddRange(new byte[2]);
                    stringBytes.AddRange(Encoding.Unicode.GetBytes(page.ButtonImage));
                    stringBytes.AddRange(new byte[2]);

                    //Now writing the msg name and 3 more duplicates of that too
                    stringBytes.AddRange(Encoding.Unicode.GetBytes(page.MsgName));
                    stringBytes.AddRange(new byte[2]);
                    stringBytes.AddRange(Encoding.Unicode.GetBytes(page.MsgName));
                    stringBytes.AddRange(new byte[2]);
                    stringBytes.AddRange(Encoding.Unicode.GetBytes(page.MsgName));
                    stringBytes.AddRange(new byte[2]);
                    stringBytes.AddRange(Encoding.Unicode.GetBytes(page.MsgName));
                    stringBytes.AddRange(new byte[2]);
                }
            }

            bytes.AddRange(stringBytes);
            return bytes.ToArray();
        }

        /// <summary>
        /// Parse the xml at the specified path and convert it into a binary .tnn file, and save it at the same path minus the .xml.
        /// </summary>
        public static void Write(string xmlPath)
        {
            string saveLocation = String.Format("{0}/{1}", Path.GetDirectoryName(xmlPath), Path.GetFileNameWithoutExtension(xmlPath));
            YAXSerializer serializer = new YAXSerializer(typeof(TNN_File), YAXSerializationOptions.DontSerializeNullObjects);
            var tnnFile = (TNN_File)serializer.DeserializeFromFile(xmlPath);

            File.WriteAllBytes(saveLocation, tnnFile.Write());
        }
        #endregion
    }

    [YAXSerializeAs("Tutorial")]
    public class TNN_Tutorial : IInstallable
    {
        #region Installer
        [YAXDontSerialize]
        public int SortID => ID;
        [YAXDontSerialize]
        public int ID
        {
            set => Index = value.ToString();
            get => Utils.TryParseInt(Index);
        }
        #endregion

        [YAXSerializeAs("ID")]
        [YAXAttributeForClass]
        [BindingAutoId]
        public string Index { get; set; }
        [YAXAttributeForClass]
        public string Name { get; set; }
        [YAXAttributeForClass]
        public int I_04 { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Page")]
        public List<TNN_Page> Pages { get; set; } = new List<TNN_Page>();
    }

    [YAXSerializeAs("Page")]
    public class TNN_Page
    {
        public enum PageType
        {
            Single = 0,
            FirstPage = 1,
            MiddlePage = 2,
            LastPage = 3
        }

        [YAXAttributeForClass]
        public PageType Type { get; set; }
        [YAXAttributeForClass]
        public int I_08 { get; set; }

        [YAXAttributeFor("MainImage")]
        [YAXSerializeAs("value")]
        public string MainImage { get; set; }
        [YAXAttributeFor("ButtonImage")]
        [YAXSerializeAs("value")]
        public string ButtonImage { get; set; }
        [YAXAttributeFor("MsgName")]
        [YAXSerializeAs("value")]
        public string MsgName { get; set; }
    }

}
