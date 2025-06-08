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

using DevExpress.XtraEditors;
using MTF.SoundTool.Base.Types;
using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MTF.SoundTool.Base.Helpers
{
    public static class FWSEHelper
    {
        private static readonly int[] ADPCMTable = {
            7, 8, 9, 10, 11, 12, 13, 14,
            16, 17, 19, 21, 23, 25, 28, 31,
            34, 37, 41, 45, 50, 55, 60, 66,
            73, 80, 88, 97, 107, 118, 130, 143,
            157, 173, 190, 209, 230, 253, 279, 307,
            337, 371, 408, 449, 494, 544, 598, 658,
            724, 796, 876, 963, 1060, 1166, 1282, 1411,
            1552, 1707, 1878, 2066, 2272, 2499, 2749, 3024,
            3327, 3660, 4026, 4428, 4871, 5358, 5894, 6484,
            7132, 7845, 8630, 9493, 10442, 11487, 12635, 13899,
            15289, 16818, 18500, 20350, 22385, 24623, 27086, 29794,
            32767
        };

        private static readonly int[] CAPCOM_IndexTable = { 8, 6, 4, 2, -1, -1, -1, -1, -1, -1, -1, -1, 2, 4, 6, 8 };

        private static int IMA_MTF_ExpandNibble(int nibble, int shift, ref int sample_decoded_last, ref int step_index)
        {
            nibble = nibble >> shift & 0xF;

            int step = ADPCMTable[step_index];
            int sample = sample_decoded_last;

            int delta = step * (2 * nibble - 15);

            sample += delta;
            sample_decoded_last = sample;

            step_index += CAPCOM_IndexTable[nibble];
            step_index = Utility.Clamp(step_index, 0, 88);

            return Utility.Clamp(sample >> 4, -32768, 32767);
        }

        private static int MTF_IMA_SimplifyNible(int sample, ref int sample_predicted, ref int step_index)
        {
            int diff = (sample << 4) - sample_predicted;
            int step = ADPCMTable[step_index];

            int nibble = Utility.Clamp((int)Math.Round(diff / 2.0 / step) + 8, 0, 15);

            sample_predicted += step * (2 * nibble - 15);

            step_index += CAPCOM_IndexTable[nibble];
            step_index = Utility.Clamp(step_index, 0, 88);

            return nibble;
        }

        private static short[] DecodeFWSE(byte[] FWSEData)
        {
            int[] Result = new int[FWSEData.Length * 2];

            int sample_decoded_last = 0;
            int result_index = 0;
            int step_index = 0;

            foreach (var FWSENibble in FWSEData)
            {
                int sample = IMA_MTF_ExpandNibble(FWSENibble, 4, ref sample_decoded_last, ref step_index);
                Result[result_index] = sample; result_index++;

                sample = IMA_MTF_ExpandNibble(FWSENibble, 0, ref sample_decoded_last, ref step_index);
                Result[result_index] = sample; result_index++;
            }

            return Result.Select(i => (short)i).ToArray();
        }

        private static byte[] EncodeFWSE(short[] WAVEData)
        {
            int[] Result = new int[WAVEData.Length / 2];

            int sample_predicted = 0;
            int step_index = 0;

            int nibble_left = 0;
            int nibble_counter = 0;

            for (int i = 0; i < WAVEData.Length; i++)
            {
                int sample_encoded = MTF_IMA_SimplifyNible(WAVEData[i], ref sample_predicted, ref step_index);

                if (i % 2 == 0)
                    nibble_left = sample_encoded;
                else
                {
                    Result[nibble_counter] = (nibble_left << 4) | sample_encoded;
                    nibble_counter++;
                }
            }

            return Result.Select(i => (byte)i).ToArray();
        }

        private static byte[] DefaultInfoData { get; set; }

        public static byte[] GetDefaultInfoData(int Size = 992)
        {
            if (DefaultInfoData != null)
                return DefaultInfoData;

            DefaultInfoData = new byte[Size];

            for (int i = 0; i < Size; i++)
            {
                if (i < 8)
                    DefaultInfoData[i] = 0xFF;
                else if (i < 12)
                    DefaultInfoData[i] = 0x00;
                else if (i < 32)
                    DefaultInfoData[i] = 0xCC;
                else if (i < 36)
                    DefaultInfoData[i] = 0x00;
                else if (i < 60)
                    DefaultInfoData[i] = 0xCC;
                else if (i < 184)
                    DefaultInfoData[i] = 0x00;
                else if (i < 824)
                    DefaultInfoData[i] = 0xCC;
                else
                    DefaultInfoData[i] = 0x00;
            }

            return DefaultInfoData;
        }

        public static bool ValidadeFWSEFile(string FilePath, string FileName)
        {
            using (FileStream FS = new FileStream(FilePath, FileMode.Open))
            {
                using (BinaryReader BR = new BinaryReader(FS))
                {
                    if (FS.Length < 16)
                    {
                        XtraMessageBox.Show($"Error reading {FileName}: The file stream is too short to be a FWSE file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }

                    string Format = "";
                    for (int i = 0; i < 4; i++)
                        Format += (char)BR.ReadByte();

                    if (Format != "FWSE")
                    {
                        XtraMessageBox.Show($"Error reading {FileName}: Incorrect file format, the header must start with the extension string.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }

                    int Version = BR.ReadInt32();
                    if (Version != (int)FWSEVersion.MTF2)
                    {
                        XtraMessageBox.Show($"Error reading {FileName}: Unsupported file version, only the MT Framework 2.0 version is currently supported.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }

                    int FileSize = BR.ReadInt32();
                    if (FileSize < FS.Length)
                    {
                        XtraMessageBox.Show($"Error reading {FileName}: It's total length doesn't match what is registered inside of it.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }

                    int HeaderSize = BR.ReadInt32();
                    if (HeaderSize != 1024)
                    {
                        XtraMessageBox.Show($"Error reading {FileName}: Unsupported file header, please refer to a valid RE5 FWSE file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }

                    return true;
                }
            }
        }

        public static FWSE ReadFWSE(string FilePath, string FileName, int Index = 0)
        {
            if (!ValidadeFWSEFile(FilePath, FileName))
                return null;

            try
            {
                using (FileStream FS = new FileStream(FilePath, FileMode.Open))
                {
                    using (BinaryReader BR = new BinaryReader(FS))
                    {
                        FWSE FWSEFile = new FWSE(Index);

                        for (int i = 0; i < 4; i++)
                            FWSEFile.Format += (char)BR.ReadByte();

                        FWSEFile.Version = BR.ReadInt32();
                        FWSEFile.FileSize = BR.ReadInt32();
                        FWSEFile.HeaderSize = BR.ReadInt32();
                        FWSEFile.NumChannels = BR.ReadInt32();
                        FWSEFile.Samples = BR.ReadInt32();
                        FWSEFile.SampleRate = BR.ReadInt32();
                        FWSEFile.BitsPerSample = BR.ReadInt32();
                        FWSEFile.InfoData = BR.ReadBytes(FWSEFile.HeaderSize - 32);
                        FWSEFile.SoundData = BR.ReadBytes((int)FS.Length - FWSEFile.HeaderSize);

                        FWSEFile.DurationSpan = TimeSpan.FromSeconds((double)FWSEFile.Samples / FWSEFile.SampleRate);
                        FWSEFile.ExpectedFileName = $"{FWSEFile.Index}.fwse";
                        FWSEFile.DisplayFormat = "FWSE";

                        return FWSEFile;
                    }
                }
            }
            catch (Exception)
            {
                XtraMessageBox.Show($"Error reading {FileName}: File seems to be corrupted, please refer to a valid FWSE file when using this tool.", "Ops!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        public static void WriteFWSE(string FilePath, FWSE FWSEFile, bool MessageBox = true)
        {
            using (FileStream FS = new FileStream(FilePath, FileMode.Create))
            {
                using (BinaryWriter BW = new BinaryWriter(FS))
                {
                    BW.Write(FWSEFile.Format.ToCharArray());
                    BW.Write(FWSEFile.Version);
                    BW.Write(FWSEFile.FileSize);
                    BW.Write(FWSEFile.HeaderSize);
                    BW.Write(FWSEFile.NumChannels);
                    BW.Write(FWSEFile.Samples);
                    BW.Write(FWSEFile.SampleRate);
                    BW.Write(FWSEFile.BitsPerSample);
                    BW.Write(FWSEFile.InfoData);
                    BW.Write(FWSEFile.SoundData);

                    if (MessageBox)
                        XtraMessageBox.Show("FWSE file written sucessfully!", "Done!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        public static WAVE ConvertToWAVE(FWSE FWSEFile)
        {
            WAVE WAVEFile = new WAVE();

            WAVEFile.ChunkID = "RIFF";
            WAVEFile.Format = "WAVE";

            WAVEFile.Subchunk1ID = "fmt ";
            WAVEFile.Subchunk1Size = 16;
            WAVEFile.AudioFormat = 1;
            WAVEFile.NumChannels = 1;
            WAVEFile.SampleRate = 48000;
            WAVEFile.BitsPerSample = 16;

            WAVEFile.ByteRate = WAVEFile.SampleRate * WAVEFile.NumChannels * WAVEFile.BitsPerSample / 8;
            WAVEFile.BlockAlign = (ushort)(WAVEFile.NumChannels * WAVEFile.BitsPerSample / 8);

            WAVEFile.Subchunk2ID = "data";
            WAVEFile.Subchunk2Size = (uint)(FWSEFile.SoundData.Length * 2) * WAVEFile.NumChannels * WAVEFile.BitsPerSample / 8;
            WAVEFile.Subchunk2Data = DecodeFWSE(FWSEFile.SoundData);

            WAVEFile.ChunkSize = 4 + 8 + WAVEFile.Subchunk1Size + 8 + WAVEFile.Subchunk2Size;

            return WAVEFile;
        }

        public static FWSE ConvertToFWSE(WAVE WAVEFile, int Index = 0)
        {
            byte[] FWSEData = EncodeFWSE(WAVEFile.Subchunk2Data);

            FWSE FWSEFile = new FWSE(Index);

            FWSEFile.Format = "FWSE";
            FWSEFile.Version = (int)FWSEVersion.MTF2;
            FWSEFile.FileSize = 1024 + FWSEData.Length;
            FWSEFile.HeaderSize = 1024;
            FWSEFile.NumChannels = 1;
            FWSEFile.Samples = FWSEData.Length * 2;
            FWSEFile.SampleRate = (int)WAVEFile.SampleRate;
            FWSEFile.BitsPerSample = 16;
            FWSEFile.InfoData = GetDefaultInfoData();
            FWSEFile.SoundData = FWSEData;

            FWSEFile.DurationSpan = TimeSpan.FromSeconds((double)FWSEFile.Samples / FWSEFile.SampleRate);
            FWSEFile.ExpectedFileName = $"{FWSEFile.Index}.fwse";
            FWSEFile.DisplayFormat = "FWSE";

            return FWSEFile;
        }
    }
}
