namespace VGAudio
{
    public static class ConvertStatics
    {
        public static bool WasLoopSet { get; private set; } = false;
        public static bool Loop { get; private set; } = false;
        public static int LoopStartMs { get; private set; } = 0;
        public static int LoopEndMs { get; private set; } = 0;

        public static void SetLoop(bool loop, int startMs, int endMs)
        {
            WasLoopSet = true;
            Loop = loop;
            LoopStartMs = startMs;
            LoopEndMs = endMs;
        }

        public static void ResetLoop()
        {
            WasLoopSet = false;
            Loop = false;
            LoopStartMs = 0;
            LoopEndMs = 0;
        }
    }
}
