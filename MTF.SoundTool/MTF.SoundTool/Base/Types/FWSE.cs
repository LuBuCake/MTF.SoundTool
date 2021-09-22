using System;

namespace MTF.SoundTool.Base.Types
{
    public class FWSE
    {
        public int Index { get; set; }
        public string Format { get; set; }
        public int Version { get; set; }
        public int FileSize { get; set; }
        public int HeaderSize { get; set; }
        public int Channels { get; set; }
        public int Samples { get; set; }
        public int SampleRate { get; set; }
        public int BitPerSample { get; set; }
        public byte[] InfoData { get; set; }
        public byte[] SoundData { get; set; }

        // Extra Data
        public TimeSpan DurationSpan { get; set; }
        public string ExpectedFileName { get; set; }
        public string DisplayFormat { get; set; }

        public FWSE(int FileIndex = 0) => Index = FileIndex;
    }
}
