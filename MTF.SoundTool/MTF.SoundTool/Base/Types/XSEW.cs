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
    public class XSEW // Microsoft ADPCM
    {
        public int Index { get; set; }

        public string ChunkID { get; set; }                 // 4 Bytes raw string 'RIFF'
        public uint ChunkSize { get; set; }                 // unasigned int, should equal to total filelength - 8
        public string Format { get; set; }                  // 4 Bytes raw string 'WAVE'

        // FMT sub-chunk
        public string Subchunk1ID { get; set; }             // 4 Bytes raw string 'fmt '
        public uint Subchunk1Size { get; set; }             // 4 Bytes 50 for CAPCOM's Microsoft ADPCM, This is the size of the rest of the Subchunk which follows this number.
        public ushort AudioFormat { get; set; }             // 2 Bytes 1 for PCM, other values means other type of compression
        public ushort NumChannels { get; set; }             // 2 Bytes Mono = 1, Stereo = 2, etc...
        public uint SampleRate { get; set; }                // 4 Bytes 8000, 44100, etc...
        public uint ByteRate { get; set; }                  // 4 Bytes SampleRate * BlockAlign / 8 >> BitsPerSample
        public ushort BlockAlign { get; set; }              // 2 Bytes MT Framework 2.0 = 70
        public ushort BitsPerSample { get; set; }           // 2 Bytes 8 bits = 8, 16 bits = 16, etc...
        public ushort ExtraParamSize { get; set; }          // 2 Bytes MT Framework 2.0 = 32
        public byte[] ExtraParams { get; set; }             // 32 Bytes MT Framework 2.0 = { 80 00 07 00 00 01 00 00 00 02 00 FF 00 00 00 00 C0 00 40 00 F0 00 00 00 CC 01 30 FF 88 01 18 FF }

        // DATA sub-chunk
        public string Subchunk2ID { get; set; }             // 4 Bytes raw string 'data'
        public uint Subchunk2Size { get; set; }             // 4 Bytes NumSamples * NumChannels * BitsPerSample / 8
        public int[][] Subchunk2Data { get; set; }          // Multidimensional int array containing all block data [BlockIndex][BlockDataIndex]

        // SMPL sub-chunk
        public string Subchunk3ID { get; set; }             // 4 Bytes raw string 'smpl'
        public uint Subchunk3Size { get; set; }             // 4 Bytes = 60
        public uint Subchunk3Param1 { get; set; }           // 4 Bytes = 0
        public uint Subchunk3Param2 { get; set; }           // 4 Bytes = 0
        public uint Subchunk3Samples { get; set; }          // 4 Bytes = Total sample count for something, should be 0 in a custom file
        public uint Subchunk3Param3 { get; set; }           // 4 Bytes = 60
        public uint Subchunk3Param4 { get; set; }           // 4 Bytes = 0
        public uint Subchunk3Param5 { get; set; }           // 4 Bytes = 0
        public uint Subchunk3Param6 { get; set; }           // 4 Bytes = 0
        public uint Subchunk3Param7 { get; set; }           // 4 Bytes = 1
        public uint Subchunk3Param8 { get; set; }           // 4 Bytes = 0
        public uint Subchunk3Param9 { get; set; }           // 4 Bytes = 0
        public uint Subchunk3Param10 { get; set; }          // 4 Bytes = 0
        public uint Subchunk3Param11 { get; set; }          // 4 Bytes = 0
        public uint Subchunk3Param12 { get; set; }          // 4 Bytes = 0
        public uint Subchunk3Param13 { get; set; }          // 4 Bytes = 0
        public uint Subchunk3Param14 { get; set; }          // 4 Bytes = 1

        // tIME sub-chunk
        public string Subchunk4ID { get; set; }             // 4 Bytes raw string 'tIME'
        public uint Subchunk4Size { get; set; }             // 4 Bytes = 8
        public ushort Subchunk4Year { get; set; }           // 2 Bytes year number
        public byte Subchunk4Month { get; set; }            // 1 Byte month number
        public byte Subchunk4Day { get; set; }              // 1 Byte day number
        public byte Subchunk4Hour { get; set; }             // 1 Byte hour number
        public byte Subchunk4Minute { get; set; }           // 1 Byte minute number
        public ushort Subchunk4Second { get; set; }         // 2 Bytes second number

        // ver. sub-chunk
        public string Subchunk5ID { get; set; }             // 4 Bytes raw string 'ver.'
        public uint Subchunk5Size { get; set; }             // 4 Bytes = 4
        public uint Subchunk5Version { get; set; }          // 4 Bytes = 1


        // Extra Data
        public int BlockHeaderContentCount { get; set; }
        public int BlockCount { get; set; }
        public int Samples { get; set; }
        public TimeSpan DurationSpan { get; set; }
        public string ExpectedFileName { get; set; }
        public string DisplayFormat { get; set; }

        public XSEW(int FileIndex = 0) => Index = FileIndex;
    }
}
