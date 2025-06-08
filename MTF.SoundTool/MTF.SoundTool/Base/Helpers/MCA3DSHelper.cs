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

        private static short SafeClampMCA(int value)
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

        private static List<MCA3DSChannel> SetupCoefs(FileStream input, BinaryReader br, int coefOffset, int channelCount, int coefSpacing)
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

        private static short[][] GetCoefs(byte[] soundData, int numSamples)
        {
            return NGCDSPEncoder.DSPCorrelateCoefs(soundData, numSamples);
        }

        private static byte[] EncodeNGCDSP(byte[] soundData, int numSamples, short[][] coefs)
        {
            var ms = new MemoryStream();
            using (var bw = new BinaryWriter(ms))
            using (var br = new BinaryReader(new MemoryStream(soundData)))
            {
                List<short> pcmBlock = null;
                while (br.BaseStream.Position < br.BaseStream.Length)
                {
                    if (pcmBlock != null)
                    {
                        var y = pcmBlock[14];
                        var n = pcmBlock[15];
                        pcmBlock = new List<short>();
                        pcmBlock.Add(y);
                        pcmBlock.Add(n);
                    }
                    else
                    {
                        pcmBlock = new List<short>();
                        pcmBlock.Add(0);
                        pcmBlock.Add(0);
                    }

                    if (br.BaseStream.Length - br.BaseStream.Position < 28)
                    {
                        for (int i = 0; i < br.BaseStream.Length - br.BaseStream.Position; i += 2)
                            pcmBlock.Add(br.ReadInt16());
                        while (pcmBlock.Count() < 16) pcmBlock.Add(0);
                    }
                    else
                    {
                        for (int i = 0; i < 14; i++)
                            pcmBlock.Add(br.ReadInt16());
                    }

                    var adpcm = NGCDSPEncoder.DSPEncodeFrame(pcmBlock.ToArray(), 14, coefs);
                    bw.Write(adpcm);
                }
            }

            return ms.ToArray();
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
                        MCAFile.UnknownA = BR.ReadSingle();
                        MCAFile.coefShift = BR.ReadInt16();
                        MCAFile.UnknownB = BR.ReadInt16();

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
                    BW.Write(MCAFile.UnknownA);
                    BW.Write(MCAFile.coefShift);
                    BW.Write(MCAFile.UnknownB);

                    for (int i = 0; i < 8; i++)
                        BW.Write((byte)0);

                    for (int ch = 0; ch < MCAFile.NumChannels; ch++)
                    {
                        for (var i = 0; i < 8; i++)
                            for (var j = 0; j < 2; j++)
                                BW.Write(MCAFile.CoefOutput[ch][i][j]);

                        for (var i = 0; i < 0x10; i++)
                            BW.Write((byte)0);
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

            WAVEFile.Subchunk2ID = "data";
            WAVEFile.Subchunk2Data = decoded.ToArray();
            WAVEFile.Subchunk2Size = (uint)(WAVEFile.Subchunk2Data.Length * WAVEFile.NumChannels * WAVEFile.BitsPerSample / 8);         

            WAVEFile.ChunkSize = 4 + 8 + WAVEFile.Subchunk1Size + 8 + WAVEFile.Subchunk2Size;

            return WAVEFile;
        }

        public static MCA3DS ConvertToMCA(WAVE WAVEFile, int LoopStart = 0, int LoopEnd = 0)
        {
            MCA3DS MCAFile = new MCA3DS();

            int numSamples = (int)(WAVEFile.Subchunk2Size / WAVEFile.NumChannels / (WAVEFile.BitsPerSample / 8));
            if (LoopStart > numSamples)
                LoopStart = numSamples;
            if (LoopEnd < LoopStart)
                LoopEnd = LoopStart;
            if (LoopEnd > numSamples)
                LoopEnd = numSamples;

            byte[] encoded;
            byte[] soundData = new byte[WAVEFile.Subchunk2Size];

            using (var soundMs = new MemoryStream(soundData))
            {
                using (var soundBw = new BinaryWriter(soundMs))
                {
                    for (int i = 0; i < WAVEFile.Subchunk2Data.Length; i++)
                        soundBw.Write(WAVEFile.Subchunk2Data[i]);
                }
            }

            if (WAVEFile.NumChannels == 1)
            {
                MCAFile.CoefOutput.Add(GetCoefs(soundData, numSamples));
                encoded = EncodeNGCDSP(soundData, numSamples, MCAFile.CoefOutput[0]);
            }
            else
            {
                var channelData = new List<List<byte>>();

                for (int i = 0; i < WAVEFile.NumChannels; i++)
                    channelData.Add(new List<byte>());

                using (var soundBr = new BinaryReader(new MemoryStream(soundData)))
                {
                    while (soundBr.BaseStream.Position < soundBr.BaseStream.Length)
                        for (int i = 0; i < WAVEFile.NumChannels; i++)
                            channelData[i].AddRange(soundBr.ReadBytes(2));
                }

                for (int i = 0; i < WAVEFile.NumChannels; i++)
                    MCAFile.CoefOutput.Add(GetCoefs(channelData[i].ToArray(), numSamples));

                List<List<byte>> tmpEnc = new List<List<byte>>();

                for (int i = 0; i < WAVEFile.NumChannels; i++)
                    tmpEnc.Add(EncodeNGCDSP(channelData[i].ToArray(), numSamples, MCAFile.CoefOutput[i]).ToList());

                for (int i = 0; i < WAVEFile.NumChannels; i++)
                    tmpEnc[i].AddRange(new byte[0x100 - tmpEnc[i].Count() % 0x100]);

                encoded = new byte[tmpEnc[0].Count() * WAVEFile.NumChannels];
                int blocksIn = 0;
                int blocksInData = tmpEnc[0].Count() / 0x100;
                while (blocksIn < blocksInData)
                {
                    for (int i = 0; i < WAVEFile.NumChannels; i++)
                        Array.Copy(tmpEnc[i].ToArray(), blocksIn * 0x100, encoded, blocksIn * 0x100 * WAVEFile.NumChannels + i * 0x100, 0x100);
                    blocksIn++;
                }
            }

            MCAFile.Format = "MADP";
            MCAFile.Version = (int)MCAVersion.V_A;
            MCAFile.NumChannels = (short)WAVEFile.NumChannels;
            MCAFile.Interleave = 0x100;
            MCAFile.Samples = (int)(WAVEFile.Subchunk2Size / (WAVEFile.NumChannels * (WAVEFile.BitsPerSample / 8)));
            MCAFile.SampleRate = (int)WAVEFile.SampleRate;
            MCAFile.LoopStart = LoopStart;
            MCAFile.LoopEnd = LoopEnd;
            MCAFile.HeaderSize = ((MCAFile.Version < 5) ? 0x34 : 0x38) + 0x30 * WAVEFile.NumChannels;
            MCAFile.StreamSize = encoded.Length;
            MCAFile.UnknownA = 0.0f;
            MCAFile.coefShift = 0;
            MCAFile.UnknownB = 0;
            MCAFile.SoundData = encoded;
            
            return MCAFile;
        }
    }
}
