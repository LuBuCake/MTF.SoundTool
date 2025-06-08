/*
    This file is part of MTF Sound Tool.
    MTF Sound Tool is free software: you can redistribute it
    and/or modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation, either version 3 of
    the License, or (at your option) any later version.
    MTF Sound Tool is distributed in the hope that it will
    be useful, but WITHOUT ANY WARRANTY; without even the implied
    warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
    See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License
    along with MTF Sound Tool. If not, see <https://www.gnu.org/licenses/>6.
*/

using System;
using System.Collections.Generic;

namespace MTF.SoundTool.Base.Types
{
    //CAPCOM 3DS Games

    public class MCA3DSChannel
    {
        public List<short> adpcmCoefs = new List<short>();
        public short hist1 = 0;
        public short hist2 = 0;
    }

    public class MCA3DS
    {
        public List<MCA3DSChannel> Channels { get; set; } = new List<MCA3DSChannel>();
        public List<short[][]> CoefOutput { get; set; } = new List<short[][]>();

        public string Format { get; set; }
        public int Version { get; set; }
        public short NumChannels { get; set; }
        public short Interleave { get; set; }
        public int Samples { get; set; }
        public int SampleRate { get; set; }
        public int LoopStart { get; set; }
        public int LoopEnd { get; set; }
        public int HeaderSize { get; set; }
        public int StreamSize { get; set; }
        public byte[] SoundData { get; set; }

        // Codec data
        public bool IsLooped { get; set; }
        public int CoefOffset { get; set; }
        public int StreamOffset { get; set; }

        // Extra Data
        public TimeSpan DurationSpan { get; set; }
        public string ExpectedFileName { get; set; }
        public string DisplayFormat { get; set; }
    }
}
