/*
    This file is part of RESIDENT EVIL STQ Tool.
    RESIDENT EVIL STQ Tool is free software: you can redistribute it
    and/or modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation, either version 3 of
    the License, or (at your option) any later version.
    RESIDENT EVIL STQ Tool is distributed in the hope that it will
    be useful, but WITHOUT ANY WARRANTY; without even the implied
    warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
    See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License
    along with RESIDENT EVIL STQ Tool. If not, see <https://www.gnu.org/licenses/>6.
*/

using System;
using System.Windows.Forms;
using MTF.SoundTool.Updater.Base.Helpers;

namespace MTF.SoundTool.Updater.Base.Modules
{
    public static class Terminal
    {
        private static App Main { get; set; }

        public static void StartModule(App GameXRef)
        {
            Main = GameXRef;
            WriteLine("[Console] Module started successfully.");
        }

        public static void ScrollToEnd()
        {
            Main.ConsoleOutputMemoEdit.SelectionStart = Main.ConsoleOutputMemoEdit.Text.Length;
            Main.ConsoleOutputMemoEdit.MaskBox?.MaskBoxScrollToCaret();
        }

        public static void WriteLine(string Output, bool DownloadReport = false)
        {
            string Current = Main.ConsoleOutputMemoEdit.Text;

            if (string.IsNullOrWhiteSpace(Current))
                Current = Output;
            else
            {
                if (DownloadReport && Current.Contains("[App] Downloading: "))
                {
                    string NewPercentage = Utility.StringBetween(Output, "[App] Downloading: ", "%");
                    string OldPercentage = Utility.StringBetween(Current, "[App] Downloading: ", "%");

                    Current = Current.Replace(OldPercentage, NewPercentage);
                }
                else
                    Current += Environment.NewLine + Output;
            }

            if (Main.ConsoleOutputMemoEdit.InvokeRequired)
            {
                Main.ConsoleOutputMemoEdit.Invoke((MethodInvoker)delegate
                {
                    Main.ConsoleOutputMemoEdit.Text = Current;
                    ScrollToEnd();
                });
                return;
            }

            Main.ConsoleOutputMemoEdit.Text = Current;
            ScrollToEnd();
        }
    }
}
