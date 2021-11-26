using System.Collections.Generic;
using System.Xml.Linq;
using LB_Mod_Installer.Installer;

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
            xmlPath = path;
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

                            //Error=Skip:
                            //We wil just delete the whole XML element for this. Could be problematic for some files with nested data however...
                            if(attr.Value == BindingManager.NullTokenStr)
                            {
                                //Delete entry and restart
                                element.Parent.Remove();
                                goto restart;
                            }
                        }
                    }
                }

                if (element.HasElements)
                {
                    ParseElements(element.Elements());
                }
            }

            return;

        restart:
            BeginParse();
        }
    }

}
