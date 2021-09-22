using DevExpress.XtraEditors;
using MTF.SoundTool.Base.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace MTF.SoundTool.Base.Helpers
{
    public static class SPACHelper
    {
        public static bool ValidadeSPACFile(string FilePath, string FileName)
        {
            using (FileStream FS = new FileStream(FilePath, FileMode.Open))
            {
                using (BinaryReader BR = new BinaryReader(FS))
                {
                    if (FS.Length < 8)
                    {
                        XtraMessageBox.Show($"Error reading {FileName}: The file stream is too short to be a SPC file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }

                    string Format = "";
                    for (int i = 0; i < 4; i++)
                        Format += (char)BR.ReadByte();

                    if (Format != "SPAC")
                    {
                        XtraMessageBox.Show($"Error reading {FileName}: Incorrect file format, the header must start with the extension string.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }

                    int Version = BR.ReadInt32();
                    if (Version != (int)SPACVersion.RE5 && Version != (int)SPACVersion.RE6)
                    {
                        XtraMessageBox.Show($"Error reading {FileName}: Unsupported file version, only RE5 and RE6 versions are currently supported.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }

                    return true;
                }
            }
        }

        public static SPAC ReadSPAC(string FilePath, string FileName)
        {
            if (!ValidadeSPACFile(FilePath, FileName))
                return null;

            try
            {
                using (FileStream FS = new FileStream(FilePath, FileMode.Open))
                {
                    using (BinaryReader BR = new BinaryReader(FS))
                    {
                        SPAC SPACFile = new SPAC();

                        for (int i = 0; i < 4; i++)
                            SPACFile.Format += (char)BR.ReadByte();

                        SPACFile.Version = BR.ReadInt32();
                        SPACFile.Sounds = BR.ReadInt32();
                        SPACFile.UnknownDataA = BR.ReadInt32();
                        SPACFile.UnknownDataB = BR.ReadInt32();
                        SPACFile.MetaAStart = BR.ReadInt32();
                        SPACFile.MetaBStart = BR.ReadInt32();
                        SPACFile.SoundDataStart = BR.ReadInt32();

                        long StartPostion = BR.BaseStream.Position;

                        int MetaASize = SPACFile.MetaBStart - SPACFile.MetaAStart;
                        int MetaBSize = SPACFile.SoundDataStart - SPACFile.MetaBStart;

                        BR.BaseStream.Position = SPACFile.MetaAStart;
                        SPACFile.MetaA = BR.ReadBytes(MetaASize);

                        BR.BaseStream.Position = SPACFile.MetaBStart;
                        SPACFile.MetaB = BR.ReadBytes(MetaBSize);

                        BR.BaseStream.Position = StartPostion;

                        long HeaderStartPosition = BR.BaseStream.Position;
                        long SoundStartPosition = SPACFile.SoundDataStart;

                        switch (SPACFile.Version)
                        {
                            case (int)SPACVersion.RE5:
                                SPACFile.FWSEFiles = new List<FWSE>();

                                for (int i = 0; i < SPACFile.Sounds; i++)
                                {
                                    BR.BaseStream.Position = HeaderStartPosition;

                                    FWSE FWSEFile = new FWSE(i);

                                    for (int f = 0; f < 4; f++)
                                        FWSEFile.Format += (char)BR.ReadByte();

                                    FWSEFile.Version = BR.ReadInt32();
                                    FWSEFile.FileSize = BR.ReadInt32();
                                    FWSEFile.HeaderSize = BR.ReadInt32();
                                    FWSEFile.Channels = BR.ReadInt32();
                                    FWSEFile.Samples = BR.ReadInt32();
                                    FWSEFile.SampleRate = BR.ReadInt32();
                                    FWSEFile.BitPerSample = BR.ReadInt32();
                                    FWSEFile.InfoData = BR.ReadBytes(FWSEFile.HeaderSize - 32);

                                    HeaderStartPosition = BR.BaseStream.Position;
                                    BR.BaseStream.Position = SoundStartPosition;

                                    FWSEFile.SoundData = BR.ReadBytes(FWSEFile.FileSize - FWSEFile.HeaderSize);
                                    SoundStartPosition = BR.BaseStream.Position;

                                    FWSEFile.DurationSpan = TimeSpan.FromSeconds((double)FWSEFile.Samples / FWSEFile.SampleRate);
                                    FWSEFile.ExpectedFileName = $"{FWSEFile.Index}.fwse";
                                    FWSEFile.DisplayFormat = "FWSE";

                                    SPACFile.FWSEFiles.Add(FWSEFile);
                                }

                                break;
                            case (int)SPACVersion.RE6:
                                SPACFile.XSEWFiles = new List<XSEW>();

                                for (int i = 0; i < SPACFile.Sounds; i++)
                                {
                                    BR.BaseStream.Position = HeaderStartPosition;

                                    XSEW XSEWFile = new XSEW(i);

                                    for (int j = 0; j < 4; j++)
                                        XSEWFile.ChunkID += (char)BR.ReadByte();

                                    XSEWFile.ChunkSize = BR.ReadUInt32();

                                    for (int j = 0; j < 4; j++)
                                        XSEWFile.Format += (char)BR.ReadByte();

                                    for (int j = 0; j < 4; j++)
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

                                    HeaderStartPosition = BR.BaseStream.Position;
                                    BR.BaseStream.Position = SoundStartPosition;

                                    XSEWFile.BlockHeaderContentCount = XSEWFile.BlockAlign - XSEWHelper.XSEWBlockHeaderContentByteCount + XSEWHelper.XSEWBlockHeaderContentCount;
                                    XSEWFile.BlockCount = (int)XSEWFile.Subchunk2Size / XSEWFile.BlockAlign;
                                    XSEWFile.Samples = (2 * (XSEWFile.BlockAlign - XSEWHelper.XSEWBlockHeaderContentByteCount) + 2) * XSEWFile.BlockCount;

                                    XSEWFile.Subchunk2Data = new int[XSEWFile.BlockCount][];

                                    for (int j = 0; j < XSEWFile.BlockCount; j++)
                                    {
                                        XSEWFile.Subchunk2Data[j] = new int[XSEWFile.BlockHeaderContentCount];

                                        for (int k = 0; k < XSEWFile.Subchunk2Data[j].Length; k++)
                                        {
                                            if (k < 1)
                                                XSEWFile.Subchunk2Data[j][k] = BR.ReadByte();
                                            else if (k < 4)
                                                XSEWFile.Subchunk2Data[j][k] = BR.ReadInt16();
                                            else
                                                XSEWFile.Subchunk2Data[j][k] = BR.ReadByte();
                                        }
                                    }

                                    SoundStartPosition = BR.BaseStream.Position;

                                    XSEWFile.DurationSpan = TimeSpan.FromSeconds((double)XSEWFile.Samples / XSEWFile.SampleRate);
                                    XSEWFile.ExpectedFileName = $"{XSEWFile.Index}.xsew";
                                    XSEWFile.DisplayFormat = "XSEW";

                                    SPACFile.XSEWFiles.Add(XSEWFile);
                                }

                                break;
                        }

                        return SPACFile;
                    }
                }
            }
            catch (Exception)
            {
                XtraMessageBox.Show($"Error reading {FileName}: File seems to be corrupted, please refer to a valid SPC file when using this tool.", "Ops!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        public static void WriteSPAC(string FilePath, SPAC SPACFile)
        {
            using (FileStream FS = new FileStream(FilePath, FileMode.Create))
            {
                using (BinaryWriter BW = new BinaryWriter(FS))
                {
                    BW.Write(SPACFile.Format.ToCharArray());
                    BW.Write(SPACFile.Version);
                    BW.Write(SPACFile.Sounds);
                    BW.Write(SPACFile.UnknownDataA);
                    BW.Write(SPACFile.UnknownDataB);
                    BW.Write(SPACFile.MetaAStart);
                    BW.Write(SPACFile.MetaBStart);
                    BW.Write(SPACFile.SoundDataStart);

                    for (int i = 0; i < SPACFile.Sounds; i++)
                    {
                        switch (SPACFile.Version)
                        {
                            case (int)SPACVersion.RE5:
                                BW.Write(SPACFile.FWSEFiles[i].Format.ToCharArray());
                                BW.Write(SPACFile.FWSEFiles[i].Version);
                                BW.Write(SPACFile.FWSEFiles[i].FileSize);
                                BW.Write(SPACFile.FWSEFiles[i].HeaderSize);
                                BW.Write(SPACFile.FWSEFiles[i].Channels);
                                BW.Write(SPACFile.FWSEFiles[i].Samples);
                                BW.Write(SPACFile.FWSEFiles[i].SampleRate);
                                BW.Write(SPACFile.FWSEFiles[i].BitPerSample);
                                BW.Write(SPACFile.FWSEFiles[i].InfoData);
                                break;
                            case (int)SPACVersion.RE6:
                                BW.Write(SPACFile.XSEWFiles[i].ChunkID.ToCharArray());
                                BW.Write(SPACFile.XSEWFiles[i].ChunkSize);
                                BW.Write(SPACFile.XSEWFiles[i].Format.ToCharArray());

                                BW.Write(SPACFile.XSEWFiles[i].Subchunk1ID.ToCharArray());
                                BW.Write(SPACFile.XSEWFiles[i].Subchunk1Size);
                                BW.Write(SPACFile.XSEWFiles[i].AudioFormat);
                                BW.Write(SPACFile.XSEWFiles[i].NumChannels);
                                BW.Write(SPACFile.XSEWFiles[i].SampleRate);
                                BW.Write(SPACFile.XSEWFiles[i].ByteRate);
                                BW.Write(SPACFile.XSEWFiles[i].BlockAlign);
                                BW.Write(SPACFile.XSEWFiles[i].BitsPerSample);
                                BW.Write(SPACFile.XSEWFiles[i].ExtraParamSize);
                                BW.Write(SPACFile.XSEWFiles[i].ExtraParams);

                                BW.Write(SPACFile.XSEWFiles[i].Subchunk2ID.ToCharArray());
                                BW.Write(SPACFile.XSEWFiles[i].Subchunk2Size);

                                BW.Write(SPACFile.XSEWFiles[i].Subchunk3ID.ToCharArray());
                                BW.Write(SPACFile.XSEWFiles[i].Subchunk3Size);
                                BW.Write(SPACFile.XSEWFiles[i].Subchunk3Param1);
                                BW.Write(SPACFile.XSEWFiles[i].Subchunk3Param2);
                                BW.Write(SPACFile.XSEWFiles[i].Subchunk3Samples);
                                BW.Write(SPACFile.XSEWFiles[i].Subchunk3Param3);
                                BW.Write(SPACFile.XSEWFiles[i].Subchunk3Param4);
                                BW.Write(SPACFile.XSEWFiles[i].Subchunk3Param5);
                                BW.Write(SPACFile.XSEWFiles[i].Subchunk3Param6);
                                BW.Write(SPACFile.XSEWFiles[i].Subchunk3Param7);
                                BW.Write(SPACFile.XSEWFiles[i].Subchunk3Param8);
                                BW.Write(SPACFile.XSEWFiles[i].Subchunk3Param9);
                                BW.Write(SPACFile.XSEWFiles[i].Subchunk3Param10);
                                BW.Write(SPACFile.XSEWFiles[i].Subchunk3Param11);
                                BW.Write(SPACFile.XSEWFiles[i].Subchunk3Param12);
                                BW.Write(SPACFile.XSEWFiles[i].Subchunk3Param13);
                                BW.Write(SPACFile.XSEWFiles[i].Subchunk3Param14);
                                break;
                        }
                    }

                    BW.Write(SPACFile.MetaA);
                    BW.Write(SPACFile.MetaB);

                    for (int i = 0; i < SPACFile.Sounds; i++)
                    {
                        switch (SPACFile.Version)
                        {
                            case (int)SPACVersion.RE5:
                                BW.Write(SPACFile.FWSEFiles[i].SoundData);
                                break;
                            case (int)SPACVersion.RE6:

                                XSEW XSEWFile = SPACFile.XSEWFiles[i];

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

                                break;
                        }
                    }

                    XtraMessageBox.Show("SPC file saved sucessfully!", "Done!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        public static void ReplaceSPACSound(SPAC SPACFile, object SoundFile, int Index)
        {
            switch (SPACFile.Version)
            {
                case (int)SPACVersion.RE5:
                    SPACFile.FWSEFiles[Index] = (FWSE)SoundFile;
                    break;
                case (int)SPACVersion.RE6:
                    SPACFile.XSEWFiles[Index] = (XSEW)SoundFile;
                    break;
            }
        }
    }
}
