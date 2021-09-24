using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using MTF.SoundTool.Base.Types;
using DevExpress.XtraEditors;
using System;

namespace MTF.SoundTool.Base.Helpers
{
    public static class STRQHelper
    {
        public static bool ValidadeSTRQFile(string FilePath, string FileName)
        {
            using (FileStream FS = new FileStream(FilePath, FileMode.Open))
            {
                using (BinaryReader BR = new BinaryReader(FS))
                {
                    if (FS.Length < 12)
                    {
                        XtraMessageBox.Show($"Error reading {FileName}: The file stream is too short to be a STQ file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }

                    string Format = "";
                    for (int i = 0; i < 4; i++)
                        Format += (char)BR.ReadByte();

                    if (Format != "STRQ" && Format != "STQR")
                    {
                        XtraMessageBox.Show($"Error reading {FileName}: Incorrect file format, the header must start with the extension string.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }

                    int Version = BR.ReadInt32();
                    if (Version != (int)STRQVersion.RE5 && Version != (int)STRQVersion.RE6 && Version != (int)STRQVersion.REV1 && Version != (int)STRQVersion.REV2 && Version != (int)STRQVersion.RE0)
                    {
                        XtraMessageBox.Show($"Error reading {FileName}: Unsupported file version, only RESIDENT EVIL 5/6/REV1/REV2 & UMVC3 versions are currently supported.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }

                    int Entries = BR.ReadInt32();
                    if (Entries < 1)
                    {
                        XtraMessageBox.Show($"Error reading {FileName}: This STRQ file doesn't have any entries, consider picking one that has at least one.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }

                    return true;
                }
            }
        }

        public static STRQ ReadSTRQ(string FilePath, string FileName)
        {
            if (!ValidadeSTRQFile(FilePath, FileName))
                return null;

            try
            {
                using (FileStream FS = new FileStream(FilePath, FileMode.Open))
                {
                    using (BinaryReader BR = new BinaryReader(FS))
                    {
                        // Header Reading

                        int Start = 0;

                        STRQ STRQFile = new STRQ();

                        STRQFile.Format = "";

                        for (int i = 0; i < 4; i++)
                            STRQFile.Format += (char)BR.ReadByte();

                        STRQFile.Version = BR.ReadInt32();
                        STRQFile.Entries = BR.ReadInt32();

                        long SavePosition = BR.BaseStream.Position;

                        // UMVC3 has the same version as RE0 but must be read differently
                        if (STRQFile.Version == (int) STRQVersion.RE0)
                        {
                            BR.BaseStream.Position = 80L;
                            int FilePosTry = BR.ReadInt32();
                            BR.BaseStream.Position = FilePosTry < FS.Length ? FilePosTry : 0L;

                            if (BR.BaseStream.Position != 0L)
                            {
                                string sound = "";
                                for (int i = 0; i < 5; i++)
                                    sound += (char)BR.ReadByte();

                                if (sound == "sound")
                                    STRQFile.Version = (int)STRQVersion.UMVC3;
                            }
                        }

                        BR.BaseStream.Position = SavePosition;

                        switch (STRQFile.Version)
                        {
                            case (int)STRQVersion.RE5:
                            case (int)STRQVersion.RE6:
                                Start = 0x3C;
                                break;
                            case (int)STRQVersion.REV1:
                            case (int)STRQVersion.REV2:
                            case (int)STRQVersion.RE0:
                                Start = 0x38;
                                break;
                            case (int)STRQVersion.UMVC3:
                                Start = 0x50;
                                break;
                        }

                        int HeaderDataSize = (int)(Start - BR.BaseStream.Position);
                        STRQFile.HeaderData = new byte[HeaderDataSize];
                        STRQFile.HeaderData = BR.ReadBytes(HeaderDataSize);

                        // Entries Reading

                        STRQFile.STRQEntries = new List<STRQEntry>();

                        for (int i = 0; i < STRQFile.Entries; i++)
                        {
                            STRQEntry Entry = new STRQEntry();
                            Entry.Index = i;
                            Entry.FileNamePos = BR.ReadInt32();

                            if (STRQFile.Version == (int)STRQVersion.UMVC3)
                                Entry.UnknownData3 = BR.ReadInt32();

                            Entry.FileSize = BR.ReadInt32();
                            Entry.Duration = BR.ReadInt32();
                            Entry.Channels = BR.ReadInt32();

                            if (STRQFile.Version == (int)STRQVersion.REV1 || STRQFile.Version == (int)STRQVersion.REV2 || STRQFile.Version == (int)STRQVersion.RE0 || STRQFile.Version == (int)STRQVersion.UMVC3)
                            {
                                Entry.SampleRate = BR.ReadInt32();
                                Entry.SampleRate = Entry.SampleRate > 0 ? Entry.SampleRate : 48000;
                            }
                            else
                                Entry.SampleRate = 48000;

                            Entry.LoopStart = BR.ReadInt32();
                            Entry.LoopEnd = BR.ReadInt32();

                            if (STRQFile.Version == (int)STRQVersion.REV1 || STRQFile.Version == (int)STRQVersion.REV2 || STRQFile.Version == (int)STRQVersion.RE0 || STRQFile.Version == (int)STRQVersion.UMVC3)
                            {
                                Entry.UnknownData1 = BR.ReadInt32();
                                Entry.UnknownData2 = BR.ReadInt32();
                            }

                            // Update samples and spans to the precision point of span ticks.

                            TimeSpan DurationSpan = Entry.DurationSpan;
                            TimeSpan LoopStartSpan = Entry.LoopStartSpan;
                            TimeSpan LoopEndSpan = Entry.LoopEndSpan;

                            int Duration = Entry.Duration;
                            int LoopStart = Entry.LoopStart;
                            int LoopEnd = Entry.LoopEnd;

                            UpdateSamples(ref DurationSpan, ref Duration, Entry.SampleRate, STRQSampleMode.TimeSpan);
                            UpdateSamples(ref LoopStartSpan, ref LoopStart, Entry.SampleRate, STRQSampleMode.TimeSpan);
                            UpdateSamples(ref LoopEndSpan, ref LoopEnd, Entry.SampleRate, STRQSampleMode.TimeSpan);

                            Entry.DurationSpan = DurationSpan;
                            Entry.LoopStartSpan = LoopStartSpan;
                            Entry.LoopEndSpan = LoopEndSpan;

                            UpdateSamples(ref DurationSpan, ref Duration, Entry.SampleRate, STRQSampleMode.Integer);
                            UpdateSamples(ref LoopStartSpan, ref LoopStart, Entry.SampleRate, STRQSampleMode.Integer);
                            UpdateSamples(ref LoopEndSpan, ref LoopEnd, Entry.SampleRate, STRQSampleMode.Integer);

                            Entry.Duration = Duration;
                            Entry.LoopStart = LoopStart;
                            Entry.LoopEnd = LoopEnd;

                            // All set now add to the list

                            STRQFile.STRQEntries.Add(Entry);
                        }

                        // Unknown Data

                        int UnknownDataSize = (int)(STRQFile.STRQEntries[0].FileNamePos - BR.BaseStream.Position);
                        STRQFile.UnknownData = new byte[UnknownDataSize];
                        STRQFile.UnknownData = BR.ReadBytes(UnknownDataSize);

                        // Getting each entry's file path and name

                        for (int i = 0; i < STRQFile.Entries; i++)
                        {
                            string FileFullPath = "";

                            BR.BaseStream.Position = STRQFile.STRQEntries[i].FileNamePos;

                            for (int j = 0; j < (((i + 1) < STRQFile.Entries ? STRQFile.STRQEntries[i + 1].FileNamePos : FS.Length) - STRQFile.STRQEntries[i].FileNamePos) - 1; j++)
                            {
                                FileFullPath += (char)BR.ReadByte();
                            }

                            STRQFile.STRQEntries[i].Path = FileFullPath.Substring(0, FileFullPath.LastIndexOf(@"\") + 1);
                            STRQFile.STRQEntries[i].Name = FileFullPath.Substring(FileFullPath.LastIndexOf(@"\") + 1);
                        }

                        return STRQFile;
                    }
                }
            }
            catch (Exception)
            {
                XtraMessageBox.Show($"Error reading {FileName}: File seems to be corrupted, please refer to a valid STQ file when using this tool.", "Ops!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        public static void WriteSTRQ(string FilePath, STRQ STRQFile)
        {
            using (FileStream FS = new FileStream(FilePath, FileMode.Create))
            {
                using (BinaryWriter BW = new BinaryWriter(FS))
                {
                    BW.Write(STRQFile.Format.ToCharArray());

                    if (STRQFile.Version == (int) STRQVersion.UMVC3)
                        BW.Write((int) STRQVersion.RE0);
                    else
                        BW.Write(STRQFile.Version);

                    BW.Write(STRQFile.Entries);
                    BW.Write(STRQFile.HeaderData);

                    for (int i = 0; i < STRQFile.Entries; i++)
                    {
                        BW.Write(STRQFile.STRQEntries[i].FileNamePos);

                        if (STRQFile.Version == (int)STRQVersion.UMVC3)
                            BW.Write(STRQFile.STRQEntries[i].UnknownData3);

                        BW.Write(STRQFile.STRQEntries[i].FileSize);
                        BW.Write(STRQFile.STRQEntries[i].Duration);
                        BW.Write(STRQFile.STRQEntries[i].Channels);

                        if (STRQFile.Version == (int)STRQVersion.REV1 || STRQFile.Version == (int)STRQVersion.REV2 || STRQFile.Version == (int)STRQVersion.RE0 || STRQFile.Version == (int)STRQVersion.UMVC3)
                            BW.Write(STRQFile.STRQEntries[i].SampleRate);

                        BW.Write(STRQFile.STRQEntries[i].LoopStart);
                        BW.Write(STRQFile.STRQEntries[i].LoopEnd);

                        if (STRQFile.Version == (int)STRQVersion.REV1 || STRQFile.Version == (int)STRQVersion.REV2 || STRQFile.Version == (int)STRQVersion.RE0 || STRQFile.Version == (int)STRQVersion.UMVC3)
                        {
                            BW.Write(STRQFile.STRQEntries[i].UnknownData1);
                            BW.Write(STRQFile.STRQEntries[i].UnknownData2);
                        }
                    }

                    BW.Write(STRQFile.UnknownData);

                    for (int i = 0; i < STRQFile.Entries; i++)
                    {
                        BW.Write((STRQFile.STRQEntries[i].Path + STRQFile.STRQEntries[i].Name).ToCharArray());
                        BW.Write((byte)0);
                    }

                    XtraMessageBox.Show("STQ file saved sucessfully!", "Done!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        public static void UpdateSamples(ref TimeSpan Span, ref int Samples, int Rate, STRQSampleMode Mode)
        {
            switch (Mode)
            {
                case STRQSampleMode.TimeSpan:
                    double calc_a = Utility.Clamp((double)Samples / Rate, 0, (double)2147483647 / Rate);
                    Span = TimeSpan.FromSeconds(calc_a);
                    break;
                case STRQSampleMode.Integer:
                    double calc_b = Span.TotalSeconds * Rate;
                    int calc_c = Utility.Clamp((int)calc_b, 0, 2147483647);
                    Samples = calc_c > 0 ? calc_c : -1;
                    break;
            }
        }

        public static void UpdateEntries(STRQ STRQFile, STRQSampleMode SampleMode)
        {
            if (STRQFile.STRQEntries == null || STRQFile.STRQEntries.Count == 0)
                return;

            int BaseOffset = STRQFile.STRQEntries[0].FileNamePos;
            int TempOffset = BaseOffset;

            for (int i = 1; i < (STRQFile.Entries + 1); i++)
            {
                STRQEntry Entry = STRQFile.STRQEntries[i - 1];

                // String validations

                string Path = StringHelper.ValidatePath(Entry.Path);
                string Name = StringHelper.ValidateName(Entry.Name);
                string FileFullPath = Path + Name;

                Entry.Path = Path;
                Entry.Name = Name;

                // Update path offsets (not needed for the last entry so we skip the last index)

                if (i < STRQFile.Entries)
                {
                    TempOffset += FileFullPath.ToCharArray().Length + 1;
                    STRQFile.STRQEntries[i].FileNamePos = TempOffset;
                }

                // Update samples

                TimeSpan DurationSpan = Entry.DurationSpan;
                TimeSpan LoopStartSpan = Entry.LoopStartSpan;
                TimeSpan LoopEndSpan = Entry.LoopEndSpan;

                int Duration = Entry.Duration;
                int LoopStart = Entry.LoopStart;
                int LoopEnd = Entry.LoopEnd;

                switch (SampleMode)
                {
                    case STRQSampleMode.TimeSpan:

                        UpdateSamples(ref DurationSpan, ref Duration, Entry.SampleRate, STRQSampleMode.TimeSpan);
                        UpdateSamples(ref LoopStartSpan, ref LoopStart, Entry.SampleRate, STRQSampleMode.TimeSpan);
                        UpdateSamples(ref LoopEndSpan, ref LoopEnd, Entry.SampleRate, STRQSampleMode.TimeSpan);

                        Entry.DurationSpan = DurationSpan;
                        Entry.LoopStartSpan = LoopStartSpan;
                        Entry.LoopEndSpan = LoopEndSpan;

                        UpdateSamples(ref DurationSpan, ref Duration, Entry.SampleRate, STRQSampleMode.Integer);
                        UpdateSamples(ref LoopStartSpan, ref LoopStart, Entry.SampleRate, STRQSampleMode.Integer);
                        UpdateSamples(ref LoopEndSpan, ref LoopEnd, Entry.SampleRate, STRQSampleMode.Integer);

                        Entry.Duration = Duration;
                        Entry.LoopStart = LoopStart;
                        Entry.LoopEnd = LoopEnd;

                        break;
                    case STRQSampleMode.Integer:

                        UpdateSamples(ref DurationSpan, ref Duration, Entry.SampleRate, STRQSampleMode.Integer);
                        UpdateSamples(ref LoopStartSpan, ref LoopStart, Entry.SampleRate, STRQSampleMode.Integer);
                        UpdateSamples(ref LoopEndSpan, ref LoopEnd, Entry.SampleRate, STRQSampleMode.Integer);

                        Entry.Duration = Duration;
                        Entry.LoopStart = LoopStart;
                        Entry.LoopEnd = LoopEnd;

                        UpdateSamples(ref DurationSpan, ref Duration, Entry.SampleRate, STRQSampleMode.TimeSpan);
                        UpdateSamples(ref LoopStartSpan, ref LoopStart, Entry.SampleRate, STRQSampleMode.TimeSpan);
                        UpdateSamples(ref LoopEndSpan, ref LoopEnd, Entry.SampleRate, STRQSampleMode.TimeSpan);

                        Entry.DurationSpan = DurationSpan;
                        Entry.LoopStartSpan = LoopStartSpan;
                        Entry.LoopEndSpan = LoopEndSpan;

                        break;
                }
            }
        }
    }
}
