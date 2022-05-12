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
using System.Windows.Forms;

namespace MTF.SoundTool.Base.Helpers
{
    public static class WAVEHelper
    {
        public static bool ValidadeWAVEFile(string FilePath, string FileName, bool MessageBox = true)
        {
            using (FileStream FS = new FileStream(FilePath, FileMode.Open))
            {
                using (BinaryReader BR = new BinaryReader(FS))
                {
                    if (FS.Length < 28)
                    {
                        if (MessageBox)
                            XtraMessageBox.Show($"Error reading {FileName}: The file stream is too short to be a WAVE file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

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
                    if (ChunkSize + 8 < FS.Length)
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
                    ushort NumChannels = BR.ReadUInt16();
                    uint SampleRate = BR.ReadUInt32();

                    if (Format != "WAVE" || Subchunck1ID != "fmt " || Subchunk1Size != 16 || AudioFormat != 1)
                    {
                        if (MessageBox)
                            XtraMessageBox.Show($"Error reading {FileName}: Incorrect WAVE format, please refer to valid 16 bit PCM WAVE file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                        return false;
                    }

                    if (NumChannels != 1)
                    {
                        if (MessageBox)
                            XtraMessageBox.Show($"Error reading {FileName}: Incorrect WAVE channel count, only mono WAVE files are supported.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                        return false;
                    }

                    if (SampleRate != 48000)
                    {
                        if (MessageBox)
                            XtraMessageBox.Show($"Error reading {FileName}: Incorrect WAVE sample rate, only WAVE files with a sample rate of 48000(Hz) are supported.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                        return false;
                    }

                    return true;
                }
            }
        }

        public static WAVE ReadWAVE(string FilePath, string FileName)
        {
            if (!ValidadeWAVEFile(FilePath, FileName))
                return null;

            try
            {
                using (FileStream FS = new FileStream(FilePath, FileMode.Open))
                {
                    using (BinaryReader BR = new BinaryReader(FS))
                    {
                        WAVE WAVEFile = new WAVE();

                        for (int i = 0; i < 4; i++)
                            WAVEFile.ChunkID += (char)BR.ReadByte();

                        WAVEFile.ChunkSize = BR.ReadUInt32();

                        for (int i = 0; i < 4; i++)
                            WAVEFile.Format += (char)BR.ReadByte();

                        for (int i = 0; i < 4; i++)
                            WAVEFile.Subchunk1ID += (char)BR.ReadByte();

                        WAVEFile.Subchunk1Size = BR.ReadUInt32();
                        WAVEFile.AudioFormat = BR.ReadUInt16();
                        WAVEFile.NumChannels = BR.ReadUInt16();
                        WAVEFile.SampleRate = BR.ReadUInt32();
                        WAVEFile.ByteRate = BR.ReadUInt32();
                        WAVEFile.BlockAlign = BR.ReadUInt16();
                        WAVEFile.BitsPerSample = BR.ReadUInt16();

                        for (int i = 0; i < 4; i++)
                            WAVEFile.Subchunk2ID += (char)BR.ReadByte();

                        WAVEFile.Subchunk2Size = BR.ReadUInt32();

                        long Samples = WAVEFile.Subchunk2Size / WAVEFile.NumChannels / (WAVEFile.BitsPerSample / 8);

                        WAVEFile.Subchunk2Data = new short[Samples];

                        for (int i = 0; i < Samples; i++)
                            WAVEFile.Subchunk2Data[i] = BR.ReadInt16();

                        return WAVEFile;
                    }
                }
            }
            catch (Exception)
            {
                XtraMessageBox.Show($"Error reading {FileName}: File seems to be corrupted, please refer to a valid WAVE file when using this tool.", "Ops!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        public static void WriteWAVE(string FilePath, WAVE WAVEFile, bool MessageBox = true)
        {
            using (FileStream FS = new FileStream(FilePath, FileMode.Create))
            {
                using (BinaryWriter BW = new BinaryWriter(FS))
                {
                    BW.Write(WAVEFile.ChunkID.ToCharArray());
                    BW.Write(WAVEFile.ChunkSize);
                    BW.Write(WAVEFile.Format.ToCharArray());

                    BW.Write(WAVEFile.Subchunk1ID.ToCharArray());
                    BW.Write(WAVEFile.Subchunk1Size);
                    BW.Write(WAVEFile.AudioFormat);
                    BW.Write(WAVEFile.NumChannels);
                    BW.Write(WAVEFile.SampleRate);
                    BW.Write(WAVEFile.ByteRate);
                    BW.Write(WAVEFile.BlockAlign);
                    BW.Write(WAVEFile.BitsPerSample);

                    BW.Write(WAVEFile.Subchunk2ID.ToCharArray());
                    BW.Write(WAVEFile.Subchunk2Size);

                    foreach (short Sample in WAVEFile.Subchunk2Data)
                        BW.Write(Sample);

                    if (MessageBox)
                        XtraMessageBox.Show("WAVE file written sucessfully!", "Done!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
    }
}
