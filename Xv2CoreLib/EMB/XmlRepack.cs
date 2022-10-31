using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YAXLib;

namespace Xv2CoreLib.EMB
{
    public class XmlRepack
    {
        string FolderPath { get; set; }
        EmbPack_LB.EMB.EmbIndex EmbIndexList { get; set; }

        public XmlRepack(string path)
        {
            Console.WriteLine("(Emb/Xml Repack Mode)\n");
            FolderPath = path;
            EmbIndexList = GetIndexList(FolderPath);
            DeserializeAndRepack();
        }

        private void DeserializeAndRepack()
        {
            string[] files = Directory.GetFiles(FolderPath);

            foreach(string s in files)
            {
                if(Path.GetExtension(s) == ".xml")
                {
                    switch (Path.GetExtension(Path.GetFileNameWithoutExtension(s)))
                    {
                        case ".ecf":
                            Console.WriteLine(String.Format("Converting \"{0}\" to binary file...", s));
                            new ECF_XML.Deserializer(s);
                            break;
                        case ".emp":
                            Console.WriteLine(String.Format("Converting \"{0}\" to binary file...", s));
                            new EMP.Deserializer(s);
                            break;
                    }
                }
            }

            Console.WriteLine("Repacking...");
            var embRepacker = new EmbPack_LB.EMB.Repacker(FolderPath, EmbIndexList);
        }



        private EmbPack_LB.EMB.EmbIndex GetIndexList(string path)
        {


            if(File.Exists(String.Format("{0}/EmbIndex.xml", path)))
            {
                YAXSerializer serializer = new YAXSerializer(typeof(EmbPack_LB.EMB.EmbIndex), YAXSerializationOptions.DontSerializeNullObjects);
                return (EmbPack_LB.EMB.EmbIndex)serializer.DeserializeFromFile(String.Format("{0}/EmbIndex.xml", path));

            } else if (File.Exists(String.Format("{0}/embFiles.xml", path)))
            {
                YAXSerializer serializer = new YAXSerializer(typeof(EmbPack_LB.EMB.EmbIndex_Compat), YAXSerializationOptions.DontSerializeNullObjects);
                EmbPack_LB.EMB.EmbIndex_Compat embFiles = (EmbPack_LB.EMB.EmbIndex_Compat)serializer.DeserializeFromFile(String.Format("{0}/embFiles.xml", path));

                EmbPack_LB.EMB.EmbIndex newEmbIndex = new EmbPack_LB.EMB.EmbIndex() {I_08 = 37568, I_10 = 0, Entry = new List<EmbPack_LB.EMB.EmbEntry>() };

                for(int i = 0; i < embFiles.Entry.Count(); i++)
                {
                    newEmbIndex.Entry.Add(new EmbPack_LB.EMB.EmbEntry()
                    {
                        Name = embFiles.Entry[i].FileName1,
                        Index = i
                    });
                }

                return newEmbIndex;

            }
            else
            {
                Console.WriteLine("Could not find the index xml!");
                Console.ReadLine();
                Environment.Exit(0);
                return new EmbPack_LB.EMB.EmbIndex();
            }


        }


    }
}
