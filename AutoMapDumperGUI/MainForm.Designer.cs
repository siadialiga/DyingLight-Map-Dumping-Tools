using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;

namespace AutoMapDumperGUI
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        private TreeView tvMaps;
        private Button btnBrowseGame;
        private Button btnDump;
        private TextBox txtLog;
        private Label lblMap;
        
        private Label lblOutputDir;
        private TextBox txtOutputDir;
        private Button btnBrowseOutput;
        private Label lblOutputHint;
        
        private LinkLabel lblCredits;
        private LinkLabel lblVersion;
        private TabControl tcMode;
        private TabPage tpAutomated;
        private TabPage tpManual;
        private Label lblAutoDesc;
        private Label lblManualDesc;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.Text = "Dying Light Auto Map Dumper";
            this.Size = new Size(800, 735);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            lblMap = new Label { Text = "Available Maps (Grouped by DLC):", Location = new Point(20, 20), AutoSize = true };
            this.Controls.Add(lblMap);

            btnBrowseGame = new Button { Text = "Browse Game", Location = new Point(670, 15), Width = 100 };
            btnBrowseGame.Click += BtnBrowseGame_Click;
            this.Controls.Add(btnBrowseGame);

            tvMaps = new TreeView { Location = new Point(20, 45), Width = 750, Height = 220 };
            tvMaps.AfterSelect += TvMaps_AfterSelect;
            this.Controls.Add(tvMaps);

            lblOutputDir = new Label { Text = "Output Folder:", Location = new Point(20, 275), AutoSize = true };
            this.Controls.Add(lblOutputDir);

            txtOutputDir = new TextBox { Location = new Point(20, 295), Width = 640 };
            this.Controls.Add(txtOutputDir);

            btnBrowseOutput = new Button { Text = "Browse Output", Location = new Point(670, 294), Width = 100 };
            btnBrowseOutput.Click += BtnBrowseOutput_Click;
            this.Controls.Add(btnBrowseOutput);

            lblOutputHint = new Label { Location = new Point(20, 320), AutoSize = true, Font = new Font("Segoe UI", 8F, FontStyle.Italic), ForeColor = Color.Gray };
            this.Controls.Add(lblOutputHint);

            tcMode = new TabControl { Location = new Point(20, 340), Width = 750, Height = 95 };
            
            tpAutomated = new TabPage { Text = "Automated (Direct Binary Patch) - RECOMMENDED" };
            lblAutoDesc = new Label {
                Text = "This mode directly patches the .map binary with ModelObjects.\nNo extra manual steps or .eds files required.\nYou just need to click the dump button.",
                Location = new Point(10, 10),
                Size = new Size(720, 50)
            };
            tpAutomated.Controls.Add(lblAutoDesc);

            tpManual = new TabPage { Text = "Manual (EDS Export) - LEGACY" };
            lblManualDesc = new Label {
                Text = "Extracts to an external .eds file. Requires you to run Map2EDS, create a dummy object,\ngroup it, set matrix to 0,0,0, and manually swap the generated .eds file in your project folder.\nUse only if you need EDS files of core game maps.",
                Location = new Point(10, 10),
                Size = new Size(720, 50)
            };
            tpManual.Controls.Add(lblManualDesc);

            tcMode.TabPages.Add(tpAutomated);
            tcMode.TabPages.Add(tpManual);
            tcMode.SelectedIndexChanged += TcMode_SelectedIndexChanged;
            this.Controls.Add(tcMode);

            btnDump = new Button { Text = "Dump Selected", Location = new Point(20, 445), Width = 200, Height = 40, Enabled = false };
            btnDump.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnDump.Click += BtnDump_Click;
            this.Controls.Add(btnDump);

            txtLog = new TextBox { Location = new Point(20, 495), Width = 750, Height = 175, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };
            this.Controls.Add(txtLog);

            lblCredits = new LinkLabel 
            { 
                Text = "Made by Batuhan and Brendon", 
                AutoSize = true, 
                Font = new Font("Segoe UI", 8F),
                LinkColor = Color.DarkBlue,
                ActiveLinkColor = Color.Blue,
                LinkBehavior = LinkBehavior.HoverUnderline
            };
            
            lblCredits.Links.Add(8, 7, "https://github.com/siadialiga/");
            lblCredits.Links.Add(20, 7, "https://github.com/12brendon34/");
            
            lblCredits.LinkClicked += (s, e) => 
            {
                if (e.Link != null && e.Link.LinkData != null)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = e.Link.LinkData.ToString(),
                        UseShellExecute = true
                    });
                }
            };
            this.Controls.Add(lblCredits);

            lblVersion = new LinkLabel
            {
                Text = $"DLMDT.v{Application.ProductVersion}",
                AutoSize = true,
                Font = new Font("Segoe UI", 8F),
                LinkColor = Color.DarkBlue,
                ActiveLinkColor = Color.Blue,
                LinkBehavior = LinkBehavior.HoverUnderline
            };
            lblVersion.Links.Add(0, 5, "https://github.com/siadialiga/DyingLight-Map-Dumping-Tools");
            this.Controls.Add(lblVersion);

            lblVersion.LinkClicked += (s, e) => 
            {
                if (e.Link != null && e.Link.LinkData != null)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = e.Link.LinkData.ToString(),
                        UseShellExecute = true
                    });
                }
            };
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            if (lblCredits != null && txtLog != null)
            {
                // Place it slightly below the bottom right of the log box
                lblCredits.Location = new Point(txtLog.Right - lblCredits.Width, txtLog.Bottom + 5);
            }
            if (lblVersion != null && txtLog != null)
            {
                lblVersion.Location = new Point(txtLog.Left, txtLog.Bottom + 5);
            }
        }
    }
}
