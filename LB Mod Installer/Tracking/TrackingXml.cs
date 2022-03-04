using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace LB_Mod_Installer.Installer
{
    public class TrackingXml
    {
        public TrackingXml() { }

        [YAXComment("For tracking installed file entries so that they can be updated and uninstalled.")]

        public Mod Mod { get; set; }
        
        //New
        public Mod GetCurrentMod()
        {
            string name = GeneralInfo.InstallerXmlInfo.Name;
            string author = GeneralInfo.InstallerXmlInfo.Author;
            string ver = GeneralInfo.InstallerXmlInfo.VersionString;

            //This is a new XML, so add a new mod entry for the current mod
            if (Mod == null)
            {
                Mod = new Mod(GeneralInfo.InstallerXmlInfo.Name, GeneralInfo.InstallerXmlInfo.Author, ver);
            }

            //The mod defined in this xml doesn't match the current mod. 
            if(!Mod.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase) || !Mod.Author.Equals(author, StringComparison.InvariantCultureIgnoreCase))
            {
                Mod = new Mod(name, author, ver);
            }
            

            if (Mod.Files == null) Mod.Files = new List<_File>();
            if (Mod.MsgComponents == null) Mod.MsgComponents = new List<_File>();
            if (Mod.JungleFiles == null) Mod.JungleFiles = new List<_File>();

            return Mod;
        }

        public void AddJungleFile(string path)
        {
            Mod mod = GetCurrentMod();

            if(!mod.JungleFiles.Any(e => e.filePath == path))
                mod.JungleFiles.Add(new _File(path));
        }
        
        /// <summary>
        /// Adds a MsgID for use with this file.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="ID"></param>
        public void AddMsgID(string filePath, string sectionName, int MsgID)
        {
            string idStr = MsgID.ToString();
            Section section = GetCurrentMod().GetMsgComponentEntry(filePath).GetSection(sectionName);

            if (!section.IDs.Contains(idStr)) section.IDs.Add(idStr);
        }

        /// <summary>
        /// Adds a ID for use with this file.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="ID"></param>
        public void AddID(string filePath, string sectionName, string ID)
        {
            Section section = GetCurrentMod().GetFileEntry(filePath).GetSection(sectionName);
            
            if (!section.IDs.Contains(ID)) section.IDs.Add(ID);
        }

        public void AddIDs(string filePath, string sectionName, List<string> IDs)
        {
            Section section = GetCurrentMod().GetFileEntry(filePath).GetSection(sectionName);

            foreach(var id in IDs)
            {
                if (!section.IDs.Contains(id)) section.IDs.Add(id);
            }
        }

        /// <summary>
        /// Removes a ID for use with this file.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="ID"></param>
        public void RemoveID(string filePath, string sectionName, int ID)
        {
            string intStr = ID.ToString();
            Section fileEntry = GetCurrentMod().GetFileEntry(filePath).GetSection(sectionName);

            if (fileEntry.IDs.Contains(intStr)) fileEntry.IDs.Remove(intStr);
        }

        /// <summary>
        /// Removes a MsgID for use with this file.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="ID"></param>
        public void RemoveMsgID(string filePath, string sectionName, int MsgID)
        {
            string idStr = MsgID.ToString();
            Section fileEntry = GetCurrentMod().GetMsgComponentEntry(filePath).GetSection(sectionName);

            if (fileEntry.IDs.Contains(idStr)) fileEntry.IDs.Remove(idStr);
        }

    }
    
    //New
    public class Mod
    {
        public Mod()
        {

        }

        public Mod(string name, string author, string version)
        {
            Name = name;
            Author = author;
            VersionString = version;
            newMod = true;
        }

        [YAXDontSerialize]
        public bool newMod = false;
        [YAXDontSerialize]
        public Version Version { get { if (VersionString == null) return null; return new Version(VersionString); } }
        [YAXDontSerialize]
        public int TotalInstalledFiles
        {
            get
            {
                int num = 0;

                if (Files != null)
                    num += Files.Count;
                if (MsgComponents != null)
                    num += MsgComponents.Count;

                return num;
            }
        }
        [YAXDontSerialize]
        public string VersionFormattedString
        {
            get
            {
                try
                {
                    string[] split = Version.ToString().Split('.');
                    if (split[2] == "0" && split[3] == "0")
                    {
                        return String.Format("{0}.{1}", split[0], split[1]);
                    }
                    else if (split[3] == "0")
                    {
                        return String.Format("{0}.{1}{2}", split[0], split[1], split[2]);
                    }
                    else
                    {
                        return String.Format("{0}.{1}{2}{3}", split[0], split[1], split[2], split[3]);
                    }
                }
                catch
                {
                    return (Version != null) ?  Version.ToString() : "0.0";
                }
            }
        }

        [YAXAttributeForClass]
        public string Name { get; set; }
        [YAXAttributeForClass]
        public string Author { get; set; }
        [YAXAttributeForClass]
        [YAXSerializeAs("Version")]
        public string VersionString { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.Recursive, EachElementName = "File")]
        public List<_File> Files { get; set; } = new List<_File>();
        [YAXCollection(YAXCollectionSerializationTypes.Recursive, EachElementName = "File")]
        public List<_File> MsgComponents { get; set; } = new List<_File>();
        [YAXCollection(YAXCollectionSerializationTypes.Recursive, EachElementName = "File")]
        public List<_File> JungleFiles { get; set; } = new List<_File>();


        public _File GetFileEntry(string fileName)
        {
            if (Files == null) Files = new List<_File>();
            _File newFile = null;

            foreach(var file in Files)
            {
                if(file.filePath == fileName)
                {
                    newFile = file;
                    break;
                }
            }

            if(newFile == null)
            {
                newFile = new _File(fileName);
                Files.Add(newFile);
            }

            return newFile;
        }

        public _File GetMsgComponentEntry(string fileName)
        {
            if (Files == null) Files = new List<_File>();
            _File newFile = null;

            foreach (var file in MsgComponents)
            {
                if (file.filePath == fileName)
                {
                    newFile = file;
                    break;
                }
            }

            if (newFile == null)
            {
                newFile = new _File(fileName);
                MsgComponents.Add(newFile);
            }

            return newFile;
        }
        
    }

    [YAXSerializeAs("File")]
    public class _File
    {
        public _File() { }

        public _File(string path)
        {
            filePath = path;
        }

        [YAXSerializeAs("FilePath")]
        [YAXAttributeForClass]
        public string filePath { get; set; }
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Section")]
        public List<Section> Sections { get; set; } = new List<Section>();

        public Section GetSection(string sectionName)
        {
            foreach(var section in Sections)
            {
                if (section.FileSection == sectionName) return section;
            }

            //Doesn't exist so add it.
            Section newSection = new Section(sectionName);
            Sections.Add(newSection);
            return newSection;
        }
    }

    public class Section
    {
        //Each file section represents an entry in a IInstallable collection.
        //ID is Index (string)
        //AutoIDs is also Index, but is only allowed on very specific values. So its not supported on all file types. (Since Index can be multiple ints merged together... AutoIDs cant work with that)
        //Also used for MsgComponents, which works slightly different. 

        public Section() { }

        public Section(string name)
        {
            FileSection = name;
        }

        [YAXAttributeForClass]
        public string FileSection { get; set; }
        
        [YAXAttributeForClass]
        [YAXCollection(YAXCollectionSerializationTypes.Serially, SeparateBy = ",")]
        public List<string> IDs { get; set; } = new List<string>(); //IDs of ALL entries that have been installed in this section for this mod
        
    }


}
