using System;

namespace MTF.SoundTool.Base.Types
{
    public class SCGI
    {
        public object SoundFile { get; set; }
        public string Format { get; set; }
        public string FileName { get; set; }
        public TimeSpan DurationSpan { get; set; }
        public int BitsPerSample { get; set; }
        public int NumChannels { get; set; }
        public int Samples { get; set; }
        public int SampleRate { get; set; }
    }
}
