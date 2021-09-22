using System.Collections.Generic;

namespace MTF.SoundTool.Base.Types
{
    public class SPAC
    {
        public List<FWSE> FWSEFiles { get; set; }
        public List<XSEW> XSEWFiles { get; set; }

        public string Format { get; set; }
        public int Version { get; set; }
        public int Sounds { get; set; }
        public int UnknownDataA { get; set; }
        public int UnknownDataB { get; set; }
        public int MetaAStart { get; set; }
        public int MetaBStart { get; set; }
        public int SoundDataStart { get; set; }

        public byte[] MetaA { get; set; }
        public byte[] MetaB { get; set; }
    }
}
