using System;
using System.Collections.Generic;
using MTF.SoundTool.Base.Helpers;

namespace MTF.SoundTool.Base.Types
{
    public class STRQ
    {
        public List<STRQEntry> STRQEntries { get; set; }

        public string Format { get; set; }
        public int Version { get; set; }
        public int Entries { get; set; }
        public byte[] HeaderData { get; set; }
        public byte[] UnknownData { get; set; }
    }
}
