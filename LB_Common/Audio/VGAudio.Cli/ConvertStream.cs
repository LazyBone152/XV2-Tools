using System;
using System.IO;
using System.Linq;
using VGAudio.Containers;
using VGAudio.Formats;

namespace VGAudio.Cli
{
    public class ConvertStream
    {
        private ConvertStream() { }
        private AudioData Audio { get; set; }
        private Configuration Configuration { get; set; }
        private ContainerType OutType { get; set; }

        public static byte[] ConvertFile(Options options, MemoryStream inputStream, FileType inputFileType, FileType outputFileType)
        {
            if (options.Job != JobType.Convert && options.Job != JobType.Batch) return null;

            var converter = new ConvertStream();

            AudioFileStream audioFile = new AudioFileStream(inputStream, inputFileType);

            converter.ReadFile(audioFile);
            converter.EncodeFiles(audioFile, options);
            converter.SetConfiguration(outputFileType, options);
            return converter.WriteFile();
        }

        private void ReadFile(AudioFileStream file)
        {
            ContainerTypes.Containers.TryGetValue(file.Type, out ContainerType type);

            if (type == null)
            {
                throw new ArgumentOutOfRangeException(nameof(file.Type), file.Type, null);
            }

            AudioWithConfig audio = type.GetReader().ReadWithConfig(file.Stream);
            file.Audio = audio.Audio;
            Configuration = audio.Configuration;
        }

        private byte[] WriteFile()
        {
            byte[] bytes;

            using(MemoryStream stream = new MemoryStream())
            {
                OutType.GetWriter().WriteToStream(Audio, stream, Configuration);
                bytes = stream.ToArray();
            }

            return bytes;
        }

        private void EncodeFiles(AudioFileStream file, Options options)
        {
            if(file.Channels != null)
            {
                IAudioFormat format = file.Audio.GetAllFormats().First();
                file.Audio = new AudioData(format.GetChannels(file.Channels.ToArray()));
            }

            Audio = file.Audio; //Nothing to combine

            if (options.NoLoop)
            {
                Audio.SetLoop(false);
            }

            if (options.Loop)
            {
                Audio.SetLoop(options.Loop, options.LoopStart, options.LoopEnd);
            }
        }

        private void SetConfiguration(FileType fileType, Options options)
        {
            if (!options.KeepConfiguration)
            {
                Configuration = null;
            }

            if (!ContainerTypes.Containers.TryGetValue(fileType, out ContainerType type))
            {
                throw new ArgumentOutOfRangeException(nameof(type), "Output type not in type dictionary");
            }
            OutType = type;

            Configuration = OutType.GetConfiguration(options, Configuration);
        }
    }
}
