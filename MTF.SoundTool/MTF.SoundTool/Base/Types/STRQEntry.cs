using System;

namespace MTF.SoundTool.Base.Types
{
    public class STRQEntry
    {
        public int Index { get; set; }
        public int FileNamePos { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public int FileSize { get; set; }
        public int Duration { get; set; }
        public int Channels { get; set; }
        public int SampleRate { get; set; }
        public int LoopStart { get; set; }
        public int LoopEnd { get; set; }

        // MT Framework 2.0
        public int UnknownData1 { get; set; }
        public int UnknownData2 { get; set; }

        // UMVC3
        public int UnknownData3 { get; set; }
    }
}
