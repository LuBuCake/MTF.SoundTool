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
