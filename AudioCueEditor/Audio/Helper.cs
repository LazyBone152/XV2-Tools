using System;
using System.Collections.Generic;
using System.IO;
using VGAudio;
using VGAudio.Cli;
using Xv2CoreLib.ACB;

namespace AudioCueEditor.Audio
{
    public static class Helper
    {
        public static List<EncodeType> SupportedEncodeTypes { get; set; } = new List<EncodeType>()
        {
            EncodeType.HCA,
            EncodeType.ADX,
            EncodeType.DSP
            //AT9 and BCWAV cannot encode correctly
        };

        public static byte[] LoadAndConvertFile(string path, FileType convertToType, bool loop, ulong encrpytionKey = 0)
        {
            switch (Path.GetExtension(path).ToLower())
            {
                case ".wav":
                    return ConvertFile(File.ReadAllBytes(path), FileType.Wave, convertToType, loop, encrpytionKey);
                case ".mp3":
                case ".wma":
                case ".aac":
                    return ConvertFile(WAV.ConvertToWav(path), FileType.Wave, convertToType, loop, encrpytionKey);
                case ".hca":
                    return ConvertFile(File.ReadAllBytes(path), FileType.Hca, convertToType, loop, encrpytionKey);
                case ".adx":
                    if (convertToType == FileType.Adx) return File.ReadAllBytes(path);
                    return ConvertFile(File.ReadAllBytes(path), FileType.Adx, convertToType, loop, encrpytionKey);
                case ".at9":
                    return ConvertFile(File.ReadAllBytes(path), FileType.Atrac9, convertToType, loop, encrpytionKey);
                case ".dsp":
                    return ConvertFile(File.ReadAllBytes(path), FileType.Dsp, convertToType, loop, encrpytionKey);
                case ".bcwav":
                    return ConvertFile(File.ReadAllBytes(path), FileType.Bcwav, convertToType, loop, encrpytionKey);
            }

            throw new InvalidDataException($"Filetype of \"{path}\" is not supported.");
        }

        public static byte[] ConvertFile(byte[] bytes, FileType encodeType, FileType convertToType, bool loop, ulong encryptionKey = 0)
        {
            ConvertStatics.SetLoop(loop, 0, 0);

            using (var ms = new MemoryStream(bytes))
            {
                var options = new Options();
                options.KeyCode = encryptionKey;
                options.Loop = loop;

                if(options.Loop)
                    options.LoopEnd = int.MaxValue;

                byte[] track = ConvertStream.ConvertFile(options, ms, encodeType, convertToType);

                //if (convertToType == FileType.Hca && loop)
                //    track = HCA.EncodeLoop(track, loop);

                return track;
            }
        }

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
                case EncodeType.DSP:
                    return FileType.Dsp;
                case EncodeType.BCWAV:
                    return FileType.Bcwav;
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
                case FileType.Bcwav:
                    return EncodeType.BCWAV;
                case FileType.Dsp:
                    return EncodeType.DSP;
                default:
                    return EncodeType.None;
            }
        }
    }
}
