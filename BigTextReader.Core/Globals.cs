namespace BigTextReader.Core
{
    public static class Globals
    {
        public const int ReadBufferSize = 1 << 20;
        public const int CheckpointInterval = 1000;
        public const int ProgressIntervalMs = 200;
        public const int MaxRenderedLineLength = 4000;
    }
}
