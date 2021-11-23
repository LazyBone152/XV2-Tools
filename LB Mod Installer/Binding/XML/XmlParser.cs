using System.Collections.Generic;
using System.Xml.Linq;
using LB_Mod_Installer.Installer;
using LB_Mod_Installer.Binding;

namespace LB_Mod_Installer.Binding.Xml
{
    /// <summary>
    /// Binding parser for XML files.
    /// </summary>
    public class XmlParser
    {
        XDocument xml;
        string xmlPath;

        public XmlParser(XDocument _xml, string path = "")
        {
            xml = _xml;
        }

        public void BeginParse()
        {
            if (Install.bindingManager == null) return; 

            ParseElements(xml.Elements());
        }

        private void ParseElements(IEnumerable<XElement> elements)
        {
            if (elements == null) return;

            foreach(var element in elements)
            {
                if (element.HasAttributes)
                {
                    foreach(var attr in element.Attributes())
                    {
                        if (Install.bindingManager.HasBinding(attr.Value))
                        {
                            attr.Value = Install.bindingManager.ParseString(attr.Value, xmlPath, attr.Name.LocalName);
                        }
                    }
                }

                if (element.HasElements)
                {
                    ParseElements(element.Elements());
                }
            }
        }
    }

}
