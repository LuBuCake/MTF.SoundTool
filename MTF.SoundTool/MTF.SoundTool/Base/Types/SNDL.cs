using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTF.SoundTool.Base.Types
{
    public class SNDL
    {
        public string Format { get; set; }
        public int FileSize { get; set; }
        public int Version { get; set; }
        public int UnknownDataA { get; set; }
        public int HeaderSize { get; set; }
    }
}
