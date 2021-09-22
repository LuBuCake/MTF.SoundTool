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

namespace MTF.SoundTool.Updater
{
    partial class App
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(App));
            this.ConsoleGP = new DevExpress.XtraEditors.GroupControl();
            this.ConsoleOutputMemoEdit = new DevExpress.XtraEditors.MemoEdit();
            ((System.ComponentModel.ISupportInitialize)(this.ConsoleGP)).BeginInit();
            this.ConsoleGP.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ConsoleOutputMemoEdit.Properties)).BeginInit();
            this.SuspendLayout();
            // 
            // ConsoleGP
            // 
            this.ConsoleGP.Controls.Add(this.ConsoleOutputMemoEdit);
            this.ConsoleGP.Location = new System.Drawing.Point(12, 12);
            this.ConsoleGP.Name = "ConsoleGP";
            this.ConsoleGP.ShowCaption = false;
            this.ConsoleGP.Size = new System.Drawing.Size(640, 314);
            this.ConsoleGP.TabIndex = 3;
            // 
            // ConsoleOutputMemoEdit
            // 
            this.ConsoleOutputMemoEdit.Location = new System.Drawing.Point(5, 5);
            this.ConsoleOutputMemoEdit.Name = "ConsoleOutputMemoEdit";
            this.ConsoleOutputMemoEdit.Properties.AllowFocused = false;
            this.ConsoleOutputMemoEdit.Properties.ReadOnly = true;
            this.ConsoleOutputMemoEdit.Properties.ShowNullValuePrompt = DevExpress.XtraEditors.ShowNullValuePromptOptions.NullValue;
            this.ConsoleOutputMemoEdit.Properties.UseReadOnlyAppearance = false;
            this.ConsoleOutputMemoEdit.Size = new System.Drawing.Size(630, 304);
            this.ConsoleOutputMemoEdit.TabIndex = 0;
            this.ConsoleOutputMemoEdit.TabStop = false;
            // 
            // App
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(664, 338);
            this.Controls.Add(this.ConsoleGP);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.IconOptions.Image = ((System.Drawing.Image)(resources.GetObject("App.IconOptions.Image")));
            this.MaximizeBox = false;
            this.Name = "App";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "RESPCTool - Updater";
            this.Load += new System.EventHandler(this.App_Load);
            ((System.ComponentModel.ISupportInitialize)(this.ConsoleGP)).EndInit();
            this.ConsoleGP.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.ConsoleOutputMemoEdit.Properties)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraEditors.GroupControl ConsoleGP;
        public DevExpress.XtraEditors.MemoEdit ConsoleOutputMemoEdit;
    }
}

