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
    public static class XSEWHelper
    {
        public const int XSEWBlockHeaderContentCount = 4;
        public const int XSEWBlockHeaderContentByteCount = 7;

        public static readonly byte[] XSEWSubchunk1ExtraParams = { 0x80, 0x00, 0x07, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x02, 0x00, 0xFF, 0x00, 0x00, 0x00, 0x00, 0xC0, 0x00, 0x40, 0x00, 0xF0, 0x00, 0x00, 0x00, 0xCC, 0x01, 0x30, 0xFF, 0x88, 0x01, 0x18, 0xFF };

        public static readonly int[] AdaptationTable = {
            230, 230, 230, 230, 307, 409, 512, 614,
            768, 614, 512, 409, 307, 230, 230, 230
        };

        public static readonly int[] AdaptCoeff1 = { 256, 512, 0, 192, 240, 460, 392 };
        public static readonly int[] AdaptCoeff2 = { 0, -256, 0, 64, 0, -208, -232 };

        private static int[] RemapSoundData(int[] SoundData, int TargetSampleQuantity)
        {
            int[] newsounddata = new int[TargetSampleQuantity];
            SoundData.CopyTo(newsounddata, 0);

            return newsounddata;
        }

        private static void ChoosePredictor(int[] SoundData, ref int sample_index, ref int predictor, ref int delta)
        {
            const int IDELTA_COUNT = 24;

            int bestpred = 0;
            int bestdelta = 0;

            for (int bpred = 0; bpred < 7; bpred++)
            {
                int deltasum = 0;

                for (int k = sample_index; k < sample_index + IDELTA_COUNT; k++)
                    deltasum += Math.Abs(SoundData[k] - (SoundData[k - 1] * AdaptCoeff1[bpred] + SoundData[k - 2] * AdaptCoeff2[bpred] >> 8));

                deltasum /= 4 * IDELTA_COUNT;

                if (bpred == 0 || deltasum < bestdelta)
                {
                    bestpred = bpred;
                    bestdelta = deltasum;
                }

                if (deltasum != 0)
                    continue;

                bestpred = bpred;
                bestdelta = 16;
                break;
            }

            if (bestdelta < 16)
                bestdelta = 16;

            predictor = bestpred;
            delta = bestdelta;
        }

        private static int[] GenerateBlockHeader(int[] SoundData, ref int predictor, ref int delta, ref int coeff1, ref int coeff2, ref int sample1, ref int sample2, ref int sample_index)
        {
            sample2 = SoundData[sample_index]; sample_index++;
            sample1 = SoundData[sample_index]; sample_index++;

            ChoosePredictor(SoundData, ref sample_index, ref predictor, ref delta);

            coeff1 = AdaptCoeff1[predictor];
            coeff2 = AdaptCoeff2[predictor];

            int[] BlockHeader = new int[4];
            BlockHeader[0] = predictor;
            BlockHeader[1] = delta;
            BlockHeader[2] = sample1;
            BlockHeader[3] = sample2;

            return BlockHeader;
        }

        private static int[] GenerateBlockData(int[] SoundData, ref int coeff1, ref int coeff2, ref int sample1, ref int sample2, ref int delta, ref int sample_index)
        {
            int[] BlockData = new int[63];

            for (int i = 0; i < 63; i++)
            {
                int nibble = IMA_MS_SimplifyNibble(SoundData[sample_index], ref sample1, ref sample2, ref coeff1, ref coeff2, ref delta) << 4; sample_index++;
                nibble |= IMA_MS_SimplifyNibble(SoundData[sample_index], ref sample1, ref sample2, ref coeff1, ref coeff2, ref delta); sample_index++;
                BlockData[i] = nibble;
            }

            return BlockData;
        }

        private static int IMA_MS_ExpandNibble(byte nibble, int nibble_shift, ref int predictor, ref int sample1, ref int sample2, ref int coeff1, ref int coeff2, ref int delta)
        {
            int sample_nibble = nibble >> nibble_shift & 0xF;
            int signed_nibble = (sample_nibble & 8) == 8 ? sample_nibble - 16 : sample_nibble;

            predictor = ((sample1 * coeff1 + sample2 * coeff2) >> 8) + signed_nibble * delta;
            predictor = Utility.Clamp(predictor, -32768, 32767);

            sample2 = sample1;
            sample1 = predictor;

            delta = (AdaptationTable[sample_nibble] * delta) >> 8;

            if (delta < 16)
                delta = 16;

            return sample1;
        }

        private static int IMA_MS_SimplifyNibble(int sample, ref int sample1, ref int sample2, ref int coeff1, ref int coeff2, ref int delta)
        {
            int predictor = (sample1 * coeff1 + sample2 * coeff2) >> 8;
            int sample_nibble = Utility.Clamp((sample - predictor) / delta, -8, 6);

            predictor += sample_nibble * delta;
            predictor = Utility.Clamp(predictor, -32768, 32767);

            if (sample_nibble < 0)
                sample_nibble += 16;

            sample2 = sample1;
            sample1 = predictor;

            delta = (AdaptationTable[sample_nibble] * delta) >> 8;

            if (delta < 16)
                delta = 16;

            return sample_nibble;
        }

        private static int[] DecodeXSEWBlock(int[] XSEWBlock)
        {
            int BlockSampleCount = XSEWBlock.Length - XSEWBlockHeaderContentCount;
            int[] result = new int[BlockSampleCount * 2 + 2];
            int result_index = 0;

            int predictor = Utility.Clamp(XSEWBlock[0], 0, 6);
            int delta = XSEWBlock[1];
            int sample1 = XSEWBlock[2];
            int sample2 = XSEWBlock[3];

            result[result_index] = sample2; result_index++;
            result[result_index] = sample1; result_index++;

            int coeff1 = AdaptCoeff1[predictor];
            int coeff2 = AdaptCoeff2[predictor];

            for (int i = XSEWBlockHeaderContentCount; i < XSEWBlock.Length; i++)
            {
                result[result_index] = IMA_MS_ExpandNibble((byte)XSEWBlock[i], 4, ref predictor, ref sample1, ref sample2, ref coeff1, ref coeff2, ref delta); result_index++;
                result[result_index] = IMA_MS_ExpandNibble((byte)XSEWBlock[i], 0, ref predictor, ref sample1, ref sample2, ref coeff1, ref coeff2, ref delta); result_index++;
            }

            return result;
        }

        private static short[] DecodeXSEW(XSEW XSEWFile)
        {
            int[] Result = new int[XSEWFile.Samples];
            int ResultIndex = 0;

            for (int i = 0; i < XSEWFile.BlockCount; i++)
            {
                int[] DecodedBlock = DecodeXSEWBlock(XSEWFile.Subchunk2Data[i]);
                DecodedBlock.CopyTo(Result, ResultIndex);
                ResultIndex += DecodedBlock.Length;
            }

            return Result.Select(x => (short)x).ToArray();
        }

        private static int[][] EncodeXSEW(WAVE WAVEFile)
        {
            int[] SoundData = WAVEFile.Subchunk2Data.Select(x => (int)x).ToArray();
            int BlockQuantity = (int)Math.Ceiling((double)SoundData.Length / (63 * 2 + 2));
            SoundData = RemapSoundData(SoundData, BlockQuantity * (63 * 2 + 2));

            int[][][] Encoded = new int[2][][];
            Encoded[0] = new int[BlockQuantity][]; // Block Header
            Encoded[1] = new int[BlockQuantity][]; // Block Data

            int predictor = 0;
            int delta = 0;
            int coeff1 = 0;
            int coeff2 = 0;
            int sample1 = 0;
            int sample2 = 0;

            int sample_index = 0;

            for (int i = 0; i < BlockQuantity; i++)
            {
                Encoded[0][i] = GenerateBlockHeader(SoundData, ref predictor, ref delta, ref coeff1, ref coeff2, ref sample1, ref sample2, ref sample_index);
                Encoded[1][i] = GenerateBlockData(SoundData, ref coeff1, ref coeff2, ref sample1, ref sample2, ref delta, ref sample_index);
            }

            int[][] Result = new int[BlockQuantity][];

            for (int i = 0; i < BlockQuantity; i++)
            {
                Result[i] = new int[63 + XSEWBlockHeaderContentCount];
                Encoded[0][i].CopyTo(Result[i], 0);
                Encoded[1][i].CopyTo(Result[i], 4);
            }

            return Result;
        }

        public static bool ValidadeXSEWFile(string FilePath, string FileName, bool MessageBox = true)
        {
            using (FileStream FS = new FileStream(FilePath, FileMode.Open))
            {
                using (BinaryReader BR = new BinaryReader(FS))
                {
                    if (FS.Length < 22)
                    {
                        if (MessageBox)
                            XtraMessageBox.Show($"Error reading {FileName}: The file stream is too short to be a XSEW file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                        return false;
                    }

                    string ChunkID = "";
                    for (int i = 0; i < 4; i++)
                        ChunkID += (char)BR.ReadByte();

                    if (ChunkID != "RIFF")
                    {
                        if (MessageBox)
                            XtraMessageBox.Show($"Error reading {FileName}: Incorrect file format, the header must start with the extension string.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                        return false;
                    }

                    uint ChunkSize = BR.ReadUInt32();
                    if (ChunkSize + 8 > FS.Length)
                    {
                        if (MessageBox)
                            XtraMessageBox.Show($"Error reading {FileName}: It's total length doesn't match what is registered inside of it.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                        return false;
                    }

                    string Format = "";
                    for (int i = 0; i < 4; i++)
                        Format += (char)BR.ReadByte();

                    string Subchunck1ID = "";
                    for (int i = 0; i < 4; i++)
                        Subchunck1ID += (char)BR.ReadByte();

                    uint Subchunk1Size = BR.ReadUInt32();
                    ushort AudioFormat = BR.ReadUInt16();

                    if (Format != "WAVE" || Subchunck1ID != "fmt " || Subchunk1Size != 50 || AudioFormat != 2)
                    {
                        if (MessageBox)
                            XtraMessageBox.Show($"Error reading {FileName}: Incorrect XSEW format, please refer to valid Microsoft ADPCM XSEW file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                        return false;
                    }

                    return true;
                }
            }
        }

        public static XSEW ReadXSEW(string FilePath, string FileName, int Index = 0)
        {
            if (!ValidadeXSEWFile(FilePath, FileName))
                return null;

            try
            {
                using (FileStream FS = new FileStream(FilePath, FileMode.Open))
                {
                    using (BinaryReader BR = new BinaryReader(FS))
                    {
                        XSEW XSEWFile = new XSEW(Index);

                        for (int i = 0; i < 4; i++)
                            XSEWFile.ChunkID += (char)BR.ReadByte();

                        XSEWFile.ChunkSize = BR.ReadUInt32();

                        for (int i = 0; i < 4; i++)
                            XSEWFile.Format += (char)BR.ReadByte();

                        for (int i = 0; i < 4; i++)
                            XSEWFile.Subchunk1ID += (char)BR.ReadByte();

                        XSEWFile.Subchunk1Size = BR.ReadUInt32();
                        XSEWFile.AudioFormat = BR.ReadUInt16();
                        XSEWFile.NumChannels = BR.ReadUInt16();
                        XSEWFile.SampleRate = BR.ReadUInt32();
                        XSEWFile.ByteRate = BR.ReadUInt32();
                        XSEWFile.BlockAlign = BR.ReadUInt16();
                        XSEWFile.BitsPerSample = BR.ReadUInt16();

                        XSEWFile.ExtraParamSize = BR.ReadUInt16();
                        XSEWFile.ExtraParams = BR.ReadBytes(XSEWFile.ExtraParamSize);

                        for (int j = 0; j < 4; j++)
                            XSEWFile.Subchunk2ID += (char)BR.ReadByte();

                        XSEWFile.Subchunk2Size = BR.ReadUInt32();

                        XSEWFile.BlockHeaderContentCount = XSEWFile.BlockAlign - XSEWBlockHeaderContentByteCount + XSEWBlockHeaderContentCount;
                        XSEWFile.BlockCount = (int)XSEWFile.Subchunk2Size / XSEWFile.BlockAlign;
                        XSEWFile.Samples = (2 * (XSEWFile.BlockAlign - XSEWBlockHeaderContentByteCount) + 2) * XSEWFile.BlockCount;

                        XSEWFile.Subchunk2Data = new int[XSEWFile.BlockCount][];

                        for (int j = 0; j < XSEWFile.BlockCount; j++)
                        {
                            XSEWFile.Subchunk2Data[j] = new int[XSEWFile.BlockHeaderContentCount];

                            for (int k = 0; k < XSEWFile.BlockHeaderContentCount; k++)
                            {
                                if (k < 1)
                                    XSEWFile.Subchunk2Data[j][k] = BR.ReadByte();
                                else if (k < 4)
                                    XSEWFile.Subchunk2Data[j][k] = BR.ReadInt16();
                                else
                                    XSEWFile.Subchunk2Data[j][k] = BR.ReadByte();
                            }
                        }

                        for (int j = 0; j < 4; j++)
                            XSEWFile.Subchunk3ID += (char)BR.ReadByte();

                        XSEWFile.Subchunk3Size = BR.ReadUInt32();
                        XSEWFile.Subchunk3Param1 = BR.ReadUInt32();
                        XSEWFile.Subchunk3Param2 = BR.ReadUInt32();
                        XSEWFile.Subchunk3Samples = BR.ReadUInt32();
                        XSEWFile.Subchunk3Param3 = BR.ReadUInt32();
                        XSEWFile.Subchunk3Param4 = BR.ReadUInt32();
                        XSEWFile.Subchunk3Param5 = BR.ReadUInt32();
                        XSEWFile.Subchunk3Param6 = BR.ReadUInt32();
                        XSEWFile.Subchunk3Param7 = BR.ReadUInt32();
                        XSEWFile.Subchunk3Param8 = BR.ReadUInt32();
                        XSEWFile.Subchunk3Param9 = BR.ReadUInt32();
                        XSEWFile.Subchunk3Param10 = BR.ReadUInt32();
                        XSEWFile.Subchunk3Param11 = BR.ReadUInt32();
                        XSEWFile.Subchunk3Param12 = BR.ReadUInt32();
                        XSEWFile.Subchunk3Param13 = BR.ReadUInt32();
                        XSEWFile.Subchunk3Param14 = BR.ReadUInt32();

                        if (XSEWFile.ChunkSize + 8 + 16 <= FS.Length) // tIME sub-chunk found
                        {
                            for (int j = 0; j < 4; j++)
                                XSEWFile.Subchunk4ID += (char)BR.ReadByte();

                            XSEWFile.Subchunk4Size = BR.ReadUInt32();
                            XSEWFile.Subchunk4Year = BR.ReadUInt16();
                            XSEWFile.Subchunk4Month = BR.ReadByte();
                            XSEWFile.Subchunk4Day = BR.ReadByte();
                            XSEWFile.Subchunk4Hour = BR.ReadByte();
                            XSEWFile.Subchunk4Minute = BR.ReadByte();
                            XSEWFile.Subchunk4Second = BR.ReadUInt16();
                        }

                        if (XSEWFile.ChunkSize + 8 + 16 + 12 <= FS.Length) // ver. sub-chunk found
                        {
                            for (int j = 0; j < 4; j++)
                                XSEWFile.Subchunk5ID += (char)BR.ReadByte();

                            XSEWFile.Subchunk5Size = BR.ReadUInt32();
                            XSEWFile.Subchunk5Version = BR.ReadUInt32();
                        }

                        XSEWFile.DurationSpan = TimeSpan.FromSeconds((double)XSEWFile.Samples / XSEWFile.SampleRate);
                        XSEWFile.ExpectedFileName = $"{XSEWFile.Index}.xsew";
                        XSEWFile.DisplayFormat = "XSEW";

                        return XSEWFile;
                    }
                }
            }
            catch (Exception)
            {
                XtraMessageBox.Show($"Error reading {FileName}: File seems to be corrupted, please refer to a valid XSEW file when using this tool.", "Ops!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        public static void WriteXSEW(string FilePath, XSEW XSEWFile, bool MessageBox = true)
        {
            using (FileStream FS = new FileStream(FilePath, FileMode.Create))
            {
                using (BinaryWriter BW = new BinaryWriter(FS))
                {
                    BW.Write(XSEWFile.ChunkID.ToCharArray());
                    BW.Write(XSEWFile.ChunkSize);
                    BW.Write(XSEWFile.Format.ToCharArray());

                    BW.Write(XSEWFile.Subchunk1ID.ToCharArray());
                    BW.Write(XSEWFile.Subchunk1Size);
                    BW.Write(XSEWFile.AudioFormat);
                    BW.Write(XSEWFile.NumChannels);
                    BW.Write(XSEWFile.SampleRate);
                    BW.Write(XSEWFile.ByteRate);
                    BW.Write(XSEWFile.BlockAlign);
                    BW.Write(XSEWFile.BitsPerSample);

                    BW.Write(XSEWFile.ExtraParamSize);
                    BW.Write(XSEWFile.ExtraParams);

                    BW.Write(XSEWFile.Subchunk2ID.ToCharArray());
                    BW.Write(XSEWFile.Subchunk2Size);

                    for (int j = 0; j < XSEWFile.BlockCount; j++)
                    {
                        for (int k = 0; k < XSEWFile.BlockHeaderContentCount; k++)
                        {
                            if (k < 1)
                                BW.Write((byte)XSEWFile.Subchunk2Data[j][k]);
                            else if (k < 4)
                                BW.Write((short)XSEWFile.Subchunk2Data[j][k]);
                            else
                                BW.Write((byte)XSEWFile.Subchunk2Data[j][k]);
                        }
                    }

                    BW.Write(XSEWFile.Subchunk3ID.ToCharArray());
                    BW.Write(XSEWFile.Subchunk3Size);
                    BW.Write(XSEWFile.Subchunk3Param1);
                    BW.Write(XSEWFile.Subchunk3Param2);
                    BW.Write(XSEWFile.Subchunk3Samples);
                    BW.Write(XSEWFile.Subchunk3Param3);
                    BW.Write(XSEWFile.Subchunk3Param4);
                    BW.Write(XSEWFile.Subchunk3Param5);
                    BW.Write(XSEWFile.Subchunk3Param6);
                    BW.Write(XSEWFile.Subchunk3Param7);
                    BW.Write(XSEWFile.Subchunk3Param8);
                    BW.Write(XSEWFile.Subchunk3Param9);
                    BW.Write(XSEWFile.Subchunk3Param10);
                    BW.Write(XSEWFile.Subchunk3Param11);
                    BW.Write(XSEWFile.Subchunk3Param12);
                    BW.Write(XSEWFile.Subchunk3Param13);
                    BW.Write(XSEWFile.Subchunk3Param14);

                    BW.Write(XSEWFile.Subchunk4ID.ToCharArray());
                    BW.Write(XSEWFile.Subchunk4Size);
                    BW.Write(XSEWFile.Subchunk4Year);
                    BW.Write(XSEWFile.Subchunk4Month);
                    BW.Write(XSEWFile.Subchunk4Day);
                    BW.Write(XSEWFile.Subchunk4Hour);
                    BW.Write(XSEWFile.Subchunk4Minute);
                    BW.Write(XSEWFile.Subchunk4Second);

                    BW.Write(XSEWFile.Subchunk5ID.ToCharArray());
                    BW.Write(XSEWFile.Subchunk5Size);
                    BW.Write(XSEWFile.Subchunk5Version);

                    if (MessageBox)
                        XtraMessageBox.Show("XSEW file written sucessfully!", "Done!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        public static WAVE ConvertToWAVE(XSEW XSEWFile)
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
            WAVEFile.Subchunk2Size = (uint)XSEWFile.Samples * 2;
            WAVEFile.Subchunk2Data = DecodeXSEW(XSEWFile);

            WAVEFile.ChunkSize = 4 + 8 + WAVEFile.Subchunk1Size + 8 + WAVEFile.Subchunk2Size;

            return WAVEFile;
        }

        public static XSEW ConvertToXSEW(WAVE WAVEFile, int Index = 0)
        {
            XSEW XSEWFile = new XSEW(Index);

            int[][] EncodedWAVEData = EncodeXSEW(WAVEFile);

            XSEWFile.ChunkID = "RIFF";
            XSEWFile.Format = "WAVE";

            XSEWFile.Subchunk1ID = "fmt ";
            XSEWFile.Subchunk1Size = 50;
            XSEWFile.AudioFormat = 2;
            XSEWFile.NumChannels = 1;
            XSEWFile.SampleRate = 48000;
            XSEWFile.BlockAlign = 70;
            XSEWFile.BitsPerSample = 4;
            XSEWFile.ExtraParamSize = 32;
            XSEWFile.ExtraParams = XSEWSubchunk1ExtraParams;

            XSEWFile.ByteRate = XSEWFile.SampleRate * XSEWFile.BlockAlign / 8 >> XSEWFile.BitsPerSample;

            XSEWFile.Subchunk2ID = "data";
            XSEWFile.Subchunk2Size = (uint)((uint)XSEWFile.BlockAlign * EncodedWAVEData.Length);
            XSEWFile.Subchunk2Data = EncodedWAVEData;

            XSEWFile.Subchunk3ID = "smpl";
            XSEWFile.Subchunk3Size = 60;
            XSEWFile.Subchunk3Param1 = 0;
            XSEWFile.Subchunk3Param2 = 0;
            XSEWFile.Subchunk3Samples = 0;
            XSEWFile.Subchunk3Param3 = 60;
            XSEWFile.Subchunk3Param4 = 0;
            XSEWFile.Subchunk3Param5 = 0;
            XSEWFile.Subchunk3Param6 = 0;
            XSEWFile.Subchunk3Param7 = 1;
            XSEWFile.Subchunk3Param8 = 0;
            XSEWFile.Subchunk3Param9 = 0;
            XSEWFile.Subchunk3Param10 = 0;
            XSEWFile.Subchunk3Param11 = 0;
            XSEWFile.Subchunk3Param12 = 0;
            XSEWFile.Subchunk3Param13 = 0;
            XSEWFile.Subchunk3Param14 = 1;

            DateTime Now = DateTime.Now;

            XSEWFile.Subchunk4ID = "tIME";
            XSEWFile.Subchunk4Size = 8;
            XSEWFile.Subchunk4Year = (ushort)Now.Year;
            XSEWFile.Subchunk4Month = (byte)Now.Month;
            XSEWFile.Subchunk4Day = (byte)Now.Day;
            XSEWFile.Subchunk4Hour = (byte)Now.Hour;
            XSEWFile.Subchunk4Minute = (byte)Now.Minute;
            XSEWFile.Subchunk4Second = (ushort)Now.Second;

            XSEWFile.Subchunk5ID = "ver.";
            XSEWFile.Subchunk5Size = 4;
            XSEWFile.Subchunk5Version = 1;

            XSEWFile.ChunkSize = 4 + 8 + XSEWFile.Subchunk1Size + 8 + XSEWFile.Subchunk2Size + 8 + XSEWFile.Subchunk3Size;

            XSEWFile.BlockHeaderContentCount = XSEWFile.BlockAlign - XSEWBlockHeaderContentByteCount + XSEWBlockHeaderContentCount;
            XSEWFile.BlockCount = (int)XSEWFile.Subchunk2Size / XSEWFile.BlockAlign;
            XSEWFile.Samples = (2 * (XSEWFile.BlockAlign - XSEWBlockHeaderContentByteCount) + 2) * XSEWFile.BlockCount;
            XSEWFile.DurationSpan = TimeSpan.FromSeconds((double)XSEWFile.Samples / XSEWFile.SampleRate);
            XSEWFile.ExpectedFileName = $"{XSEWFile.Index}.xsew";
            XSEWFile.DisplayFormat = "XSEW";

            return XSEWFile;
        }
    }
}
