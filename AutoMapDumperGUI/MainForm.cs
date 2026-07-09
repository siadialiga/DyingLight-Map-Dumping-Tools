// ---------------------------------------------------------
// Created by Batuhan on 7/8/2026
// ---------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutoMapDumperGUI
{
    public partial class MainForm : Form
    {
        private TreeView tvMaps;
        private Button btnBrowseGame;
        private Button btnDump;
        private TextBox txtLog;
        private Label lblMap;
        
        private Label lblOutputDir;
        private TextBox txtOutputDir;
        private Button btnBrowseOutput;
        
        private LinkLabel lblCredits;

        private readonly string defaultGamePath = @"C:\Program Files (x86)\Steam\steamapps\common\Dying Light";

        public MainForm()
        {
            InitializeComponent();
            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "favicon.ico");
                if (File.Exists(iconPath))
                {
                    this.Icon = new Icon(iconPath);
                }
            }
            catch { }
            this.Load += MainForm_Load;
        }

        private void InitializeComponent()
        {
            this.Text = "Dying Light Auto Map Dumper";
            this.Size = new Size(800, 640);
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

            lblOutputDir = new Label { Text = "DumpedMaps Output Folder:", Location = new Point(20, 280), AutoSize = true };
            this.Controls.Add(lblOutputDir);

            txtOutputDir = new TextBox { Location = new Point(20, 300), Width = 640 };
            this.Controls.Add(txtOutputDir);

            btnBrowseOutput = new Button { Text = "Browse Output", Location = new Point(670, 299), Width = 100 };
            btnBrowseOutput.Click += BtnBrowseOutput_Click;
            this.Controls.Add(btnBrowseOutput);

            btnDump = new Button { Text = "Dump Selected", Location = new Point(20, 335), Width = 200, Height = 40, Enabled = false };
            btnDump.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnDump.Click += BtnDump_Click;
            this.Controls.Add(btnDump);

            txtLog = new TextBox { Location = new Point(20, 385), Width = 750, Height = 175, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };
            this.Controls.Add(txtLog);

            lblCredits = new LinkLabel 
            { 
                Text = "Made by Batuhan and Brendon", 
                AutoSize = true, 
                Font = new Font("Segoe UI", 8F),
                LinkColor = Color.Gray,
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
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            if (lblCredits != null && txtLog != null)
            {
                // Place it slightly below the bottom right of the log box
                lblCredits.Location = new Point(txtLog.Right - lblCredits.Width, txtLog.Bottom + 5);
            }
        }

        private void Log(string message)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => Log(message)));
                return;
            }
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
            txtLog.SelectionStart = txtLog.Text.Length;
            txtLog.ScrollToCaret();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Log("Application started.");
            
            txtOutputDir.Text = Path.Combine(defaultGamePath, "DumpedMaps");

            if (Directory.Exists(defaultGamePath))
            {
                Log($"Found default game path: {defaultGamePath}");
                ScanForPaks(defaultGamePath);
            }
            else
            {
                Log("Default game path not found. Please click 'Browse Game' to locate your Dying Light installation.");
            }
        }

        private void BtnBrowseGame_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Select your Dying Light installation folder";
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtOutputDir.Text = Path.Combine(fbd.SelectedPath, "DumpedMaps");
                    ScanForPaks(fbd.SelectedPath);
                }
            }
        }

        private void BtnBrowseOutput_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Select output folder for Dumped Maps";
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtOutputDir.Text = fbd.SelectedPath;
                }
            }
        }

        private async void ScanForPaks(string path)
        {
            tvMaps.Nodes.Clear();
            btnDump.Enabled = false;

            Log($"Scanning for .pak files containing maps in {path}...");
            
            await Task.Run(() =>
            {
                try
                {
                    string[] files = Directory.GetFiles(path, "*.pak", SearchOption.AllDirectories);
                    
                    var pakData = new Dictionary<string, List<string>>();

                    foreach (var file in files)
                    {
                        string fileName = Path.GetFileName(file).ToLower();
                        if (!fileName.Contains("data") || !fileName.Contains("2")) continue;

                        try
                        {
                            using (var archive = ZipFile.OpenRead(file))
                            {
                                var maps = archive.Entries
                                    .Where(entry => entry.FullName.EndsWith(".sobj", StringComparison.OrdinalIgnoreCase))
                                    .Select(entry => entry.FullName)
                                    .ToList();

                                if (maps.Count > 0)
                                {
                                    pakData[file] = maps;
                                }
                            }
                        }
                        catch { }
                    }

                    this.Invoke(new Action(() =>
                    {
                        if (pakData.Count > 0)
                        {
                            TreeNode rootNode = new TreeNode("Dying Light");
                            rootNode.Tag = new string[] { "ROOT" };

                            foreach (var kvp in pakData.OrderBy(x => x.Key))
                            {
                                string pakPath = kvp.Key;
                                List<string> maps = kvp.Value;

                                string parentDirName = new DirectoryInfo(Path.GetDirectoryName(pakPath)).Name;
                                string groupName = parentDirName; // DW, DW_DLC1, etc.
                                string nodeName = $"{groupName} ({Path.GetFileName(pakPath)})";

                                TreeNode pakNode = new TreeNode(nodeName);
                                pakNode.Tag = new string[] { "PAK", pakPath, groupName };

                                foreach (var map in maps.OrderBy(m => m))
                                {
                                    TreeNode childNode = new TreeNode(Path.GetFileName(map));
                                    childNode.Tag = new string[] { "MAP", pakPath, map, groupName };
                                    pakNode.Nodes.Add(childNode);
                                }

                                rootNode.Nodes.Add(pakNode);
                            }

                            tvMaps.Nodes.Add(rootNode);
                            rootNode.Expand();
                            Log($"Found maps in {pakData.Count} DLC/Pak file(s).");
                        }
                        else
                        {
                            Log("No maps (.sobj) found in any .pak files.");
                        }
                    }));
                }
                catch (Exception ex)
                {
                    Log($"Error scanning folder: {ex.Message}");
                }
            });
        }

        private void TvMaps_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node == null || !(e.Node.Tag is string[] tagData))
            {
                btnDump.Enabled = false;
                return;
            }

            btnDump.Enabled = true;

            switch (tagData[0])
            {
                case "ROOT":
                    btnDump.Text = "Dump ALL Maps";
                    break;
                case "PAK":
                    btnDump.Text = "Dump Selected Group";
                    break;
                case "MAP":
                    btnDump.Text = "Dump Selected Map";
                    break;
            }
        }

        private async void BtnDump_Click(object sender, EventArgs e)
        {
            if (tvMaps.SelectedNode == null || !(tvMaps.SelectedNode.Tag is string[] tagData)) return;

            string outputBasePath = txtOutputDir.Text;
            if (string.IsNullOrWhiteSpace(outputBasePath))
            {
                Log("Error: Output folder cannot be empty.");
                return;
            }

            var mapsToDump = new List<(string pakPath, string mapEntry, string groupName)>();

            if (tagData[0] == "ROOT")
            {
                foreach (TreeNode pakNode in tvMaps.SelectedNode.Nodes)
                {
                    if (pakNode.Tag is string[] pakTag)
                    {
                        foreach (TreeNode mapNode in pakNode.Nodes)
                        {
                            if (mapNode.Tag is string[] mapTag)
                            {
                                mapsToDump.Add((mapTag[1], mapTag[2], mapTag[3]));
                            }
                        }
                    }
                }
            }
            else if (tagData[0] == "PAK")
            {
                foreach (TreeNode mapNode in tvMaps.SelectedNode.Nodes)
                {
                    if (mapNode.Tag is string[] mapTag)
                    {
                        mapsToDump.Add((mapTag[1], mapTag[2], mapTag[3]));
                    }
                }
            }
            else if (tagData[0] == "MAP")
            {
                mapsToDump.Add((tagData[1], tagData[2], tagData[3]));
            }

            if (mapsToDump.Count == 0)
            {
                Log("No maps found to dump.");
                return;
            }

            btnDump.Enabled = false;
            tvMaps.Enabled = false;
            btnBrowseGame.Enabled = false;
            btnBrowseOutput.Enabled = false;

            await Task.Run(() => ProcessDump(mapsToDump, outputBasePath));

            btnDump.Enabled = true;
            tvMaps.Enabled = true;
            btnBrowseGame.Enabled = true;
            btnBrowseOutput.Enabled = true;
        }

        private string FindTool(string name)
        {
            string[] possiblePaths = new string[]
            {
                name,
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, name),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", Path.GetFileNameWithoutExtension(name), name),
                Path.Combine(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)?.Parent?.Parent?.Parent?.Parent?.FullName ?? "", Path.GetFileNameWithoutExtension(name), "bin", "Debug", "net8.0", name),
                Path.Combine(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)?.Parent?.Parent?.Parent?.Parent?.FullName ?? "", Path.GetFileNameWithoutExtension(name), "bin", "Release", "net8.0", name),
            };

            foreach (var p in possiblePaths)
            {
                try
                {
                    if (File.Exists(p)) return p;
                }
                catch { }
            }
            return null;
        }

        private void ProcessDump(List<(string pakPath, string mapEntry, string groupName)> mapsToDump, string outputBasePath)
        {
            Log($"Starting dump process for {mapsToDump.Count} map(s)...");

            string dumperExe = FindTool("SO18_Dumper.exe");
            string map2edsExe = FindTool("Map2EDS.exe");

            if (dumperExe == null)
            {
                Log("ERROR: SO18_Dumper.exe not found.");
                return;
            }
            if (map2edsExe == null)
            {
                Log("ERROR: Map2EDS.exe not found.");
                return;
            }

            foreach (var map in mapsToDump)
            {
                string groupOutputDir = Path.Combine(outputBasePath, map.groupName);
                Directory.CreateDirectory(groupOutputDir);

                string mapName = Path.GetFileName(map.mapEntry);
                string mapNameNoExt = Path.GetFileNameWithoutExtension(mapName);
                
                string extractedMapPath = Path.Combine(groupOutputDir, mapName);
                string txtPath = Path.Combine(groupOutputDir, $"{mapNameNoExt}.txt");
                string edsPath = Path.Combine(groupOutputDir, $"{mapNameNoExt}.eds");

                Log($"[{map.groupName}] Processing {mapName}...");

                // 1. Extract
                try
                {
                    using (var archive = ZipFile.OpenRead(map.pakPath))
                    {
                        var entry = archive.GetEntry(map.mapEntry);
                        if (entry != null)
                        {
                            entry.ExtractToFile(extractedMapPath, true);
                        }
                        else
                        {
                            Log($" -> Error: Could not find inside archive.");
                            continue;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log($" -> Extraction failed: {ex.Message}");
                    continue;
                }

                // 2. Dump
                try
                {
                    var psi = new ProcessStartInfo(dumperExe, $"\"{extractedMapPath}\"")
                    {
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    };
                    using (var process = Process.Start(psi))
                    {
                        string output = process.StandardOutput.ReadToEnd();
                        process.WaitForExit();
                        File.WriteAllText(txtPath, output);
                    }
                }
                catch (Exception ex)
                {
                    Log($" -> Error running SO18_Dumper: {ex.Message}");
                    continue;
                }

                // 3. Map2EDS
                try
                {
                    var psi = new ProcessStartInfo(map2edsExe, $"\"{txtPath}\" \"{edsPath}\"")
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using (var process = Process.Start(psi))
                    {
                        process.WaitForExit();
                    }
                }
                catch (Exception ex)
                {
                    Log($" -> Error running Map2EDS: {ex.Message}");
                    continue;
                }

                // 4. Cleanup
                try
                {
                    if (File.Exists(extractedMapPath)) File.Delete(extractedMapPath);
                    if (File.Exists(txtPath)) File.Delete(txtPath);
                }
                catch { }

                Log($" -> Success: {edsPath}");
            }

            WriteReadme(outputBasePath);
            Log("=== ALL OPERATIONS COMPLETE ===");
        }

        private void WriteReadme(string outputDir)
        {
            string guideText = @"================================================================================
IMPORTANT: FINAL MANUAL STEPS REQUIRED
================================================================================

Even though the extraction is automated, you must complete these final steps manually:

1. Locate the map folder in the game files:
   - Path: C:\Program Files (x86)\Steam\steamapps\common\Dying Light\DW\Data2.pak\data\maps\
   - Use 7-Zip to open Data2.pak.
   - Copy the folder of the map you just dumped and paste it onto your Desktop.

2. Prepare the map file:
   - Inside the copied folder on your Desktop, change the extension of the .exp file to .map.

3. Install the map for DevTools:
   - Move the modified map folder to:
     C:\Program Files (x86)\Steam\steamapps\common\Dying Light\DevTools\workshop\(YOUR PROJECT FOLDER)\data\maps

   - Then open your project in the Dying Light Developer Tools and load the map you exported from the core game.

4. Initialize the map in Developer Tools:
   - You will see objects scattered randomly—this is normal.
   - Place any random object in the map.
   - In the Attributes section, set its Matrix coordinates to 0, 0, 0 (x, y, z).
   - Group this object, save the map, and exit.

5. Import the dumped data:
   - Take the .eds file generated by this program.
   - Rename it to match the name of the .eds (the group) you just created in the map.
   - Replace the file in the workshop folder.

6. Final result:
   - Reopen the map in DevTools. All objects should now be properly loaded and positioned.
   - Recommendation: Use 'Destroy Hierarchy' to ungroup objects for easier editing.
================================================================================";

            string readmePath = Path.Combine(outputDir, "README.txt");
            if (!File.Exists(readmePath))
            {
                try { File.WriteAllText(readmePath, guideText); } catch { }
            }
        }
    }
}
