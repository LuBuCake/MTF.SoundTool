/*
    This file is part of RESIDENT EVIL SPC Tool.
    RESIDENT EVIL STQ Tool is free software: you can redistribute it
    and/or modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation, either version 3 of
    the License, or (at your option) any later version.
    RESIDENT EVIL STQ Tool is distributed in the hope that it will
    be useful, but WITHOUT ANY WARRANTY; without even the implied
    warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
    See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License
    along with RESIDENT EVIL SPC Tool. If not, see <https://www.gnu.org/licenses/>6.
*/

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Media;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.LookAndFeel;
using DevExpress.XtraEditors;
using MTF.SoundTool.Base.Helpers;
using MTF.SoundTool.Base.Types;

namespace MTF.SoundTool
{
    public partial class App : XtraForm
    {
        private SoundPlayer SPACSoundPlayer { get; set; }
        private SPAC SPACFile { get; set; }

        public App()
        {
            InitializeComponent();
        }

        private async void App_Load(object sender, EventArgs e)
        {
            string AppDir = Directory.GetCurrentDirectory();
            string ConfigDir = AppDir + "/RESPCTool.exe.config";
            string UpdaterConfigDir = AppDir + "/Updater.exe.config";

            if (File.Exists(ConfigDir))
            {
                string Config = File.ReadAllText(ConfigDir);

                if (Config.Contains("VS Dark"))
                    ThemeRadioGroup.EditValue = "VS Dark";
                else if (Config.Contains("VS Light"))
                    ThemeRadioGroup.EditValue = "VS Light";
            }

            if (File.Exists(UpdaterConfigDir))
            {
                string UpdaterConfig = File.ReadAllText(UpdaterConfigDir);
                string Value = ThemeRadioGroup.EditValue.ToString();

                if (UpdaterConfig.Contains("VS Dark") && Value == "VS Light")
                    UpdaterConfig = UpdaterConfig.Replace("VS Dark", Value);
                else if (UpdaterConfig.Contains("VS Light") && Value == "VS Dark")
                    UpdaterConfig = UpdaterConfig.Replace("VS Light", Value);

                File.WriteAllText(UpdaterConfigDir, UpdaterConfig);
            }

            FileNameTextEdit.Text = "No SPC file loaded.";
            ThemeRadioGroup.Enabled = false;
            OpenFileButton.Enabled = false;
            SaveFileButton.Enabled = false;
            CloseFileButton.Enabled = false;
            GitHubButton.Enabled = false;
            ForumButton.Enabled = false;
            ExtractDataButton.Enabled = false;
            ReplaceDataButton.Enabled = false;
            ExtractDecodedCheckEdit.Enabled = false;

            bool UpdateAllowed = await CheckForToolUpdate();

            if (UpdateAllowed)
                return;

            SetupControls();
        }

        private async Task<bool> CheckForToolUpdate(bool IgnoreUpdater = false)
        {
            if (!IgnoreUpdater)
            {
                bool UpdaterMustUpdate = await CheckForUpdaterUpdate();

                if (UpdaterMustUpdate)
                    return true;
            }

            bool HasConnection = await Task.Run(() => Utility.TestConnection("8.8.8.8"));

            if (!HasConnection)
            {
                return false;
            }

            Text = "Resident Evil - SPC Tool - Checking Tool Version";

            using (WebClient GitHubChecker = new WebClient())
            {
                string LatestVerion = await Task.Run(() => GitHubChecker.DownloadString("https://raw.githubusercontent.com/LuBuCake/RESPCTool/main/RESPCTool/RESPCTool.Versioning/RESPCTool/latest.txt"));

                Assembly CurApp = Assembly.GetExecutingAssembly();
                AssemblyName CurName = new AssemblyName(CurApp.FullName);

                int Current = int.Parse(CurName.Version.ToString().Replace(".", ""));
                int Latest = int.Parse(LatestVerion.Replace(".", ""));

                if (Current >= Latest)
                {
                    return false;
                }

                if (XtraMessageBox.Show("A new version is available. Would you like to update it now?", "Info", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    string AppDirectory = Directory.GetCurrentDirectory();
                    string UpdaterDirectory = AppDirectory + "/updater/";

                    if (!Directory.Exists(UpdaterDirectory))
                        Directory.CreateDirectory(UpdaterDirectory);

                    AppVersion _version = new AppVersion()
                    {
                        FileRoute = "https://raw.githubusercontent.com/LuBuCake/RESPCTool/main/RESPCTool/RESPCTool.Versioning/RESPCTool/latest.zip",
                    };

                    Serializer.WriteDataFile(UpdaterDirectory + "updateapp.json", Serializer.SerializeAppVersion(_version));

                    Process.Start(AppDirectory + "/Updater.exe");
                    Application.Exit();

                    return true;
                }

                return false;
            }
        }

        private async Task<bool> CheckForUpdaterUpdate()
        {
            bool HasConnection = await Task.Run(() => Utility.TestConnection("8.8.8.8"));

            if (!HasConnection)
            {
                return false;
            }

            Text = "Resident Evil - SPC Tool - Checking Updater Version";

            using (WebClient GitHubChecker = new WebClient())
            {
                string LatestVerion = await Task.Run(() => GitHubChecker.DownloadString("https://raw.githubusercontent.com/LuBuCake/RESPCTool/main/RESPCTool/RESPCTool.Versioning/RESPCTool.Updater/latest.txt"));
                string FilePath = Directory.GetCurrentDirectory() + "/updater.exe";

                AssemblyName CurName = new AssemblyName();

                bool FileExists = File.Exists(FilePath);

                if (FileExists)
                {
                    Assembly CurApp = Assembly.Load(File.ReadAllBytes(FilePath));
                    CurName = new AssemblyName(CurApp.FullName);
                }

                int Current = int.Parse(FileExists ? CurName.Version.ToString().Replace(".", "") : "0");
                int Latest = int.Parse(LatestVerion.Replace(".", ""));

                if (Current >= Latest)
                {
                    return false;
                }

                string AppDirectory = Directory.GetCurrentDirectory();
                string UpdaterDirectory = AppDirectory + "/updater/";

                if (!Directory.Exists(UpdaterDirectory))
                    Directory.CreateDirectory(UpdaterDirectory);

                GitHubChecker.DownloadProgressChanged += ReportUpdaterDownloadProgress;
                GitHubChecker.DownloadFileCompleted += UpdaterDownloadFinished;
                GitHubChecker.DownloadFileAsync(new Uri("https://raw.githubusercontent.com/LuBuCake/RESPCTool/main/RESPCTool/RESPCTool.Versioning/RESPCTool.Updater/latest.zip"), UpdaterDirectory + "latest.zip");

                return true;
            }
        }

        private void ReportUpdaterDownloadProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            Text = $"Resident Evil - SPC Tool - Downloading Updater {e.ProgressPercentage}%";
        }

        private async void UpdaterDownloadFinished(object sender, AsyncCompletedEventArgs e)
        {
            Text = "Resident Evil - SPC Tool - Extracting Updater";
            await ExtractLatestPackage();
            await CheckForToolUpdate(true);
            SetupControls();
        }

        private async Task ExtractLatestPackage()
        {
            string AppDirectory = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar;
            string ZipPath = AppDirectory + "/updater/latest.zip";

            using (ZipArchive archive = ZipFile.OpenRead(ZipPath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string destinationPath = Path.GetFullPath(Path.Combine(AppDirectory, entry.FullName));

                    if (destinationPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                    {
                        if (!Directory.Exists(destinationPath))
                            Directory.CreateDirectory(destinationPath);

                        continue;
                    }

                    try
                    {
                        await Task.Run(() => entry.ExtractToFile(destinationPath, true));
                    }
                    catch (Exception)
                    {
                        // ignore
                    }
                }
            }

            Directory.Delete(AppDirectory + "/updater/", true);
        }

        private void SetupControls()
        {
            Text = "Resident Evil - SPC Tool";

            ThemeRadioGroup.SelectedIndexChanged += Theme_IndexChanged;
            OpenFileButton.Click += OpenFile_Click;
            SaveFileButton.Click += SaveFile_Click;
            CloseFileButton.Click += CloseFile_Click;
            GitHubButton.Click += OpenLink_Click;
            ForumButton.Click += OpenLink_Click;
            PlayButtonEdit.Click += PlaySound_Click;
            ExtractButtonEdit.Click += ExtractSound_Click;
            ReplaceButtonEdit.Click += ReplaceSound_Click;
            ExtractDataButton.Click += ExtractData_Click;
            ReplaceDataButton.Click += ReplaceData_Click;

            ThemeRadioGroup.Enabled = true;
            OpenFileButton.Enabled = true;
            SaveFileButton.Enabled = true;
            CloseFileButton.Enabled = true;
            GitHubButton.Enabled = true;
            ForumButton.Enabled = true;
            ExtractDataButton.Enabled = true;
            ReplaceDataButton.Enabled = true;
            ExtractDecodedCheckEdit.Enabled = true;
        }

        private void OpenFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog OFD = new OpenFileDialog())
            {
                OFD.Filter = "SPC files (*.spc)|*.spc";
                OFD.Title = "Load SPC";
                OFD.RestoreDirectory = true;

                if (OFD.ShowDialog() == DialogResult.OK)
                {
                    SPACFile = SPACHelper.ReadSPAC(OFD.FileName, OFD.SafeFileName);

                    if (SPACFile == null)
                        return;

                    string SPACNameSafe = OFD.SafeFileName;

                    if (SPACNameSafe.Contains(".spc"))
                        SPACNameSafe = SPACNameSafe.Replace(".spc", "");

                    FileNameTextEdit.Text = "SPC: " + SPACNameSafe;

                    switch (SPACFile.Version)
                    {
                        case (int)SPACVersion.RE5:
                            SPACDataGridControl.DataSource = SPACFile.FWSEFiles;
                            break;
                        case (int)SPACVersion.RE6:
                            SPACDataGridControl.DataSource = SPACFile.XSEWFiles;
                            break;
                    }

                    SPACSoundPlayer = new SoundPlayer();
                }
            }
        }

        private void SaveFile_Click(object sender, EventArgs e)
        {
            if (SPACFile == null)
            {
                XtraMessageBox.Show("There isn't any SPC file loaded.", "Ops!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (SaveFileDialog SFD = new SaveFileDialog())
            {
                SFD.Filter = "SPC files (*.spc)|*.spc";
                SFD.Title = "Save STQ";
                SFD.RestoreDirectory = true;

                if (SFD.ShowDialog() == DialogResult.OK)
                {
                    SPACHelper.WriteSPAC(SFD.FileName, SPACFile);
                }
            }
        }

        private void CloseFile_Click(object sender, EventArgs e)
        {
            if (SPACFile == null)
            {
                XtraMessageBox.Show("There isn't any SPC file loaded.", "Ops!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            SPACFile = null;
            SPACSoundPlayer.Dispose();
            SPACSoundPlayer = null;
            FileNameTextEdit.Text = "No SPC file loaded.";
            SPACDataGridControl.DataSource = null;
        }

        private void OpenLink_Click(object sender, EventArgs e)
        {
            SimpleButton SB = sender as SimpleButton;

            if (XtraMessageBox.Show("This will open up a page on your browser, confirm?", "Info", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
            {
                switch (SB.Name)
                {
                    case "GitHubButton":
                        Process.Start("https://github.com/LuBuCake/RESPCTool");
                        break;
                    case "ForumButton":
                        Process.Start("https://residentevilmodding.boards.net/thread/13992/resident-evil-fwse-spc-tool");
                        break;
                }
            }
        }

        private void Theme_IndexChanged(object sender, EventArgs e)
        {
            string Value = ThemeRadioGroup.EditValue.ToString();
            UserLookAndFeel.Default.SetSkinStyle(UserLookAndFeel.Default.ActiveSkinName, Value);

            string AppDir = Directory.GetCurrentDirectory();
            string ConfigDir = AppDir + "/RESPCTool.exe.config";
            string UpdaterConfigDir = AppDir + "/Updater.exe.config";

            if (File.Exists(ConfigDir))
            {
                string Config = File.ReadAllText(ConfigDir);

                if (Config.Contains("VS Dark") && Value == "VS Light")
                    Config = Config.Replace("VS Dark", "VS Light");
                else if (Config.Contains("VS Light") && Value == "VS Dark")
                    Config = Config.Replace("VS Light", "VS Dark");

                File.WriteAllText(ConfigDir, Config);
            }

            if (File.Exists(UpdaterConfigDir))
            {
                string UpdaterConfig = File.ReadAllText(UpdaterConfigDir);

                if (UpdaterConfig.Contains("VS Dark") && Value == "VS Light")
                    UpdaterConfig = UpdaterConfig.Replace("VS Dark", Value);
                else if (UpdaterConfig.Contains("VS Light") && Value == "VS Dark")
                    UpdaterConfig = UpdaterConfig.Replace("VS Light", Value);

                File.WriteAllText(UpdaterConfigDir, UpdaterConfig);
            }
        }

        private void PlaySound_Click(object sender, EventArgs e)
        {
            if (SPACFile == null)
                return;

            WAVE WAVEFile = null;

            switch (SPACFile.Version)
            {
                case (int)SPACVersion.RE5:
                    FWSE FWSEFile = SPACDataGridView.GetRow(SPACDataGridView.FocusedRowHandle) as FWSE;
                    WAVEFile = FWSEHelper.ConvertToWAVE(FWSEFile);
                    break;
                case (int)SPACVersion.RE6:
                    XSEW XSEWFile = SPACDataGridView.GetRow(SPACDataGridView.FocusedRowHandle) as XSEW;
                    WAVEFile = XSEWHelper.ConvertToWAVE(XSEWFile);
                    break;
            }

            string WAVEFileDir = Directory.GetCurrentDirectory() + "/ToPlay.wav";
            WAVEHelper.WriteWAVE(WAVEFileDir, WAVEFile, false);
            MemoryStream MS = new MemoryStream(File.ReadAllBytes(WAVEFileDir));
            File.Delete(WAVEFileDir);
            SPACSoundPlayer.Stream = MS;
            SPACSoundPlayer.Play();

            SPACDataGridView.CloseEditor();
        }

        private void ExtractSound_Click(object sender, EventArgs e)
        {
            if (SPACFile == null)
                return;

            using (SaveFileDialog SFD = new SaveFileDialog())
            {
                switch (SPACFile.Version)
                {
                    case (int)SPACVersion.RE5:

                        FWSE FWSEFile = SPACDataGridView.GetRow(SPACDataGridView.FocusedRowHandle) as FWSE;

                        SFD.Filter = "FWSE Files (*.fwse)|*.fwse|WAVE Files (*.wav)|*.wav";
                        SFD.Title = "Save FWSE / WAV";
                        SFD.RestoreDirectory = true;

                        if (SFD.ShowDialog() == DialogResult.OK)
                        {
                            switch (SFD.FilterIndex)
                            {
                                case 1:
                                    FWSEHelper.WriteFWSE(SFD.FileName, FWSEFile);
                                    break;
                                case 2:
                                    WAVEHelper.WriteWAVE(SFD.FileName, FWSEHelper.ConvertToWAVE(FWSEFile));
                                    break;
                            }
                        }

                        break;
                    case (int)SPACVersion.RE6:

                        XSEW XSEWFile = SPACDataGridView.GetRow(SPACDataGridView.FocusedRowHandle) as XSEW;

                        SFD.Filter = "XSEW Files (*.xsew)|*.xsew|WAVE Files (*.wav)|*.wav";
                        SFD.Title = "Save XSEW / WAV";
                        SFD.RestoreDirectory = true;

                        if (SFD.ShowDialog() == DialogResult.OK)
                        {
                            switch (SFD.FilterIndex)
                            {
                                case 1:
                                    XSEWHelper.WriteXSEW(SFD.FileName, XSEWFile);
                                    break;
                                case 2:
                                    WAVEHelper.WriteWAVE(SFD.FileName, XSEWHelper.ConvertToWAVE(XSEWFile));
                                    break;
                            }
                        }

                        break;
                }
            }

            SPACDataGridView.CloseEditor();
        }

        private void ReplaceSound_Click(object sender, EventArgs e)
        {
            if (SPACFile == null)
                return;

            int Index;

            using (OpenFileDialog OFD = new OpenFileDialog())
            {
                switch (SPACFile.Version)
                {
                    case (int)SPACVersion.RE5:

                        FWSE FocusedFWSEFile = SPACDataGridView.GetRow(SPACDataGridView.FocusedRowHandle) as FWSE;
                        Index = FocusedFWSEFile.Index;

                        OFD.Filter = "FWSE Files (*.fwse)|*.fwse|WAVE Files (*.wav)|*.wav";
                        OFD.Title = "Select a FWSE or a WAV file";
                        OFD.RestoreDirectory = true;

                        if (OFD.ShowDialog() == DialogResult.OK)
                        {
                            string Format = "";

                            using (FileStream FS = new FileStream(OFD.FileName, FileMode.Open))
                            {
                                using (BinaryReader BR = new BinaryReader(FS))
                                {
                                    if (FS.Length < 4)
                                    {
                                        XtraMessageBox.Show("The file stream is too short to be a FWSE or a WAVE file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        break;
                                    }

                                    for (int i = 0; i < 4; i++)
                                        Format += (char)BR.ReadByte();
                                }
                            }

                            if (Format == "RIFF")
                            {
                                WAVE WAVEFile = WAVEHelper.ReadWAVE(OFD.FileName, OFD.SafeFileName);

                                if (WAVEFile == null)
                                    break;

                                FWSE FWSEFile = FWSEHelper.ConvertToFWSE(WAVEFile, Index);
                                SPACHelper.ReplaceSPACSound(SPACFile, FWSEFile, Index);
                            }
                            else if (Format == "FWSE")
                            {
                                FWSE FWSEFile = FWSEHelper.ReadFWSE(OFD.FileName, OFD.SafeFileName, Index);

                                if (FWSEFile == null)
                                    break;

                                SPACHelper.ReplaceSPACSound(SPACFile, FWSEFile, Index);
                            }
                            else
                            {
                                XtraMessageBox.Show("Invalid file selected, please refer to a valid FWSE or WAVE file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                break;
                            }

                            SPACDataGridControl.RefreshDataSource();
                            XtraMessageBox.Show($"File {Index}.FWSE sucessfully replaced!", "Done!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }

                        break;
                    case (int)SPACVersion.RE6:

                        XSEW FocusedXSEWFile = SPACDataGridView.GetRow(SPACDataGridView.FocusedRowHandle) as XSEW;
                        Index = FocusedXSEWFile.Index;

                        OFD.Filter = "XSEW Files (*.xsew)|*.xsew|WAVE Files (*.wav)|*.wav";
                        OFD.Title = "Select a XSEW or a WAV file";
                        OFD.RestoreDirectory = true;

                        if (OFD.ShowDialog() == DialogResult.OK)
                        {
                            string ChunkID = "";
                            ushort AudioFormat;

                            using (FileStream FS = new FileStream(OFD.FileName, FileMode.Open))
                            {
                                using (BinaryReader BR = new BinaryReader(FS))
                                {
                                    if (FS.Length < 22)
                                    {
                                        XtraMessageBox.Show("The file stream is too short to be a XSEW or a WAVE file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        break;
                                    }

                                    for (int i = 0; i < 4; i++)
                                        ChunkID += (char)BR.ReadByte();

                                    if (ChunkID != "RIFF")
                                    {
                                        XtraMessageBox.Show("Invalid file selected, please refer to a valid XSEW or WAVE file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        break;
                                    }

                                    uint ChunkSize = BR.ReadUInt32();
                                    if (ChunkSize + 8 < FS.Length)
                                    {
                                        XtraMessageBox.Show("The file's total length doesn't match what is registered inside of it.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        break;
                                    }

                                    string Format = "";
                                    for (int i = 0; i < 4; i++)
                                        Format += (char)BR.ReadByte();

                                    string Subchunck1ID = "";
                                    for (int i = 0; i < 4; i++)
                                        Subchunck1ID += (char)BR.ReadByte();

                                    uint Subchunk1Size = BR.ReadUInt32();
                                    AudioFormat = BR.ReadUInt16();

                                    if (Format != "WAVE" || Subchunck1ID != "fmt " || Subchunk1Size != 16 && Subchunk1Size != 50 || AudioFormat != 1 && AudioFormat != 2)
                                    {
                                        XtraMessageBox.Show("Invalid file selected, please refer to a valid XSEW or WAVE file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        break;
                                    }
                                }
                            }

                            if (AudioFormat == 1)
                            {
                                WAVE WAVEFile = WAVEHelper.ReadWAVE(OFD.FileName, OFD.SafeFileName);

                                if (WAVEFile == null)
                                    break;

                                XSEW XSEWFile = XSEWHelper.ConvertToXSEW(WAVEFile, Index);
                                SPACHelper.ReplaceSPACSound(SPACFile, XSEWFile, Index);
                            }
                            else if (AudioFormat == 2)
                            {
                                XSEW XSEWFile = XSEWHelper.ReadXSEW(OFD.FileName, OFD.SafeFileName, Index);

                                if (XSEWFile == null)
                                    break;

                                SPACHelper.ReplaceSPACSound(SPACFile, XSEWFile, Index);
                            }

                            SPACDataGridControl.RefreshDataSource();
                            XtraMessageBox.Show($"File {Index}.xsew sucessfully replaced!", "Done!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }

                        break;
                }
            }

            SPACDataGridView.CloseEditor();
        }

        private void ExtractData_Click(object sender, EventArgs e)
        {
            if (SPACFile == null)
            {
                XtraMessageBox.Show("There isn't any SPC file loaded.", "Ops!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (FolderBrowserDialog FBD = new FolderBrowserDialog())
            {
                if (FBD.ShowDialog() == DialogResult.OK)
                {
                    string BasePath = FBD.SelectedPath + @"\";

                    switch (SPACFile.Version)
                    {
                        case (int)SPACVersion.RE5:
                            if (ExtractDecodedCheckEdit.Checked)
                            {
                                foreach (FWSE FWSEFile in SPACFile.FWSEFiles)
                                    WAVEHelper.WriteWAVE($"{BasePath}{FWSEFile.Index}.wav", FWSEHelper.ConvertToWAVE(FWSEFile), false);

                                XtraMessageBox.Show("WAVE files written sucessfully!", "Done!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            else
                            {
                                foreach (FWSE FWSEFile in SPACFile.FWSEFiles)
                                    FWSEHelper.WriteFWSE($"{BasePath}{FWSEFile.Index}.fwse", FWSEFile, false);

                                XtraMessageBox.Show("FWSE files written sucessfully!", "Done!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            break;
                        case (int)SPACVersion.RE6:
                            if (ExtractDecodedCheckEdit.Checked)
                            {
                                foreach (XSEW XSEWFile in SPACFile.XSEWFiles)
                                    WAVEHelper.WriteWAVE($"{BasePath}{XSEWFile.Index}.wav", XSEWHelper.ConvertToWAVE(XSEWFile), false);

                                XtraMessageBox.Show("WAVE files written sucessfully!", "Done!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            else
                            {
                                foreach (XSEW XSEWFile in SPACFile.XSEWFiles)
                                    XSEWHelper.WriteXSEW($"{BasePath}{XSEWFile.Index}.xsew", XSEWFile, false);

                                XtraMessageBox.Show("XSEW files written sucessfully!", "Done!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            break;
                    }
                }
            }
        }

        private void ReplaceData_Click(object sender, EventArgs e)
        {
            if (SPACFile == null)
            {
                XtraMessageBox.Show("There isn't any SPC file loaded.", "Ops!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            int Replaced = 0;
            int Skipped = 0;

            using (OpenFileDialog OFD = new OpenFileDialog())
            {
                switch (SPACFile.Version)
                {
                    case (int)SPACVersion.RE5:

                        OFD.Filter = "FWSE Files (*.fwse)|*.fwse|WAVE Files (*.wav)|*.wav";
                        OFD.Title = "Select a FWSE or a WAV file";
                        OFD.RestoreDirectory = true;
                        OFD.Multiselect = true;

                        if (OFD.ShowDialog() == DialogResult.OK)
                        {
                            for (int i = 0; i < OFD.FileNames.Length; i++)
                            {
                                string FileNameClear = OFD.SafeFileNames[i].ToLower();

                                switch (OFD.FilterIndex)
                                {
                                    case 1:
                                        FileNameClear = FileNameClear.Replace(".fwse", "");
                                        break;
                                    case 2:
                                        FileNameClear = FileNameClear.Replace(".wav", "");
                                        break;
                                }

                                if (!int.TryParse(FileNameClear, out int Index) || SPACFile.FWSEFiles.Count - 1 < Index)
                                {
                                    XtraMessageBox.Show($"Skipping file {OFD.SafeFileNames[i]}: The file must be named after a valid index.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    Skipped++;
                                    continue;
                                }

                                string Format = "";

                                using (FileStream FS = new FileStream(OFD.FileNames[i], FileMode.Open))
                                {
                                    using (BinaryReader BR = new BinaryReader(FS))
                                    {
                                        if (FS.Length < 4)
                                        {
                                            XtraMessageBox.Show($"Skipping file {OFD.SafeFileNames[i]}: The file stream is too short to be a FWSE or a WAVE file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                            Skipped++;
                                            continue;
                                        }

                                        for (int j = 0; j < 4; j++)
                                            Format += (char) BR.ReadByte();
                                    }
                                }

                                if (Format == "RIFF")
                                {
                                    WAVE WAVEFile = WAVEHelper.ReadWAVE(OFD.FileNames[i], OFD.SafeFileNames[i]);

                                    if (WAVEFile == null)
                                    {
                                        Skipped++;
                                        continue;
                                    }

                                    FWSE FWSEFile = FWSEHelper.ConvertToFWSE(WAVEFile, Index);
                                    SPACHelper.ReplaceSPACSound(SPACFile, FWSEFile, Index);
                                    Replaced++;
                                }
                                else if (Format == "FWSE")
                                {
                                    FWSE FWSEFile = FWSEHelper.ReadFWSE(OFD.FileNames[i], OFD.SafeFileNames[i], Index);

                                    if (FWSEFile == null)
                                    {
                                        Skipped++;
                                        continue;
                                    }

                                    SPACHelper.ReplaceSPACSound(SPACFile, FWSEFile, Index);
                                    Replaced++;
                                }
                                else
                                {
                                    XtraMessageBox.Show($"Skipping file {OFD.SafeFileNames[i]}: Incorrect file format selected, please refer to a valid FWSE or WAVE file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    Skipped++;
                                }
                            }

                            SPACDataGridControl.RefreshDataSource();
                            XtraMessageBox.Show($"Task completed with a total of {Skipped} files skipped and a total of {Replaced} files replaced.", "Done!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }

                        break;

                    case (int)SPACVersion.RE6:

                        OFD.Filter = "XSEW Files (*.xsew)|*.xsew|WAVE Files (*.wav)|*.wav";
                        OFD.Title = "Select a XSEW or a WAV file";
                        OFD.RestoreDirectory = true;
                        OFD.Multiselect = true;

                        if (OFD.ShowDialog() == DialogResult.OK)
                        {
                            for (int i = 0; i < OFD.FileNames.Length; i++)
                            {
                                string FileNameClear = OFD.SafeFileNames[i].ToLower();

                                switch (OFD.FilterIndex)
                                {
                                    case 1:
                                        FileNameClear = FileNameClear.Replace(".xsew", "");
                                        break;
                                    case 2:
                                        FileNameClear = FileNameClear.Replace(".wav", "");
                                        break;
                                }

                                if (!int.TryParse(FileNameClear, out int Index) || SPACFile.XSEWFiles.Count - 1 < Index)
                                {
                                    XtraMessageBox.Show($"Skipping file {OFD.SafeFileNames[i]}: The file must be named after a valid index.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    Skipped++;
                                    continue;
                                }

                                string ChunkID = "";
                                ushort AudioFormat;

                                using (FileStream FS = new FileStream(OFD.FileNames[i], FileMode.Open))
                                {
                                    using (BinaryReader BR = new BinaryReader(FS))
                                    {
                                        if (FS.Length < 22)
                                        {
                                            XtraMessageBox.Show($"Skipping file {OFD.SafeFileNames[i]}: The file stream is too short to be a XSEW or a WAVE file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                            Skipped++;
                                            continue;
                                        }

                                        for (int j = 0; j < 4; j++)
                                            ChunkID += (char)BR.ReadByte();

                                        if (ChunkID != "RIFF")
                                        {
                                            XtraMessageBox.Show($"Skipping file {OFD.SafeFileNames[i]}: Incorrect file format selected, please refer to a valid XSEW or WAVE file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                            Skipped++;
                                        }

                                        uint ChunkSize = BR.ReadUInt32();
                                        if (ChunkSize + 8 < FS.Length)
                                        {
                                            XtraMessageBox.Show($"Skipping file {OFD.SafeFileNames[i]}: It's total length doesn't match what is registered inside of it.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                            Skipped++;
                                            continue;
                                        }

                                        string Format = "";
                                        for (int j = 0; j < 4; j++)
                                            Format += (char)BR.ReadByte();

                                        string Subchunck1ID = "";
                                        for (int j = 0; j < 4; j++)
                                            Subchunck1ID += (char)BR.ReadByte();

                                        uint Subchunk1Size = BR.ReadUInt32();
                                        AudioFormat = BR.ReadUInt16();

                                        if (Format != "WAVE" || Subchunck1ID != "fmt " || Subchunk1Size != 16 && Subchunk1Size != 50 || AudioFormat != 1 && AudioFormat != 2)
                                        {
                                            XtraMessageBox.Show($"Skipping file {OFD.SafeFileNames[i]}: Incorrect file format selected, please refer to a valid XSEW or WAVE file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                            Skipped++;
                                            continue;
                                        }
                                    }
                                }

                                if (AudioFormat == 1)
                                {
                                    WAVE WAVEFile = WAVEHelper.ReadWAVE(OFD.FileNames[i], OFD.SafeFileNames[i]);

                                    if (WAVEFile == null)
                                    {
                                        Skipped++;
                                        continue;
                                    }

                                    XSEW XSEWFile = XSEWHelper.ConvertToXSEW(WAVEFile, Index);
                                    SPACHelper.ReplaceSPACSound(SPACFile, XSEWFile, Index);
                                    Replaced++;
                                }
                                else if (AudioFormat == 2)
                                {
                                    XSEW XSEWFile = XSEWHelper.ReadXSEW(OFD.FileNames[i], OFD.SafeFileNames[i], Index);

                                    if (XSEWFile == null)
                                    {
                                        Skipped++;
                                        continue;
                                    }

                                    SPACHelper.ReplaceSPACSound(SPACFile, XSEWFile, Index);
                                    Replaced++;
                                }
                            }

                            SPACDataGridControl.RefreshDataSource();
                            XtraMessageBox.Show($"Task completed with a total of {Skipped} files skipped and a total of {Replaced} files replaced.", "Done!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }

                        break;
                }
            }
        }
    }
}
