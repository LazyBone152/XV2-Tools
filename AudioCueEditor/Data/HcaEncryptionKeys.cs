using System;
using System.IO;
using Xv2CoreLib.Resource;
using YAXLib;

namespace AudioCueEditor.Data
{

    public class HcaEncryptionKeysManager
    {

        private const string Path = "LBTools/ACE_HcaEncryptKeys.xml";

        #region Singleton
        private static Lazy<HcaEncryptionKeysManager> instance = new Lazy<HcaEncryptionKeysManager>(() => new HcaEncryptionKeysManager());
        public static HcaEncryptionKeysManager Instance => instance.Value;

        private HcaEncryptionKeysManager() 
        {
            Load();
        }
        #endregion

        public HcaEncryptionKeys EncryptionKeys { get; set; }

        private void Load()
        {
            string filePath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/{Path}";

            if (File.Exists(filePath))
            {
                try
                {
                    YAXSerializer serializer = new YAXSerializer(typeof(HcaEncryptionKeys), YAXSerializationOptions.DontSerializeNullObjects);
                    HcaEncryptionKeys keys = (HcaEncryptionKeys)serializer.DeserializeFromFile(filePath);

                    if (keys.Keys == null)
                        keys.Keys = new AsyncObservableCollection<HcaEncryptionKey>();

                    EncryptionKeys = keys;
                }
                catch
                {
                    EncryptionKeys = new HcaEncryptionKeys();
                }
            }
            else
            {
                EncryptionKeys = new HcaEncryptionKeys();
            }
        }
    
        public void Save()
        {
            string filePath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/{Path}";

            YAXSerializer serializer = new YAXSerializer(typeof(HcaEncryptionKeys));
            serializer.SerializeToFile(EncryptionKeys, filePath);
        }

        public void AddKey(string name, ulong key)
        {
            if(EncryptionKeys != null)
                EncryptionKeys.Keys.Add(new HcaEncryptionKey(name, key));
        }

        public void AddKey(ulong key)
        {
            if (EncryptionKeys != null)
                EncryptionKeys.Keys.Add(new HcaEncryptionKey($"New Key {EncryptionKeys.Keys.Count + 1}", key));
        }

        public AsyncObservableCollection<HcaEncryptionKey> GetReadOnlyViewEncryptionKeys()
        {
            AsyncObservableCollection<HcaEncryptionKey> keys = new AsyncObservableCollection<HcaEncryptionKey>();

            keys.Add(new HcaEncryptionKey("None", 0));
            keys.AddRange(EncryptionKeys.Keys);

            return keys;
        }
    }

    public class HcaEncryptionKeys
    {
        public AsyncObservableCollection<HcaEncryptionKey> Keys { get; set; } = new AsyncObservableCollection<HcaEncryptionKey>();

    }

    public class HcaEncryptionKey
    {
        public string Name { get; set; }
        public ulong Key { get; set; }

        public HcaEncryptionKey() { }

        public HcaEncryptionKey(string name, ulong key)
        {
            Name = name;
            Key = key;
        }
    }

}
