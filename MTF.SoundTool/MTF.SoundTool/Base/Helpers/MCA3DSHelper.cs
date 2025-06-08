using DevExpress.XtraEditors;
using MTF.SoundTool.Base.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MTF.SoundTool.Base.Helpers
{
    public static class MCA3DSHelper
    {
        private static sbyte[] NibbleToSByte = { 0, 1, 2, 3, 4, 5, 6, 7, -8, -7, -6, -5, -4, -3, -2, -1 };

        private static sbyte GetLowNibble(byte value)
        {
            return NibbleToSByte[value & 0xF];
        }

        private static sbyte GetHighNibble(byte value)
        {
            return NibbleToSByte[value >> 4];
        }

        static short SafeClampMCA(int value)
        {
            if (value < -32768) value = -32768;
            if (value > 32767) value = 32767;
            return (short)value;
        }

        private static short[] DecodeNGCDSP(byte[] soundData, MCA3DSChannel channel)
        {
            var result = new List<short>();

            using (var soundBr = new BinaryReader(new MemoryStream(soundData)))
            {
                short hist1 = 0;
                short hist2 = 0;

                while (soundBr.BaseStream.Position < soundBr.BaseStream.Length)
                {
                    byte head = soundBr.ReadByte();

                    ushort scale = (ushort)(1 << (head & 0xF));
                    byte coefIndex = (byte)(head >> 4);
                    short coef1 = channel.adpcmCoefs[2 * coefIndex];
                    short coef2 = channel.adpcmCoefs[2 * coefIndex + 1];

                    for (uint i = 0; i < 7; i++)
                    {
                        byte b = soundBr.ReadByte();

                        for (uint s = 0; s < 2; s++)
                        {
                            sbyte adpcmNibble = (s == 0) ? GetHighNibble(b) : GetLowNibble(b);
                            short sample = SafeClampMCA(((adpcmNibble * scale) << 11) + 1024 + ((coef1 * hist1) + (coef2 * hist2)) >> 11);

                            hist2 = hist1;
                            hist1 = sample;
                            result.Add(sample);
                        }
                    }
                }
            }

            return result.ToArray();
        }

        public static List<MCA3DSChannel> SetupCoefs(FileStream input, BinaryReader br, int coefOffset, int channelCount, int coefSpacing)
        {
            var startOffset = input.Position;
            var channels = new List<MCA3DSChannel>();

            br.BaseStream.Position = coefOffset;

            using (var coefBr = new BinaryReader(new MemoryStream(br.ReadBytes(channelCount * coefSpacing))))
            {
                for (int ch = 0; ch < channelCount; ch++)
                {
                    channels.Add(new MCA3DSChannel());

                    for (int i = 0; i < 16; i++)
                        channels[ch].adpcmCoefs.Add(coefBr.ReadInt16());

                    coefBr.BaseStream.Position += coefSpacing - 0x20;
                }
            }

            input.Position = startOffset;

            return channels;
        }

        public static bool ValidadeMCAFile(string FilePath, string FileName)
        {
            using (FileStream FS = new FileStream(FilePath, FileMode.Open))
            {
                using (BinaryReader BR = new BinaryReader(FS))
                {
                    if (FS.Length < 30)
                    {
                        XtraMessageBox.Show($"Error reading {FileName}: The file stream is too short to be a MCA file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }

                    string Format = "";
                    for (int i = 0; i < 4; i++)
                        Format += (char)BR.ReadByte();

                    if (Format != "MADP")
                    {
                        XtraMessageBox.Show($"Error reading {FileName}: Incorrect file format, the header must start with the extension string.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }

                    int Version = BR.ReadInt16();
                    if (Version != (int)MCAVersion.V_A)
                    {
                        XtraMessageBox.Show($"Error reading {FileName}: Unsupported file version.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }

                    short NumChannels = BR.ReadInt16();
                    if (NumChannels > 2)
                    {
                        XtraMessageBox.Show($"Error reading {FileName}: Unsupported number of channels ({NumChannels}).", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }

                    return true;
                }
            }
        }

        public static MCA3DS ReadMCA(string FilePath, string FileName)
        {
            if (!ValidadeMCAFile(FilePath, FileName))
                return null;

            try
            {
                using (FileStream FS = new FileStream(FilePath, FileMode.Open))
                {
                    using (BinaryReader BR = new BinaryReader(FS))
                    {
                        MCA3DS MCAFile = new MCA3DS();

                        for (int i = 0; i < 4; i++)
                            MCAFile.Format += (char)BR.ReadByte();

                        MCAFile.Version = BR.ReadInt32();
                        MCAFile.NumChannels = BR.ReadInt16();
                        MCAFile.Interleave = BR.ReadInt16();
                        MCAFile.Samples = BR.ReadInt32();
                        MCAFile.SampleRate = BR.ReadInt32();
                        MCAFile.LoopStart = BR.ReadInt32();
                        MCAFile.LoopEnd = BR.ReadInt32();
                        MCAFile.HeaderSize = BR.ReadInt32();
                        MCAFile.StreamSize = BR.ReadInt32();

                        int coefStart = 0;
                        int coefShift = 0;
                        int coefSpacing = 0x30;

                        switch (MCAFile.Version)
                        {
                            case (int)MCAVersion.V_A:
                                MCAFile.HeaderSize = (int)(FS.Length - MCAFile.StreamSize);
                                coefStart = MCAFile.HeaderSize - coefSpacing * MCAFile.NumChannels;
                                MCAFile.CoefOffset = coefStart + coefShift * 0x14;
                                MCAFile.StreamOffset = MCAFile.HeaderSize;

                                break;
                        }

                        MCAFile.IsLooped = MCAFile.LoopEnd > 0;
                        if (MCAFile.LoopEnd > MCAFile.Samples)
                            MCAFile.LoopEnd = MCAFile.Samples;

                        MCAFile.Channels = SetupCoefs(FS, BR, MCAFile.CoefOffset, MCAFile.NumChannels, coefSpacing);

                        FS.Position = MCAFile.StreamOffset;
                        MCAFile.SoundData = BR.ReadBytes(MCAFile.StreamSize);

                        MCAFile.DurationSpan = TimeSpan.FromSeconds((double)MCAFile.Samples / MCAFile.SampleRate);
                        MCAFile.ExpectedFileName = $"{Path.GetFileNameWithoutExtension(FilePath)}.mca";
                        MCAFile.DisplayFormat = "MADP";

                        return MCAFile;
                    }
                }
            }
            catch (Exception)
            {
                XtraMessageBox.Show($"Error reading {FileName}: File seems to be corrupted, please refer to a valid MCA file when using this tool.", "Ops!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        public static void WriteMCA(string FilePath, MCA3DS MCAFile, bool MessageBox = true)
        {
            using (FileStream FS = new FileStream(FilePath, FileMode.Create))
            {
                using (BinaryWriter BW = new BinaryWriter(FS))
                {
                    BW.Write(MCAFile.Format.ToCharArray());
                    BW.Write(MCAFile.Version);
                    BW.Write(MCAFile.NumChannels);
                    BW.Write(MCAFile.Interleave);
                    BW.Write(MCAFile.Samples);
                    BW.Write(MCAFile.SampleRate);
                    BW.Write(MCAFile.LoopStart);
                    BW.Write(MCAFile.LoopEnd);
                    BW.Write(MCAFile.HeaderSize);
                    BW.Write(MCAFile.StreamSize);

                    for (int ch = 0; ch < MCAFile.Channels.Count; ch++)
                    {

                    }

                    BW.Write(MCAFile.SoundData);

                    if (MessageBox)
                        XtraMessageBox.Show("MCA file written sucessfully!", "Done!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        public static WAVE ConvertToWAVE(MCA3DS MCAFile)
        {
            WAVE WAVEFile = new WAVE();

            WAVEFile.ChunkID = "RIFF";
            WAVEFile.Format = "WAVE";

            WAVEFile.Subchunk1ID = "fmt ";
            WAVEFile.Subchunk1Size = 16;
            WAVEFile.AudioFormat = 1;
            WAVEFile.NumChannels = (ushort)MCAFile.NumChannels;
            WAVEFile.SampleRate = (uint)MCAFile.SampleRate;
            WAVEFile.BitsPerSample = 16;

            WAVEFile.ByteRate = WAVEFile.SampleRate * WAVEFile.NumChannels * WAVEFile.BitsPerSample / 8;
            WAVEFile.BlockAlign = (ushort)(WAVEFile.NumChannels * WAVEFile.BitsPerSample / 8);

            WAVEFile.Subchunk2ID = "data";
            WAVEFile.Subchunk2Size = (uint)(MCAFile.Samples * WAVEFile.NumChannels * WAVEFile.BitsPerSample / 8);

            var decoded = new List<short>();

            switch (MCAFile.NumChannels)
            {
                case 1:
                    decoded = DecodeNGCDSP(MCAFile.SoundData, MCAFile.Channels[0]).ToList();
                    break;
                case 2:
                    var channelData = new List<List<byte>>();

                    for (int i = 0; i < MCAFile.NumChannels; i++)
                        channelData.Add(new List<byte>());

                    using (var soundBr = new BinaryReader(new MemoryStream(MCAFile.SoundData)))
                    {
                        while (soundBr.BaseStream.Position < soundBr.BaseStream.Length)
                            for (int i = 0; i < MCAFile.NumChannels; i++)
                                channelData[i].AddRange(soundBr.ReadBytes(MCAFile.Interleave));
                    }

                    var tmpDec = new short[2][];
                    for (int i = 0; i < MCAFile.NumChannels; i++)
                        tmpDec[i] = DecodeNGCDSP(channelData[i].ToArray(), MCAFile.Channels[i]);

                    int index = 0;
                    while (index < tmpDec[0].Length)
                    {
                        decoded.Add(tmpDec[0][index]);
                        decoded.Add(tmpDec[1][index]);
                        index++;
                    }

                    break;
            }

            WAVEFile.Subchunk2Data = decoded.ToArray();
            WAVEFile.ChunkSize = 4 + 8 + WAVEFile.Subchunk1Size + 8 + WAVEFile.Subchunk2Size;

            return WAVEFile;
        }

        public static MCA3DS ConvertToMCA(WAVE WAVEFile)
        {
            throw new NotImplementedException("Conversion to MCA is not implemented yet.");
        }
    }
}
