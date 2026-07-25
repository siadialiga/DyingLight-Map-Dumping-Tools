using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutoMapDumperGUI
{
    public partial class MainForm : Form
    {
        private readonly string defaultGamePath = @"C:\Program Files (x86)\Steam\steamapps\common\Dying Light";

        public MainForm()
        {
            InitializeComponent();
            try
            {
                this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch { }
            this.Load += MainForm_Load;
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
            
            UpdateOutputPathHint();

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

        private void TcMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateOutputPathHint();
        }

        private void UpdateOutputPathHint()
        {
            bool isAutomated = (tcMode.SelectedTab == tpAutomated);
            if (isAutomated)
            {
                lblOutputHint.Text = "Please select your project's data/maps folder here. Example: ...\\Dying Light\\DevTools\\workshop\\YourProject\\data\\maps";
                string workshopPath = @"C:\Program Files (x86)\Steam\steamapps\common\Dying Light\DevTools\workshop";
                if (Directory.Exists(workshopPath))
                {
                    txtOutputDir.Text = workshopPath;
                }
                else
                {
                    txtOutputDir.Text = workshopPath;
                }
            }
            else
            {
                lblOutputHint.Text = "You can select any empty folder to extract the generated .eds files.";
                txtOutputDir.Text = Path.Combine(defaultGamePath, "DumpedMaps");
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
                if (Directory.Exists(txtOutputDir.Text))
                {
                    fbd.SelectedPath = txtOutputDir.Text;
                }
                
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

            bool useAutomated = false;
            this.Invoke(new Action(() => { useAutomated = (tcMode.SelectedTab == tpAutomated); }));

            string dumperExe = FindTool("SO18_Dumper.exe");
            string map2edsExe = FindTool("Map2EDS.exe");

            await Task.Run(() => DumpManager.ProcessDump(mapsToDump, outputBasePath, useAutomated, dumperExe, map2edsExe, Log));

            btnDump.Enabled = true;
            tvMaps.Enabled = true;
            btnBrowseGame.Enabled = true;
            btnBrowseOutput.Enabled = true;
        }

        private string FindTool(string name)
        {
            DirectoryInfo? repositoryDirectory =
                new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (repositoryDirectory != null &&
                   !File.Exists(Path.Combine(repositoryDirectory.FullName, "MapTools.sln")))
            {
                repositoryDirectory = repositoryDirectory.Parent;
            }

            string repositoryRoot = repositoryDirectory?.FullName ?? "";
            string projectDirectory =
                name.Equals("SO18_Dumper.exe", StringComparison.OrdinalIgnoreCase)
                    ? "SO_Dumper"
                    : Path.GetFileNameWithoutExtension(name);

            string[] possiblePaths = new string[]
            {
                name,
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, name),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", Path.GetFileNameWithoutExtension(name), name),
                Path.Combine(repositoryRoot, projectDirectory, "bin", "Debug", "net8.0", name),
                Path.Combine(repositoryRoot, projectDirectory, "bin", "Release", "net8.0", name),
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
    }
}
