using System.IO;
using System.Xml;
using System.Xml.Linq;
using LB_Mod_Installer.Binding.Xml;

namespace Xv2CoreLib.Resource
{
    public static class ZipReaderExtensions
    {
        //ZipReader method hijack to allow the processing of bindings before actually loading the XMLs into classes
        public static T DeserializeXmlFromArchive_Ext<T>(this ZipReader zipReader, string path) where T : new()
        {
            XDocument xml = zipReader.GetXmlDocumentFromArchive(path);

            if (xml == null)
                throw new FileNotFoundException($"DeserializeXmlFromArchive_Ext: Could not find the file \"{path}\".");

            XmlParser parser = new XmlParser(xml, path);
            parser.BeginParse();

            return zipReader.DeserializeXmlFromArchive<T>(xml);
        }
    }
}
