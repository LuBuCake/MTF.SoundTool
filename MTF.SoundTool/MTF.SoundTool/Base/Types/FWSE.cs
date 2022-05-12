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

namespace MTF.SoundTool.Base.Types
{
    public class FWSE
    {
        public int Index { get; set; }
        public string Format { get; set; }
        public int Version { get; set; }
        public int FileSize { get; set; }
        public int HeaderSize { get; set; }
        public int NumChannels { get; set; }
        public int Samples { get; set; }
        public int SampleRate { get; set; }
        public int BitsPerSample { get; set; }
        public byte[] InfoData { get; set; }
        public byte[] SoundData { get; set; }

        // Extra Data
        public TimeSpan DurationSpan { get; set; }
        public string ExpectedFileName { get; set; }
        public string DisplayFormat { get; set; }

        public FWSE(int FileIndex = 0) => Index = FileIndex;
    }
}
