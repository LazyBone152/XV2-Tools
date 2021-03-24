using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xv2CoreLib.Resource;
using Xv2CoreLib.UTF;
using YAXLib;

namespace Xv2CoreLib.ACB_NEW
{
    public class AcbFormatHelper
    {
        //Singleton
        private static Lazy<AcbFormatHelper> instance = new Lazy<AcbFormatHelper>(() => new AcbFormatHelper());
        public static AcbFormatHelper Instance => instance.Value;

        public AcbFormatHelperMain AcbFormatHelperMain { get; private set; }


        public AcbFormatHelper()
        {
            LoadXml();
        }

        public void ParseFile(UTF_File utfFile)
        {
            AcbFormatHelperMain.ParseFile(utfFile, true);
        }

        public UTF_File CreateTable(string columnName, string tableName, Version version)
        {
            var table = AcbFormatHelperMain.Tables.FirstOrDefault(x => x.Name == columnName);

            if (table == null) throw new Exception($"AcbFormatHelper.CreateTable: Could not find any information about the table \"{columnName}\".");

            return table.CreateTable(version, tableName);
        }

        public AcbFormatHelperTable GetTableHelper(string columnName, bool allowNull = false)
        {
            var helper = AcbFormatHelperMain.Tables.FirstOrDefault(x => x.Name == columnName);
            if (helper == null && !allowNull) throw new InvalidDataException($"AcbFormatHelper.GetTableHelper: Could not find table helper for \"{columnName}\".");
            return helper;
        }

        #region LimitChecks
        public bool IsActionsEnabled(Version version)
        {
            return AcbFormatHelperMain.Header.ColumnExists("ActionTrackTable", TypeFlag.Data, version);
        }

        public bool IsSequenceTypeEnabled(Version version)
        {
            var sequence = GetTableHelper("SequenceTable");

            return sequence.ColumnExists("Type", TypeFlag.UInt8, version) && sequence.ColumnExists("TrackValues", TypeFlag.Data, version);
        }

        #endregion

        #region DebugFunctions
        //These functions are primarily for use when creating a AcbFormatHelper.xml (done with external tool)

        private void LoadXml()
        {
            //Attempt to load from file if in debug mode
            bool loadedXml = false;
#if DEBUG
            loadedXml = DebugLoadXml();
#endif
            //Load it from embedded resources. For normal use.
            if (!loadedXml)
            {
                YAXSerializer serializer = new YAXSerializer(typeof(AcbFormatHelperMain), YAXSerializationOptions.DontSerializeNullObjects);
                AcbFormatHelperMain = (AcbFormatHelperMain)serializer.Deserialize(Properties.Resources.AcbFormatHelper);
            }
        }

        public void CreateNewHelper()
        {
            AcbFormatHelperMain = new AcbFormatHelperMain();
        }

        public void ParseFiles(string[] filePaths)
        {
            foreach (var file in filePaths)
            {
#if !DEBUG
                try
                {
#endif
                if (Path.GetExtension(file).ToLower() == ".acb")
                {
                    Console.WriteLine($"Parsing \"{file}\"...");
                    byte[] bytes = File.ReadAllBytes(file);
                    UTF_File utfFile = UTF_File.LoadUtfTable(bytes, bytes.Length);
                    AcbFormatHelperMain.ParseFile(utfFile, false);
                }
#if !DEBUG
                }
                catch
                {
                    Console.WriteLine($"\"{file}\" failed, skipping...");
                }
#endif
            }
        }

        public void DebugSaveXml()
        {
            YAXSerializer serializer = new YAXSerializer(typeof(AcbFormatHelperMain));
            serializer.SerializeToFile(AcbFormatHelperMain, $"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}/AcbFormatHelper.xml");
        }

        public bool DebugLoadXml()
        {
            string path = $"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}/AcbFormatHelper.xml";

            if (File.Exists(path))
            {
                YAXSerializer serializer = new YAXSerializer(typeof(AcbFormatHelperMain), YAXSerializationOptions.DontSerializeNullObjects);
                AcbFormatHelperMain = (AcbFormatHelperMain)serializer.DeserializeFromFile(path);
                return true;
            }
            else
            {
                AcbFormatHelperMain = new AcbFormatHelperMain();
                return false;
            }
        }

        #endregion
    }

    public class AcbFormatHelperMain
    {
        public AcbFormatHelperTable Header { get; private set; } = new AcbFormatHelperTable("Header");
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Column")]
        public List<AcbFormatHelperTable> Tables { get; set; } = new List<AcbFormatHelperTable>();

        public AcbFormatHelperMain() { }

        public void ParseFile(UTF_File acbFile, bool throwExIfNewColumn)
        {
            Version version = BigEndianConverter.UIntToVersion(acbFile.GetValue<uint>("Version", TypeFlag.UInt32, 0), true);

            //Parse tables
            foreach (var column in acbFile.Columns)
            {
                var tableColumn = (column.Rows[0].UtfTable != null) ? column.Rows[0].UtfTable : column.UtfTable;

                if (tableColumn != null)
                {
                    var table = GetTable(column.Name, throwExIfNewColumn);
                    table.SetExists(version);
                    table.ParseTable(tableColumn, column.Name, version, throwExIfNewColumn);
                }
            }

            //Check if they dont exist
            foreach (var table in Tables)
            {
                var utfColumn = acbFile.GetColumn(table.Name);

                if (utfColumn != null) continue;

                //Doesn't exist
                table.SetDoesNotExist(version);
            }


            //Parse all header columns
            Header.ParseTable(acbFile, "Header", version, throwExIfNewColumn);

            //Sort columns
            Sort();
        }

        /// <summary>
        /// Get a table, and optionally create it if it doesn't exist.
        /// </summary>
        /// <param name="realName">The name of the parent column that the table resides in (NOT the TableName).</param>
        /// <param name="throwExIfNewColumn">An exception will be thrown if table does not exist.</param>
        private AcbFormatHelperTable GetTable(string realName, bool throwExIfNewColumn)
        {
            if (Tables == null) Tables = new List<AcbFormatHelperTable>();
            var table = Tables.FirstOrDefault(x => x.Name == realName);

            if (table == null && throwExIfNewColumn) throw new InvalidDataException($"Unknown table \"{realName}\" encountered in ACB file. Parse failed.");

            if (table == null)
            {
                table = new AcbFormatHelperTable(realName);
                Tables.Add(table);
            }

            return table;
        }

        private void Sort()
        {
            Header.Columns.Sort((x, y) => x.Index - y.Index);

            foreach (var table in Tables)
            {
                table.Columns.Sort((x, y) => x.Index - y.Index);
            }
        }
    }

    [YAXSerializeAs("Table")]
    public class AcbFormatHelperTable : AcbFormatVersions
    {
        [YAXAttributeForClass]
        public string Name { get; set; } //parent column, not table name (except for Header).
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Column")]
        public List<AcbFormatHelperColumn> Columns { get; set; } = new List<AcbFormatHelperColumn>();


        public AcbFormatHelperTable() { }

        public AcbFormatHelperTable(string name)
        {
            Name = name;
        }

        public void ParseTable(UTF_File table, string tableName, Version version, bool throwExIfNewColumn)
        {
            if (Name != tableName) throw new InvalidOperationException("AcbFormatHelperTable.ParseTable: table name does not match!");

            //Check if they exist
            foreach (var utfColumn in table.Columns)
            {
                if (utfColumn.Name == "MusicPackageType" && utfColumn.TypeFlag == TypeFlag.Int32) continue; //Is MusicPackage, a column added by ACE. Skip this.

                var column = GetColumn(utfColumn.Name, utfColumn.TypeFlag, throwExIfNewColumn, table.Columns.IndexOf(utfColumn));
                column.SetExists(version);
            }

            //Check if they dont exist
            foreach (var column in Columns)
            {
                var utfColumn = table.GetColumn(column.Name);

                if (utfColumn != null)
                {
                    if (utfColumn.TypeFlag == column.ValueType)
                        continue;
                }

                //Doesn't exist
                column.SetDoesNotExist(version);
            }
        }

        private AcbFormatHelperColumn GetColumn(string name, TypeFlag type, bool throwExIfNewColumn, int columnIndex)
        {
            if (Columns == null) Columns = new List<AcbFormatHelperColumn>();
            var column = Columns.FirstOrDefault(x => x.Name == name && x.ValueType == type);

            if (column == null && throwExIfNewColumn) throw new InvalidDataException($"Unknown column \"{name}\" of type \"{type}\" encountered in ACB file. Parse failed.");

            if (column == null)
            {
                column = new AcbFormatHelperColumn(name, type, columnIndex);

                Columns.Add(column);
            }

            return column;
        }

        public UTF_File CreateTable(Version version, string name)
        {
            UTF_File table = new UTF_File(name);

            foreach(var column in Columns)
            {
                if(column.DoesExist(version))
                    table.Columns.Add(new UTF_Column(column.Name, column.ValueType));
            }

            return table;
        }

        public bool ColumnExists(string columnName, TypeFlag type, Version version)
        {
            var column = Columns.FirstOrDefault(x => x.Name == columnName && x.ValueType == type);

            if (column == null) return false;
            return column.DoesExist(version);
        }
    }

    [YAXSerializeAs("Column")]
    public class AcbFormatHelperColumn : AcbFormatVersions
    {
        [YAXAttributeForClass]
        public string Name { get; private set; }
        [YAXAttributeForClass]
        public TypeFlag ValueType { get; private set; }
        [YAXAttributeForClass]
        public int Index { get; set; }

        public AcbFormatHelperColumn() { }

        public AcbFormatHelperColumn(string name, TypeFlag type, int index)
        {
            Name = name;
            ValueType = type;
            Index = index;
        }

    }

    public class AcbFormatVersions
    {

        //Where column/table absolutely exists 
        [YAXDontSerialize]
        public Version PrimaryLowestVersion { get; set; }
        [YAXDontSerialize]
        public Version PrimaryHighestVersion { get; set; }

        //Earliest lowest and highest versions where column/table does NOT exist
        [YAXDontSerialize]
        public Version SecondaryLowestVersion { get; set; }
        [YAXDontSerialize]
        public Version SecondaryHighestVersion { get; set; }

        //Serialized versions
        [YAXAttributeForClass]
        [YAXSerializeAs("PrimaryLowestVersion")]
        public string PrimaryLowestVersion_Serialized { get { return SerializeVersion(PrimaryLowestVersion); } set { PrimaryLowestVersion = (!string.IsNullOrWhiteSpace(value)) ? new Version(value) : null; } }
        [YAXAttributeForClass]
        [YAXSerializeAs("PrimaryHighestVersion")]
        public string PrimaryHighestVersion_Serialized { get { return SerializeVersion(PrimaryHighestVersion); } set { PrimaryHighestVersion = (!string.IsNullOrWhiteSpace(value)) ? new Version(value) : null; } }
        [YAXAttributeForClass]
        [YAXSerializeAs("SecondaryLowestVersion")]
        public string SecondaryLowestVersion_Serialized { get { return SerializeVersion(SecondaryLowestVersion); } set { SecondaryLowestVersion = (!string.IsNullOrWhiteSpace(value)) ? new Version(value) : null; } }
        [YAXAttributeForClass]
        [YAXSerializeAs("SecondaryHighestVersion")]
        public string SecondaryHighestVersion_Serialized { get { return SerializeVersion(SecondaryHighestVersion); } set { SecondaryHighestVersion = (!string.IsNullOrWhiteSpace(value)) ? new Version(value) : null; } }

        /// <summary>
        /// Sets this column as possible for the specified version.
        /// </summary>
        /// <param name="version"></param>
        public void SetExists(Version version)
        {
            if (!IsVersionNull(PrimaryLowestVersion))
            {
                if (version < PrimaryLowestVersion)
                {
                    PrimaryLowestVersion = version;
                }
            }
            else
            {
                PrimaryLowestVersion = version;
            }

            if (!IsVersionNull(PrimaryHighestVersion))
            {
                if (version > PrimaryHighestVersion)
                {
                    PrimaryHighestVersion = version;
                }
            }
            else
            {
                PrimaryHighestVersion = version;
            }

        }

        public void SetDoesNotExist(Version version)
        {
            if(IsVersionNull(PrimaryLowestVersion) && IsVersionNull(PrimaryHighestVersion))
            {
                SecondaryHighestVersion = version;
                SecondaryLowestVersion = version;
                return;
            }

            if (version > PrimaryHighestVersion)
            {
                if (IsVersionNull(SecondaryHighestVersion))
                    SecondaryHighestVersion = version;
                else if (SecondaryHighestVersion > version)
                    SecondaryHighestVersion = version;
            }
            else if (version < PrimaryLowestVersion)
            {
                if (IsVersionNull(SecondaryLowestVersion))
                    SecondaryLowestVersion = version;
                else if (SecondaryLowestVersion < version)
                    SecondaryLowestVersion = version;
            }
            else if (IsInPrimaryRange(version))
            {
                throw new InvalidOperationException("AcbFormatVersions.SetDoesNotExist: Column already exists for the specified version...");
            }
        }

        public bool DoesExist(Version version)
        {
            return IsInSecondaryRange(version);
        }

        //Helpers
        private bool IsVersionNull(Version version)
        {
            if (version == null) return true;
            if (version.Major == 0 && version.Minor == 0 && version.Build == 0 && version.Revision == 0) return true;
            return false;
        }

        private string SerializeVersion(Version version)
        {
            return (IsVersionNull(version)) ? "0.0.0.0" : version.ToString();
        }

        private bool IsInPrimaryRange(Version version)
        {
            return (version >= PrimaryLowestVersion && version <= PrimaryHighestVersion);
        }

        private bool IsInSecondaryRange(Version version)
        {
            if (version > PrimaryHighestVersion)
            {
                if (!IsVersionNull(SecondaryHighestVersion))
                {
                    return version < SecondaryHighestVersion;
                }
                else
                {
                    return true;
                }
            }
            else if (version < PrimaryLowestVersion)
            {
                if (!IsVersionNull(SecondaryLowestVersion))
                {
                    return version > SecondaryLowestVersion;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return IsInPrimaryRange(version);
            }
        }


    }

}
