using NAudio.Wave;
using System;

namespace AudioCueEditor.Audio
{
    /// <summary>
    /// Stream for looping playback
    /// </summary>
    public class LoopStream : WaveStream
    {
        WaveStream sourceStream;

        /// <summary>
        /// Creates a new Loop stream
        /// </summary>
        /// <param name="sourceStream">The stream to read from. Note: the Read method of this stream should return 0 when it reaches the end
        /// or else we will not loop to the start again.</param>
        public LoopStream(WaveStream sourceStream)
        {
            this.sourceStream = sourceStream;
        }

        /// <summary>
        /// Use this to turn looping on or off
        /// </summary>
        public bool EnableLooping { get; set; }
        public bool ForceLoop { get; set; }
        public TimeSpan LoopStart { get; set; }
        public TimeSpan LoopEnd { get; set; }

        /// <summary>
        /// Return source stream's wave format
        /// </summary>
        public override WaveFormat WaveFormat
        {
            get { return sourceStream.WaveFormat; }
        }

        /// <summary>
        /// LoopStream simply returns
        /// </summary>
        public override long Length
        {
            get { return sourceStream.Length; }
        }

        /// <summary>
        /// LoopStream simply passes on positioning to source stream
        /// </summary>
        public override long Position
        {
            get { return sourceStream.Position; }
            set { sourceStream.Position = value; }
        }

        public override TimeSpan CurrentTime { get => base.CurrentTime; set => base.CurrentTime = value; }

        public override TimeSpan TotalTime => base.TotalTime;

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (CurrentTime >= LoopEnd && EnableLooping)
                CurrentTime = LoopStart;

            if (CurrentTime < LoopStart && ForceLoop)
                CurrentTime = LoopStart;

            if (CurrentTime >= TotalTime && EnableLooping)
                CurrentTime = LoopStart; //Bandaid fix. There is some inconsistency that causes the stream to end before LoopEnd is reached when it is very close to the end... todo: find and fix the actual problem

            return sourceStream.Read(buffer, offset, count);
        }

        public void SetLoop()
        {
            LoopStart = new TimeSpan();
            LoopEnd = TotalTime;
            EnableLooping = true;
        }

        public void SetLoop(float startMs, float endMs)
        {
            LoopStart = new TimeSpan(0, 0, 0, 0, (int)startMs);
            LoopEnd = new TimeSpan(0, 0, 0, 0, (int)endMs);
            EnableLooping = true;
        }
    }
}
