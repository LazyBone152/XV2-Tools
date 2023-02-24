using System;
using System.IO;
using Xv2CoreLib.ACB;
using Xv2CoreLib.CPK;
using Xv2CoreLib.UTF;

namespace Xv2CoreLib.AFS2
{
    public enum AwbType
    {
        Internal,
        External,
        CpkUnsupported,
        None
    }

    [Serializable]
    public class AWB_Wrapper
    {
        public AFS2_File AwbFile { get; set; }
        public UTF_File AcbFile { get; set; } //Parent ACB for AwbType = Internal
        public AwbType Type { get; set; }
        public string Path { get; set; } //Path to AWB or ACB file

        public AWB_Wrapper(string path)
        {
            Path = path;

            if(System.IO.Path.GetExtension(path) == ".acb")
            {
                Type = AwbType.Internal;

                byte[] acbBytes = File.ReadAllBytes(path);
                AcbFile = UTF_File.LoadUtfTable(acbBytes, acbBytes.Length);

                //Load internal AWB file
                AwbFile = AcbFile.GetColumnAfs2File("AwbFile");

                //No internal (supported) awb found
                if(AwbFile == null)
                {
                    if(AcbFile.GetColumnCpkFile("AwbFile") != null)
                    {
                        //AWB file was old CPK format. Not supported.
                        Type = AwbType.CpkUnsupported;
                    }
                    else
                    {
                        Type = AwbType.None;
                    }
                }
            }
            else if(System.IO.Path.GetExtension(path) == ".awb")
            {
                IAwbFile iAwbFile = ACB_File.LoadStreamAwb(path);
                Type = AwbType.External;

                if (iAwbFile is AFS2_File afs2)
                {
                    AwbFile = afs2;
                }
                else if(iAwbFile is AWB_CPK cpk)
                {
                    Type = AwbType.CpkUnsupported;
                }
            }
        }

        public void Save()
        {
            if(Type == AwbType.External)
            {
                AwbFile.SaveToStream(Path, out _, out _);
            }
            else if(Type == AwbType.Internal)
            {
                if (AcbFile == null)
                    throw new ArgumentNullException("AWB_Wrapper.Save: Type is set to Internal, but no AcbFile is loaded.");

                AcbFile.SetAfs2File("AwbFile", AwbFile);
                AcbFile.Save(Path);
            }
        }
    }
}
