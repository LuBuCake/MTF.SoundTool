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

using DevExpress.LookAndFeel;
using DevExpress.Security;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Base;
using MTF.SoundTool.Base.Helpers;
using MTF.SoundTool.Base.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Media;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MTF.SoundTool
{
    public partial class App : XtraForm
    {
        private SoundPlayer AppSoundPlayer { get; set; }
        private SPAC SPACFile { get; set; }
        private STRQ STRQFile { get; set; }
        private List<SCGI> ToConvertFiles { get; set; }

        public App() => InitializeComponent();

        private async void App_Load(object sender, EventArgs e)
        {
            string AppDir = Directory.GetCurrentDirectory();
            string ConfigDir = AppDir + "/MTFSoundTool.exe.config";
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

            FormBorderStyle = FormBorderStyle.FixedSingle;

            ThemeRadioGroup.Enabled = false;
            GitHubButton.Enabled = false;
            ForumButton.Enabled = false;

            SPACFileNameTextEdit.Text = "No SPC file loaded.";
            OpenSPACFileButton.Enabled = false;
            SaveSPACFileButton.Enabled = false;
            CloseSPACFileButton.Enabled = false;
            ExtractSPACDataButton.Enabled = false;
            ReplaceSPACDataButton.Enabled = false;
            ExtractSPACDecodedCheckEdit.Enabled = false;

            STRQFileNameTextEdit.Text = "No STQ file loaded.";
            OpenSTRQFileButton.Enabled = false;
            SaveSTRQFileButton.Enabled = false;
            CloseSTRQFileButton.Enabled = false;

            SoundConversionTextEdit.Text = "No sound file loaded.";
            OpenFilesButton.Enabled = false;
            ClearFilesButton.Enabled = false;
            SaveFilesButton.Enabled = false;
            ConversionTypeComboBox.Enabled = false;

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

            Text = "MT Framework - Sound Tool - Checking Tool Version";

            using (WebClient GitHubChecker = new WebClient())
            {
                try
                {
                    string LatestVerion = await Task.Run(() => GitHubChecker.DownloadString("https://raw.githubusercontent.com/LuBuCake/MTF.SoundTool/main/MTF.SoundTool/MTF.SoundTool.Versioning/MTF.SoundTool/latest.txt"));

                    Assembly CurApp = Assembly.GetExecutingAssembly();
                    AssemblyName CurName = new AssemblyName(CurApp.FullName);

                    int Current = int.Parse(CurName.Version.ToString().Replace(".", ""));
                    int Latest = int.Parse(LatestVerion.Replace(".", ""));

                    if (Current >= Latest)
                        return false;

                    if (XtraMessageBox.Show("A new version is available. Would you like to update it now?", "Info", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        string AppDirectory = Directory.GetCurrentDirectory();
                        string UpdaterDirectory = AppDirectory + "/updater/";

                        if (!Directory.Exists(UpdaterDirectory))
                            Directory.CreateDirectory(UpdaterDirectory);

                        AppVersion _version = new AppVersion()
                        {
                            FileRoute = "https://raw.githubusercontent.com/LuBuCake/MTF.SoundTool/main/MTF.SoundTool/MTF.SoundTool.Versioning/MTF.SoundTool/latest.zip",
                        };

                        Serializer.WriteDataFile(UpdaterDirectory + "updateapp.json", Serializer.SerializeAppVersion(_version));

                        Process.Start(AppDirectory + "/Updater.exe");
                        Application.Exit();

                        return true;
                    }

                    return false;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        private async Task<bool> CheckForUpdaterUpdate()
        {
            bool HasConnection = await Task.Run(() => Utility.TestConnection("8.8.8.8"));

            if (!HasConnection)
            {
                return false;
            }

            Text = "MT Framework - Sound Tool - Checking Updater Version";

            using (WebClient GitHubChecker = new WebClient())
            {
                try
                {
                    string LatestVerion = await Task.Run(() => GitHubChecker.DownloadString("https://raw.githubusercontent.com/LuBuCake/MTF.SoundTool/main/MTF.SoundTool/MTF.SoundTool.Versioning/MTF.SoundTool.Updater/latest.txt"));
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
                        return false;

                    string AppDirectory = Directory.GetCurrentDirectory();
                    string UpdaterDirectory = AppDirectory + "/updater/";

                    if (!Directory.Exists(UpdaterDirectory))
                        Directory.CreateDirectory(UpdaterDirectory);

                    GitHubChecker.DownloadProgressChanged += ReportUpdaterDownloadProgress;
                    GitHubChecker.DownloadFileCompleted += UpdaterDownloadFinished;
                    GitHubChecker.DownloadFileAsync(new Uri("https://raw.githubusercontent.com/LuBuCake/MTF.SoundTool/main/MTF.SoundTool/MTF.SoundTool.Versioning/MTF.SoundTool.Updater/latest.zip"), UpdaterDirectory + "latest.zip");

                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        private void ReportUpdaterDownloadProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            Text = $"MT Framework - Sound Tool - Downloading Updater {e.ProgressPercentage}%";
        }

        private async void UpdaterDownloadFinished(object sender, AsyncCompletedEventArgs e)
        {
            Text = "MT Framework - Sound Tool - Extracting Updater";
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
            Text = "MT Framework - Sound Tool";

            MainTabControl.SelectedPageChanged += ChangePageControl;
            SizeChanged += Control_SizeChanged;
            ThemeRadioGroup.SelectedIndexChanged += Theme_IndexChanged;
            GitHubButton.Click += OpenLink_Click;
            ForumButton.Click += OpenLink_Click;

            OpenSPACFileButton.Click += OpenSPACFile_Click;
            SaveSPACFileButton.Click += SaveSPACFile_Click;
            CloseSPACFileButton.Click += CloseSPACFile_Click;
            PlayButtonEdit.Click += PlaySPACSound_Click;
            ExtractButtonEdit.Click += ExtractSPACSound_Click;
            ReplaceButtonEdit.Click += ReplaceSPACSound_Click;
            ExtractSPACDataButton.Click += ExtractSPACData_Click;
            ReplaceSPACDataButton.Click += ReplaceSPACData_Click;

            OpenSTRQFileButton.Click += OpenSTRQFile_Click;
            SaveSTRQFileButton.Click += SaveSTRQFile_Click;
            CloseSTRQFileButton.Click += CloseSTRQFile_Click;
            STRQGridView.CellValueChanged += STRQGrid_CellValueChanged;

            OpenFilesButton.Click += LoadFiles_Click;
            ClearFilesButton.Click += ClearFiles_Click;
            SaveFilesButton.Click += SaveFiles_Click;
            PlaySoundFileButton.Click += PlaySoundFile_Click;
            RemoveSoundFileButton.Click += RemoveSoundFile_Click;
            ConversionTypeComboBox.SelectedIndexChanged += ConversionType_IndexChanged;

            ConversionTypeComboBox.Properties.Items.Add(new ListItem("Save as: WAVE", "WAVE"));
            ConversionTypeComboBox.Properties.Items.Add(new ListItem("Save as: FWSE", "FWSE"));
            ConversionTypeComboBox.Properties.Items.Add(new ListItem("Save as: XSEW", "XSEW"));
            ConversionTypeComboBox.Properties.Items.Add(new ListItem("Save as: MADP", "MCA"));
            ConversionTypeComboBox.SelectedIndex = 0;

            new GridSelector(STRQGridView);

            FormBorderStyle = FormBorderStyle.Sizable;

            ThemeRadioGroup.Enabled = true;
            GitHubButton.Enabled = true;
            ForumButton.Enabled = true;

            OpenSPACFileButton.Enabled = true;
            SaveSPACFileButton.Enabled = true;
            CloseSPACFileButton.Enabled = true;
            ExtractSPACDataButton.Enabled = true;
            ReplaceSPACDataButton.Enabled = true;
            ExtractSPACDecodedCheckEdit.Enabled = true;

            OpenSTRQFileButton.Enabled = true;
            SaveSTRQFileButton.Enabled = true;
            CloseSTRQFileButton.Enabled = true;

            OpenFilesButton.Enabled = true;
            ClearFilesButton.Enabled = true;
            SaveFilesButton.Enabled = true;
            ConversionTypeComboBox.Enabled = true;

            AppSoundPlayer = new SoundPlayer();

            ChangePageControl(null, null);
        }

        private void ChangePageControl(object sender, EventArgs e)
        {
            string SelectedTabName = MainTabControl.SelectedTabPage.Name;

            switch (SelectedTabName)
            {
                case "SPACEditorTab":
                    SPACFileNameTextEdit.Visible = true;
                    OpenSPACFileButton.Visible = true;
                    SaveSPACFileButton.Visible = true;
                    CloseSPACFileButton.Visible = true;
                    ExtractSPACDataButton.Visible = true;
                    ExtractSPACDecodedCheckEdit.Visible = true;
                    ReplaceSPACDataButton.Visible = true;

                    STRQFileNameTextEdit.Visible = false;
                    OpenSTRQFileButton.Visible = false;
                    SaveSTRQFileButton.Visible = false;
                    CloseSTRQFileButton.Visible = false;

                    SoundConversionTextEdit.Visible = false;
                    OpenFilesButton.Visible = false;
                    ClearFilesButton.Visible = false;
                    SaveFilesButton.Visible = false;
                    ConversionTypeComboBox.Visible = false;
                    break;
                case "STQREditorTab":
                    SPACFileNameTextEdit.Visible = false;
                    OpenSPACFileButton.Visible = false;
                    SaveSPACFileButton.Visible = false;
                    CloseSPACFileButton.Visible = false;
                    ExtractSPACDataButton.Visible = false;
                    ExtractSPACDecodedCheckEdit.Visible = false;
                    ReplaceSPACDataButton.Visible = false;

                    STRQFileNameTextEdit.Visible = true;
                    OpenSTRQFileButton.Visible = true;
                    SaveSTRQFileButton.Visible = true;
                    CloseSTRQFileButton.Visible = true;

                    SoundConversionTextEdit.Visible = false;
                    OpenFilesButton.Visible = false;
                    ClearFilesButton.Visible = false;
                    SaveFilesButton.Visible = false;
                    ConversionTypeComboBox.Visible = false;
                    break;
                case "SoundEditorTab":
                    SPACFileNameTextEdit.Visible = false;
                    OpenSPACFileButton.Visible = false;
                    SaveSPACFileButton.Visible = false;
                    CloseSPACFileButton.Visible = false;
                    ExtractSPACDataButton.Visible = false;
                    ExtractSPACDecodedCheckEdit.Visible = false;
                    ReplaceSPACDataButton.Visible = false;

                    STRQFileNameTextEdit.Visible = false;
                    OpenSTRQFileButton.Visible = false;
                    SaveSTRQFileButton.Visible = false;
                    CloseSTRQFileButton.Visible = false;

                    SoundConversionTextEdit.Visible = true;
                    OpenFilesButton.Visible = true;
                    ClearFilesButton.Visible = true;
                    SaveFilesButton.Visible = true;
                    ConversionTypeComboBox.Visible = true;
                    break;
            }
        }

        private void OpenLink_Click(object sender, EventArgs e)
        {
            SimpleButton SB = sender as SimpleButton;

            if (XtraMessageBox.Show("This will open up a page on your browser, confirm?", "Info", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
            {
                switch (SB.Name)
                {
                    case "GitHubButton":
                        Process.Start("https://github.com/LuBuCake/MTF.SoundTool");
                        break;
                    case "ForumButton":
                        Process.Start("https://residentevilmodding.boards.net/thread/15557/mt-framework-sound-tool");
                        break;
                }
            }
        }

        private void Control_SizeChanged(object sender, EventArgs e)
        {
            int AdditiveHeight = Height - MinimumSize.Height;
            MainTabControl.Height = MainTabControl.MinimumSize.Height + AdditiveHeight;
            STRQDataGP.Height = STRQDataGP.MinimumSize.Height + AdditiveHeight;
            STRQDataGridControl.Height = STRQDataGridControl.MinimumSize.Height + AdditiveHeight;
            SPACDataGP.Height = SPACDataGP.MinimumSize.Height + AdditiveHeight;
            SPACDataGridControl.Height = SPACDataGridControl.MinimumSize.Height + AdditiveHeight;
            SoundDataGP.Height = SoundDataGP.MinimumSize.Height + AdditiveHeight;
            SoundDataGridControl.Height = SoundDataGridControl.MinimumSize.Height + AdditiveHeight;
        }

        private void Theme_IndexChanged(object sender, EventArgs e)
        {
            string Value = ThemeRadioGroup.EditValue.ToString();
            UserLookAndFeel.Default.SetSkinStyle(UserLookAndFeel.Default.ActiveSkinName, Value);

            string AppDir = Directory.GetCurrentDirectory();
            string ConfigDir = AppDir + "/MTFSoundTool.exe.config";
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

        private void OpenSPACFile_Click(object sender, EventArgs e)
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

                    string SPACNameSafe = OFD.SafeFileName.ToLower();

                    if (SPACNameSafe.Contains(".spc"))
                        SPACNameSafe = SPACNameSafe.Replace(".spc", "");

                    SPACFileNameTextEdit.Text = "SPC: " + SPACNameSafe;

                    switch (SPACFile.Version)
                    {
                        case (int)SPACVersion.LostPlanet:
                        case (int)SPACVersion.RE5:
                            SPACDataGridControl.DataSource = SPACFile.FWSEFiles;
                            break;
                        case (int)SPACVersion.RE6:
                            SPACDataGridControl.DataSource = SPACFile.XSEWFiles;
                            break;
                    }
                }
            }
        }

        private void SaveSPACFile_Click(object sender, EventArgs e)
        {
            if (SPACFile == null)
            {
                XtraMessageBox.Show("There isn't any SPC file loaded.", "Ops!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (SaveFileDialog SFD = new SaveFileDialog())
            {
                SFD.Filter = "SPC files (*.spc)|*.spc";
                SFD.Title = "Save SPC";
                SFD.RestoreDirectory = true;

                if (SFD.ShowDialog() == DialogResult.OK)
                {
                    SPACHelper.WriteSPAC(SFD.FileName, SPACFile);
                }
            }
        }

        private void CloseSPACFile_Click(object sender, EventArgs e)
        {
            if (SPACFile == null)
            {
                XtraMessageBox.Show("There isn't any SPC file loaded.", "Ops!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            SPACFile = null;
            SPACFileNameTextEdit.Text = "No SPC file loaded.";
            SPACDataGridControl.DataSource = null;
        }

        private void OpenSTRQFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog OFD = new OpenFileDialog())
            {
                OFD.Filter = "STQ files (*.stq *.stqr)|*.stq;*.stqr";
                OFD.Title = "Load STQ";

                if (OFD.ShowDialog() == DialogResult.OK)
                {
                    STRQFile = STRQHelper.ReadSTRQ(OFD.FileName, OFD.SafeFileName);

                    if (STRQFile == null)
                        return;

                    string STQNameSafe = OFD.SafeFileName.ToLower();

                    if (STQNameSafe.Contains(".stqr"))
                        STQNameSafe = STQNameSafe.Replace(".stqr", "");

                    if (STQNameSafe.Contains(".stq"))
                        STQNameSafe = STQNameSafe.Replace(".stq", "");

                    STRQFileNameTextEdit.Text = "STQ: " + STQNameSafe;

                    STRQDataGridControl.DataSource = STRQFile.STRQEntries;
                }
            }
        }

        private void SaveSTRQFile_Click(object sender, EventArgs e)
        {
            if (STRQFile == null)
            {
                XtraMessageBox.Show("There isn't any STQ file loaded.", "Ops!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (SaveFileDialog SFD = new SaveFileDialog())
            {
                if (STRQFile.Version == (int)STRQVersion.REV1 || STRQFile.Version == (int)STRQVersion.REV2)
                    SFD.Filter = "STQR files (*.stqr)|*.stqr";
                else
                    SFD.Filter = "STQ files (*.stq)|*.stq";

                SFD.Title = "Save STQ";
                SFD.RestoreDirectory = true;

                if (SFD.ShowDialog() == DialogResult.OK)
                {
                    STRQHelper.WriteSTRQ(SFD.FileName, STRQFile);
                }
            }
        }

        private void CloseSTRQFile_Click(object sender, EventArgs e)
        {
            if (STRQFile == null)
            {
                XtraMessageBox.Show("There isn't any STQ file loaded.", "Ops!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            STRQFile = null;
            STRQFileNameTextEdit.Text = "No STQ file loaded.";
            STRQDataGridControl.DataSource = null;
        }

        private void STRQGrid_CellValueChanged(object sender, CellValueChangedEventArgs e)
        {
            if (STRQFile == null)
                return;

            STRQHelper.UpdateEntries(STRQFile);
            STRQDataGridControl.RefreshDataSource();
        }

        private void PlaySPACSound_Click(object sender, EventArgs e)
        {
            if (SPACFile == null)
                return;

            WAVE WAVEFile = null;

            switch (SPACFile.Version)
            {
                case (int)SPACVersion.LostPlanet:
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
            AppSoundPlayer.Stream = MS;
            AppSoundPlayer.Play();

            SPACDataGridView.CloseEditor();
        }

        private void ExtractSPACSound_Click(object sender, EventArgs e)
        {
            if (SPACFile == null)
                return;

            using (SaveFileDialog SFD = new SaveFileDialog())
            {
                switch (SPACFile.Version)
                {
                    case (int)SPACVersion.LostPlanet:
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

        private void ReplaceSPACSound_Click(object sender, EventArgs e)
        {
            if (SPACFile == null)
                return;

            int Index;

            using (OpenFileDialog OFD = new OpenFileDialog())
            {
                switch (SPACFile.Version)
                {
                    case (int)SPACVersion.LostPlanet:
                    case (int)SPACVersion.RE5:

                        FWSE FocusedFWSEFile = SPACDataGridView.GetRow(SPACDataGridView.FocusedRowHandle) as FWSE;
                        Index = FocusedFWSEFile.Index;

                        OFD.Filter = "All Files|*.*|FWSE Files (*.fwse)|*.fwse|WAVE Files (*.wav)|*.wav";
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

                                if (WAVEFile.NumChannels != 1)
                                {
                                    XtraMessageBox.Show("The selected WAVE file must be mono (1 channel).", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    break;
                                }

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

                        OFD.Filter = "All Files|*.*|XSEW Files (*.xsew)|*.xsew|WAVE Files (*.wav)|*.wav";
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
                                    if (ChunkSize + 8 > FS.Length)
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

                                if (WAVEFile.NumChannels != 1)
                                {
                                    XtraMessageBox.Show("The selected WAVE file must be mono (1 channel).", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    break;
                                }

                                XSEW XSEWFile = XSEWHelper.ConvertToXSEW(WAVEFile, Index);
                                SPACHelper.ReplaceSPACSound(SPACFile, XSEWFile, Index);
                            }
                            else if (AudioFormat == 2)
                            {
                                XSEW XSEWFile = XSEWHelper.ReadXSEW(OFD.FileName, OFD.SafeFileName, Index);

                                if (XSEWFile == null)
                                    break;

                                if (XSEWFile.NumChannels != 1)
                                {
                                    XtraMessageBox.Show("The selected XSEW file must be mono (1 channel).", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    break;
                                }

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

        private void ExtractSPACData_Click(object sender, EventArgs e)
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
                        case (int)SPACVersion.LostPlanet:
                        case (int)SPACVersion.RE5:
                            if (ExtractSPACDecodedCheckEdit.Checked)
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
                            if (ExtractSPACDecodedCheckEdit.Checked)
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

        private void ReplaceSPACData_Click(object sender, EventArgs e)
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
                    case (int)SPACVersion.LostPlanet:
                    case (int)SPACVersion.RE5:

                        OFD.Filter = "FWSE Files (*.fwse)|*.fwse|WAVE Files (*.wav)|*.wav";
                        OFD.Title = "Select a FWSE or a WAVE file";
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

                                    if (WAVEFile == null || WAVEFile.NumChannels != 1)
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
                        OFD.Title = "Select a XSEW or a WAVE file";
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

                                    if (WAVEFile == null || WAVEFile.NumChannels != 1)
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

        private void LoadFiles_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog OFD = new OpenFileDialog())
            {
                OFD.Filter = "FWSE Files (*.fwse)|*.fwse|XSEW Files (*.xsew)|*.xsew|MADP Files (*.mca)|*.mca|WAVE Files (*.wav)|*.wav";
                OFD.Title = "Select one or more FWSE/XSEW/MADP/WAVE file";
                OFD.RestoreDirectory = true;
                OFD.Multiselect = true;

                if (OFD.ShowDialog() == DialogResult.OK)
                {
                    List<ListItem> ConversionTypes = new List<ListItem>();
                    SCGI SCGIFile;

                    ToConvertFiles = new List<SCGI>();

                    for (int i = 0; i < OFD.FileNames.Length; i++)
                    {
                        switch (OFD.FilterIndex)
                        {
                            case 1:
                                FWSE FWSEFile = FWSEHelper.ReadFWSE(OFD.FileNames[i], OFD.SafeFileNames[i], i);

                                if (FWSEFile == null)
                                    continue;

                                SCGIFile = new SCGI();
                                SCGIFile.SoundFile = FWSEFile;
                                SCGIFile.Format = "FWSE";
                                SCGIFile.FileName = Path.GetFileNameWithoutExtension(OFD.FileNames[i]);
                                SCGIFile.DurationSpan = FWSEFile.DurationSpan;
                                SCGIFile.BitsPerSample = FWSEFile.BitsPerSample;
                                SCGIFile.NumChannels = FWSEFile.NumChannels;
                                SCGIFile.Samples = FWSEFile.Samples;
                                SCGIFile.SampleRate = FWSEFile.SampleRate;
                                ToConvertFiles.Add(SCGIFile);

                                if (ConversionTypes.Count == 0)
                                {
                                    ConversionTypes.Add(new ListItem("Save as: WAVE", "WAVE"));
                                    ConversionTypes.Add(new ListItem("Save as: XSEW", "XSEW"));
                                    ConversionTypes.Add(new ListItem("Save as: MADP", "MCA"));
                                }

                                break;
                            case 2:
                                XSEW XSEWFile = XSEWHelper.ReadXSEW(OFD.FileNames[i], OFD.SafeFileNames[i], i);

                                if (XSEWFile == null)
                                    continue;

                                SCGIFile = new SCGI();
                                SCGIFile.SoundFile = XSEWFile;
                                SCGIFile.Format = "XSEW";
                                SCGIFile.FileName = Path.GetFileNameWithoutExtension(OFD.FileNames[i]);
                                SCGIFile.DurationSpan = XSEWFile.DurationSpan;
                                SCGIFile.BitsPerSample = XSEWFile.BitsPerSample;
                                SCGIFile.NumChannels = XSEWFile.NumChannels;
                                SCGIFile.Samples = XSEWFile.Samples;
                                SCGIFile.SampleRate = (int)XSEWFile.SampleRate;
                                ToConvertFiles.Add(SCGIFile);

                                if (ConversionTypes.Count == 0)
                                {
                                    ConversionTypes.Add(new ListItem("Save as: WAVE", "WAVE"));
                                    ConversionTypes.Add(new ListItem("Save as: FWSE", "FWSE"));
                                    ConversionTypes.Add(new ListItem("Save as: MADP", "MCA"));
                                }

                                break;
                            case 3:
                                MCA3DS MCAFile = MCA3DSHelper.ReadMCA(OFD.FileNames[i], OFD.SafeFileNames[i]);

                                if (MCAFile == null)
                                    continue;

                                SCGIFile = new SCGI();
                                SCGIFile.SoundFile = MCAFile;
                                SCGIFile.Format = "MADP";
                                SCGIFile.FileName = Path.GetFileNameWithoutExtension(OFD.FileNames[i]);
                                SCGIFile.DurationSpan = MCAFile.DurationSpan;
                                SCGIFile.BitsPerSample = 16;
                                SCGIFile.NumChannels = MCAFile.NumChannels;
                                SCGIFile.Samples = MCAFile.Samples;
                                SCGIFile.SampleRate = (int)MCAFile.SampleRate;
                                ToConvertFiles.Add(SCGIFile);

                                if (ConversionTypes.Count == 0)
                                {
                                    ConversionTypes.Add(new ListItem("Save as: WAVE", "WAVE"));
                                    ConversionTypes.Add(new ListItem("Save as: FWSE", "FWSE"));
                                    ConversionTypes.Add(new ListItem("Save as: XSEW", "XSEW"));
                                }

                                break;
                            case 4:
                                WAVE WAVEFile = WAVEHelper.ReadWAVE(OFD.FileNames[i], OFD.SafeFileNames[i]);

                                if (WAVEFile == null)
                                    continue;

                                SCGIFile = new SCGI();
                                SCGIFile.SoundFile = WAVEFile;
                                SCGIFile.Format = "WAVE";
                                SCGIFile.FileName = Path.GetFileNameWithoutExtension(OFD.FileNames[i]);
                                SCGIFile.DurationSpan = TimeSpan.FromSeconds((double)WAVEFile.Subchunk2Size * 2 / WAVEFile.SampleRate);
                                SCGIFile.BitsPerSample = WAVEFile.BitsPerSample;
                                SCGIFile.NumChannels = WAVEFile.NumChannels;
                                SCGIFile.Samples = (int)((int)WAVEFile.Subchunk2Size * 2 / WAVEFile.SampleRate);
                                SCGIFile.SampleRate = (int)WAVEFile.SampleRate;
                                ToConvertFiles.Add(SCGIFile);

                                if (ConversionTypes.Count == 0)
                                {
                                    ConversionTypes.Add(new ListItem("Save as: FWSE", "FWSE"));
                                    ConversionTypes.Add(new ListItem("Save as: XSEW", "XSEW"));
                                    ConversionTypes.Add(new ListItem("Save as: MADP", "MCA"));
                                }

                                break;
                        }
                    }

                    if (ToConvertFiles.Count == 0)
                    {
                        ClearFiles_Click(null, null);
                        return;
                    }

                    ConversionTypeComboBox.Properties.Items.Clear();
                    ConversionTypeComboBox.Properties.Items.AddRange(ConversionTypes);
                    ConversionTypeComboBox.SelectedIndex = 0;

                    SoundDataGridControl.DataSource = ToConvertFiles;
                    ConversionType_IndexChanged(null, null);
                }
            }
        }

        private void ClearFiles_Click(object sender, EventArgs e)
        {
            if (ToConvertFiles == null)
            {
                XtraMessageBox.Show("There isn't any sound file loaded.", "Ops!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            ToConvertFiles = null;
            SoundConversionTextEdit.Text = "No sound file loaded.";
            SoundDataGridControl.DataSource = null;

            ConversionTypeComboBox.Properties.Items.Clear();
            ConversionTypeComboBox.Properties.Items.Add(new ListItem("Save as: WAVE", "WAVE"));
            ConversionTypeComboBox.Properties.Items.Add(new ListItem("Save as: FWSE", "FWSE"));
            ConversionTypeComboBox.Properties.Items.Add(new ListItem("Save as: XSEW", "XSEW"));
            ConversionTypeComboBox.Properties.Items.Add(new ListItem("Save as: MADP", "MCA"));
            ConversionTypeComboBox.SelectedIndex = 0;
        }

        private void SaveFiles_Click(object sender, EventArgs e)
        {
            if (ToConvertFiles == null)
            {
                XtraMessageBox.Show("There isn't any sound file loaded.", "Ops!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var FilesToIgnore = new List<SCGI>();

            string ConversionType = (ConversionTypeComboBox.SelectedItem as ListItem).SValue;

            switch (ConversionType)
            {
                case "FWSE":
                case "XSEW":
                    if (ToConvertFiles.Any(x => x.NumChannels != 1))
                    {
                        XtraMessageBox.Show("There are files in the list that contains more than 1 channel (Stereo), those files will be skipped. Stereo conversion is only supported in the (MADP->WAVE / WAVE->MADP) flow.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        FilesToIgnore = ToConvertFiles.Where(x => x.NumChannels != 1).ToList();
                    }

                    break;
            }

            using (FolderBrowserDialog FBD = new FolderBrowserDialog())
            {
                if (FBD.ShowDialog() == DialogResult.OK)
                {
                    string BasePath = FBD.SelectedPath + @"\";

                    foreach (SCGI SCGIFile in ToConvertFiles)
                    {
                        if (FilesToIgnore.Contains(SCGIFile))
                            continue;

                        switch (ConversionType)
                        {
                            case "WAVE":

                                WAVE WAVEFile;

                                switch (SCGIFile.Format)
                                {
                                    case "FWSE":
                                        WAVEFile = FWSEHelper.ConvertToWAVE((FWSE) SCGIFile.SoundFile);
                                        break;
                                    case "XSEW":
                                        WAVEFile = XSEWHelper.ConvertToWAVE((XSEW) SCGIFile.SoundFile);
                                        break;
                                    case "MADP":
                                        WAVEFile = MCA3DSHelper.ConvertToWAVE((MCA3DS) SCGIFile.SoundFile);
                                        break;
                                    default:
                                        continue;
                                }

                                WAVEHelper.WriteWAVE($"{BasePath}{SCGIFile.FileName}.wav", WAVEFile, false);

                                break;
                            case "FWSE":

                                FWSE FWSEFile;

                                switch (SCGIFile.Format)
                                {
                                    case "WAVE":
                                        FWSEFile = FWSEHelper.ConvertToFWSE((WAVE) SCGIFile.SoundFile);
                                        break;
                                    case "XSEW":
                                        FWSEFile = FWSEHelper.ConvertToFWSE(XSEWHelper.ConvertToWAVE((XSEW) SCGIFile.SoundFile));
                                        break;
                                    case "MADP":
                                        FWSEFile = FWSEHelper.ConvertToFWSE(MCA3DSHelper.ConvertToWAVE((MCA3DS)SCGIFile.SoundFile));
                                        break;
                                    default:
                                        continue;
                                }

                                FWSEHelper.WriteFWSE($"{BasePath}{SCGIFile.FileName}.fwse", FWSEFile, false);

                                break;
                            case "XSEW":

                                XSEW XSEWFile;

                                switch (SCGIFile.Format)
                                {
                                    case "WAVE":
                                        XSEWFile = XSEWHelper.ConvertToXSEW((WAVE) SCGIFile.SoundFile);
                                        break;
                                    case "FWSE":
                                        XSEWFile = XSEWHelper.ConvertToXSEW(FWSEHelper.ConvertToWAVE((FWSE) SCGIFile.SoundFile));
                                        break;
                                    case "MADP":
                                        XSEWFile = XSEWHelper.ConvertToXSEW(MCA3DSHelper.ConvertToWAVE((MCA3DS) SCGIFile.SoundFile));
                                        break;
                                    default:
                                        continue;
                                }

                                XSEWHelper.WriteXSEW($"{BasePath}{SCGIFile.FileName}.xsew", XSEWFile, false);

                                break;
                            case "MCA":
                                MCA3DS MCAFile;
    
                                switch (SCGIFile.Format)
                                {
                                    case "WAVE":
                                        MCAFile = MCA3DSHelper.ConvertToMCA((WAVE) SCGIFile.SoundFile);
                                        break;
                                    case "FWSE":
                                        MCAFile = MCA3DSHelper.ConvertToMCA(FWSEHelper.ConvertToWAVE((FWSE) SCGIFile.SoundFile));
                                        break;
                                    case "XSEW":
                                        MCAFile = MCA3DSHelper.ConvertToMCA(XSEWHelper.ConvertToWAVE((XSEW) SCGIFile.SoundFile));
                                        break;
                                    default:
                                        continue;
                                }
    
                                MCA3DSHelper.WriteMCA($"{BasePath}{SCGIFile.FileName}.mca", MCAFile, false);
    
                                break;
                        }
                    }

                    XtraMessageBox.Show("Files converted successfully.", "Done!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void ConversionType_IndexChanged(object sender, EventArgs e)
        {
            if (ToConvertFiles == null)
            {
                SoundConversionTextEdit.Text = "No sound file loaded.";
                return;
            }

            string ConversionType = (ConversionTypeComboBox.SelectedItem as ListItem).SValue;
            SoundConversionTextEdit.Text = $"Convert {ToConvertFiles.Count} files to: {ConversionType}";
        }

        private void PlaySoundFile_Click(object sender, EventArgs e)
        {
            if (ToConvertFiles == null)
                return;

            SCGI SCGIFile = SoundGridView.GetRow(SoundGridView.FocusedRowHandle) as SCGI;
            WAVE WAVEFile = null;

            switch (SCGIFile.Format)
            {
                case "WAVE":
                    WAVEFile = (WAVE) SCGIFile.SoundFile;
                    break;
                case "FWSE":
                    WAVEFile = FWSEHelper.ConvertToWAVE((FWSE) SCGIFile.SoundFile);
                    break;
                case "XSEW":
                    WAVEFile = XSEWHelper.ConvertToWAVE((XSEW) SCGIFile.SoundFile);
                    break;
                case "MADP":
                    WAVEFile = MCA3DSHelper.ConvertToWAVE((MCA3DS) SCGIFile.SoundFile);
                    break;
            }

            string WAVEFileDir = Directory.GetCurrentDirectory() + "/ToPlay.wav";
            WAVEHelper.WriteWAVE(WAVEFileDir, WAVEFile, false);
            MemoryStream MS = new MemoryStream(File.ReadAllBytes(WAVEFileDir));
            File.Delete(WAVEFileDir);
            AppSoundPlayer.Stream = MS;
            AppSoundPlayer.Play();

            SoundGridView.CloseEditor();
        }

        private void RemoveSoundFile_Click(object sender, EventArgs e)
        {
            if (ToConvertFiles == null)
                return;

            SCGI SCGIFile = SoundGridView.GetRow(SoundGridView.FocusedRowHandle) as SCGI;
            ToConvertFiles.Remove(SCGIFile);

            if (ToConvertFiles.Count == 0)
                ClearFiles_Click(null, null);
            else
                SoundDataGridControl.RefreshDataSource();
        }
    }
}
