using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Xml;
using YAXLib;

namespace Xv2CoreLib.Resource
{
    public class ZipReader : IDisposable
    {
        public ZipArchive archive;

        public ZipReader(ZipArchive _archive)
        {
            archive = _archive;
        }

        public bool Exists(string path)
        {
            return (GetZipEntry(path, false) == null) ? false : true;
        }

        public BitmapImage LoadBitmapFromArchive(string path)
        {
            ZipArchiveEntry entry = archive.GetEntry(path);
            BitmapImage bitmap;

            using (Stream stream = entry.Open())
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    ms.Seek(0, SeekOrigin.Begin);
                    bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = ms;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                }
            }

            return bitmap;
        }

        public ZipArchiveEntry GetZipEntry(string path, bool throwExceptionIfNotFound = true)
        {
            var entry = archive.GetEntry(path);

            if (entry == null && throwExceptionIfNotFound) throw new FileNotFoundException(string.Format("Could not find the file \"{0}\" in the archive.", path));

            return entry;
        }

        public T DeserializeXmlFromArchive<T>(string path) where T : new()
        {
            XmlDocument xml = GetXmlDocumentFromArchive(path);

            if(xml != null)
            {
                using (var stringWriter = new System.IO.StringWriter())
                {
                    using (var xmlTextWriter = XmlWriter.Create(stringWriter))
                    {
                        xml.WriteTo(xmlTextWriter);
                        xmlTextWriter.Flush();
                        string xmlStr = stringWriter.GetStringBuilder().ToString();
                        return (T)new YAXSerializer(typeof(T), YAXSerializationOptions.DontSerializeNullObjects).Deserialize(xmlStr);
                    }
                }
            }

            return default(T);
        }

        private XmlDocument GetXmlDocumentFromArchive(string path)
        {
            if (archive == null) throw new InvalidOperationException("installinfo is not loaded.");
            XmlDocument xml = null;

            xml = GetXmlDocumentFromArchive(path, archive);
            return xml;
        }

        private XmlDocument GetXmlDocumentFromArchive(string path, ZipArchive archive)
        {
            if (archive == null) return null;
            
            ZipArchiveEntry entry = archive.GetEntry(path);

            if(entry != null)
            {
                XmlDocument xml = new XmlDocument();

                using (Stream stream = entry.Open())
                {
                    xml.Load(stream);
                }

                return xml;
            }
            else
            {
                return null;
            }

        }

        public byte[] GetFileFromArchive(string path)
        {
            if (archive == null) throw new InvalidOperationException("installinfo is not loaded.");

            byte[] bytes = null;

            bytes = GetFileFromArchive(path, archive);
            return bytes;
        }

        private byte[] GetFileFromArchive(string path, ZipArchive archive)
        {
            if (archive == null) return null;

            ZipArchiveEntry entry = archive.GetEntry(path);
            byte[] bytes;

            using (Stream stream = entry.Open())
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    bytes = ms.ToArray();
                }
            }

            return bytes;
        }
        
        /// <summary>
        /// Returns the first filename that contains the specified extension
        /// </summary>
        /// <param name="extension">The extension, including the comma (.ex)</param>
        /// <returns></returns>
        public string GetPathWithExtension(string extension)
        {
            foreach(var entry in archive.Entries)
            {
                if (entry.Name.Contains(extension)) return entry.Name;
            }

            return null;
        }

        public void Dispose()
        {
            archive.Dispose();
        }
    }

    public class ZipWriter : IDisposable
    {
        ZipArchive archive;

        public ZipWriter(ZipArchive _archive)
        {
            archive = _archive;
        }

        public void AddFile(string path, byte[] bytes, CompressionLevel compression = CompressionLevel.Optimal)
        {
            //First look for an existing entry, and if it exists delete it.
            var existingEntry = archive.GetEntry(path);
            if(existingEntry != null)
                existingEntry.Delete();

            //Create new entry
            var entry = archive.CreateEntry(path, compression);
            
            //Copy bytes to entry
            using (Stream stream = entry.Open())
            {
                using (MemoryStream ms = new MemoryStream(bytes))
                {
                    ms.CopyTo(stream);
                }
            }
        }

        public void Dispose()
        {
            if (archive == null) throw new Exception("Cannot dispose as archive is null.");

            archive.Dispose();
        }
    }
}
