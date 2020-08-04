using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VGAudio.Cli;
using Xv2CoreLib.ACB_NEW;

namespace AudioCueEditor.Audio
{
    public static class Helper
    {
        //Only converts between supported formats!
        public static FileType GetFileType(EncodeType encodeType)
        {
            switch (encodeType)
            {
                case EncodeType.HCA:
                case EncodeType.HCA_ALT:
                    return FileType.Hca;
                case EncodeType.ADX:
                    return FileType.Adx;
                case EncodeType.ATRAC9:
                    return FileType.Atrac9;
                default:
                    return FileType.NotSet;
            }
        }

        public static EncodeType GetEncodeType(FileType fileType)
        {
            switch (fileType)
            {
                case FileType.Hca:
                    return EncodeType.HCA;
                case FileType.Adx:
                    return EncodeType.ADX;
                case FileType.Atrac9:
                    return EncodeType.ATRAC9;
                default:
                    return EncodeType.None;
            }
        }
    }
}
