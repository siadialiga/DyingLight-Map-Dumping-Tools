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

        class ModelEntity
        {
            public float[] Position = new float[3] { 0, 0, 0 };
            public float[] Rotation = new float[3] { 0, 0, 0 };
            public float[] Scale = new float[3] { 1, 1, 1 };
            public string MeshName = "dummy_box.msh";
            public string SkinName = "";
            public float[] Color0 = new float[4] { 1, 1, 1, 1 };
            public float[] Color1 = new float[4] { 0, 0, 0, 0 };
            public long RequiredTags = 0;
            public long ForbiddenTags = 0;
            public uint Seed = 0;
        }

        private float[] ParseVector3(string val)
        {
            var parts = val.Trim('<', '>').Split(',');
            return new float[] { float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]) };
        }

        private float[] ParseColor(string val)
        {
            var parts = val.Trim('<', '>').Split(',');
            return new float[] { 
                float.Parse(parts[0]) / 255f, 
                float.Parse(parts[1]) / 255f, 
                float.Parse(parts[2]) / 255f, 
                float.Parse(parts[3]) / 255f 
            };
        }

        private byte[] CreateTransformMatrix(float[] rot, float[] scale, float[] pos)
        {
            double radX = rot[0] * Math.PI / 180.0;
            double radY = rot[1] * Math.PI / 180.0;
            double radZ = rot[2] * Math.PI / 180.0;

            double cosX = Math.Cos(radX), sinX = Math.Sin(radX);
            double cosY = Math.Cos(radY), sinY = Math.Sin(radY);
            double cosZ = Math.Cos(radZ), sinZ = Math.Sin(radZ);

            float m11 = (float)(cosY * cosZ * scale[0]);
            float m12 = (float)(-cosY * sinZ * scale[1]);
            float m13 = (float)(sinY * scale[2]);

            float m21 = (float)((cosX * sinZ + sinX * sinY * cosZ) * scale[0]);
            float m22 = (float)((cosX * cosZ - sinX * sinY * sinZ) * scale[1]);
            float m23 = (float)(-sinX * cosY * scale[2]);

            float m31 = (float)((sinX * sinZ - cosX * sinY * cosZ) * scale[0]);
            float m32 = (float)((sinX * cosZ + cosX * sinY * sinZ) * scale[1]);
            float m33 = (float)(cosX * cosY * scale[2]);

            byte[] matrix = new byte[48];
            Buffer.BlockCopy(BitConverter.GetBytes(m11), 0, matrix, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(m12), 0, matrix, 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(m13), 0, matrix, 8, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(pos[0]), 0, matrix, 12, 4);
            
            Buffer.BlockCopy(BitConverter.GetBytes(m21), 0, matrix, 16, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(m22), 0, matrix, 20, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(m23), 0, matrix, 24, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(pos[1]), 0, matrix, 28, 4);
            
            Buffer.BlockCopy(BitConverter.GetBytes(m31), 0, matrix, 32, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(m32), 0, matrix, 36, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(m33), 0, matrix, 40, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(pos[2]), 0, matrix, 44, 4);

            return matrix;
        }

        private void WriteTLV(List<byte> buf, ushort tag, byte[] data)
        {
            buf.AddRange(BitConverter.GetBytes(tag));
            buf.AddRange(BitConverter.GetBytes((uint)data.Length));
            buf.AddRange(data);
        }

        private byte[] BuildModelObject(ModelEntity ent, uint objId)
        {
            List<byte> temp = new List<byte>();

            // w\x00 + class name block
            byte[] classBytes = System.Text.Encoding.ASCII.GetBytes("ModelObject");
            temp.Add(0x77); temp.Add(0x00);
            temp.AddRange(BitConverter.GetBytes((uint)classBytes.Length + 2));
            temp.AddRange(BitConverter.GetBytes((ushort)classBytes.Length));
            temp.AddRange(classBytes);

            // tag 0x003D (flags)
            WriteTLV(temp, 0x003D, BitConverter.GetBytes(0x00004001));

            // tag 0x145A (Z14 block)
            byte[] z14 = new byte[] {
                0x90, 0x01, 0x00, 0x00, 0x2C, 0x01, 0x00, 0x00,
                0x01, 0x00, 0x00, 0x00, 0x10, 0x00, 0x00, 0x00,
                0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            };
            WriteTLV(temp, 0x145A, z14);

            // tag 0x00C4 (colors)
            List<byte> c4 = new List<byte>();
            c4.AddRange(BitConverter.GetBytes(2u));
            c4.AddRange(BitConverter.GetBytes(0u));
            foreach (float c in ent.Color0) c4.AddRange(BitConverter.GetBytes(c));
            c4.AddRange(new byte[16]);
            foreach (float c in ent.Color0) c4.AddRange(BitConverter.GetBytes(c));
            c4.AddRange(new byte[16]);
            WriteTLV(temp, 0x00C4, c4.ToArray());

            // tag 0x00CB (tags)
            List<byte> cb = new List<byte>();
            cb.AddRange(BitConverter.GetBytes(ent.RequiredTags));
            cb.AddRange(BitConverter.GetBytes(ent.ForbiddenTags));
            cb.AddRange(BitConverter.GetBytes(ent.Seed));
            WriteTLV(temp, 0x00CB, cb.ToArray());

            // tag 0x00DF (transform matrix)
            byte[] df = CreateTransformMatrix(ent.Rotation, ent.Scale, ent.Position);
            WriteTLV(temp, 0x00DF, df);

            // tag 0x00BE
            List<byte> be = new List<byte>();
            be.AddRange(BitConverter.GetBytes(2.432065f));
            be.AddRange(BitConverter.GetBytes(1u));
            WriteTLV(temp, 0x00BE, be.ToArray());

            // tag 0x0014 (unique object ID)
            WriteTLV(temp, 0x0014, BitConverter.GetBytes(objId));

            // tag 0x007D (GUID)
            byte[] guidBytes = new byte[8];
            new Random().NextBytes(guidBytes);
            WriteTLV(temp, 0x007D, guidBytes);

            // tag 0x0016
            WriteTLV(temp, 0x0016, BitConverter.GetBytes((ushort)0));

            // tag 0x0079 (mesh/skin names)
            List<byte> x79 = new List<byte>();
            byte[] meshBytes = System.Text.Encoding.ASCII.GetBytes(ent.MeshName);
            byte[] skinBytes = string.IsNullOrEmpty(ent.SkinName) ? new byte[0] : System.Text.Encoding.ASCII.GetBytes(ent.SkinName);

            x79.Add(0x0C);
            x79.AddRange(BitConverter.GetBytes((ushort)8));
            x79.AddRange(System.Text.Encoding.ASCII.GetBytes("MeshName"));
            x79.AddRange(BitConverter.GetBytes((uint)meshBytes.Length + 2));
            x79.AddRange(BitConverter.GetBytes((ushort)meshBytes.Length));
            x79.AddRange(meshBytes);

            if (skinBytes.Length > 0 && ent.SkinName != "default")
            {
                x79.Add(0x0C);
                x79.AddRange(BitConverter.GetBytes((ushort)8));
                x79.AddRange(System.Text.Encoding.ASCII.GetBytes("SkinName"));
                x79.AddRange(BitConverter.GetBytes((uint)skinBytes.Length + 2));
                x79.AddRange(BitConverter.GetBytes((ushort)skinBytes.Length));
                x79.AddRange(skinBytes);
            }

            x79.Add(0xFF);
            WriteTLV(temp, 0x0079, x79.ToArray());

            // tag 0x007C
            WriteTLV(temp, 0x007C, BitConverter.GetBytes(-1));

            // F7 trailer
            temp.AddRange(new byte[] { 0xF7, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00 });

            // Wrap as root object
            List<byte> result = new List<byte>();
            result.Add(0x76); result.Add(0x00);
            result.AddRange(BitConverter.GetBytes((uint)temp.Count));
            result.AddRange(temp);

            return result.ToArray();
        }

        private void ProcessDump(List<(string pakPath, string mapEntry, string groupName)> mapsToDump, string outputBasePath)
        {
            Log($"Starting dump process for {mapsToDump.Count} map(s)...");

            string dumperExe = FindTool("SO18_Dumper.exe");

            if (dumperExe == null)
            {
                Log("ERROR: SO18_Dumper.exe not found.");
                return;
            }

            foreach (var map in mapsToDump)
            {
                string mapName = Path.GetFileName(map.mapEntry); // e.g. slums.sobj
                string mapNameNoExt = Path.GetFileNameWithoutExtension(mapName); // slums
                
                string mapOutputDir;

                bool isWorkshop = outputBasePath.ToLower().Contains(@"devtools\workshop") || outputBasePath.ToLower().EndsWith(@"data\maps");

                if (isWorkshop)
                {
                    string dataDir = outputBasePath;
                    while (!string.IsNullOrEmpty(dataDir) && Path.GetFileName(dataDir).ToLower() != "data")
                    {
                        dataDir = Path.GetDirectoryName(dataDir);
                    }
                    
                    if (!string.IsNullOrEmpty(dataDir))
                    {
                        mapOutputDir = Path.Combine(dataDir, "maps", mapNameNoExt);
                    }
                    else
                    {
                        mapOutputDir = Path.Combine(outputBasePath, mapNameNoExt);
                    }
                }
                else
                {
                    string groupOutputDir = Path.Combine(outputBasePath, map.groupName);
                    mapOutputDir = Path.Combine(groupOutputDir, mapNameNoExt);
                }

                Directory.CreateDirectory(mapOutputDir);

                Log($"[{map.groupName}] Extracting map folder to {mapOutputDir}...");

                // 1. Extract entire map folder from PAK
                string mapFolderInPak = Path.GetDirectoryName(map.mapEntry).Replace('\\', '/').TrimEnd('/') + "/";
                try
                {
                    using (var archive = ZipFile.OpenRead(map.pakPath))
                    {
                        foreach (var entry in archive.Entries)
                        {
                            if (entry.FullName.StartsWith(mapFolderInPak, StringComparison.OrdinalIgnoreCase))
                            {
                                string relativePath = entry.FullName.Substring(mapFolderInPak.Length);
                                if (string.IsNullOrEmpty(relativePath)) continue;

                                string destPath = Path.Combine(mapOutputDir, relativePath);
                                Directory.CreateDirectory(Path.GetDirectoryName(destPath));
                                entry.ExtractToFile(destPath, true);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log($" -> Extraction failed: {ex.Message}");
                    continue;
                }

                // 2. Rename .exp to .map
                string expFile = Directory.GetFiles(mapOutputDir, "*.exp").FirstOrDefault();
                string mapFile = null;
                if (expFile != null)
                {
                    mapFile = Path.ChangeExtension(expFile, ".map");
                    try
                    {
                        if (File.Exists(mapFile)) File.Delete(mapFile);
                        File.Move(expFile, mapFile);
                    }
                    catch (Exception ex)
                    {
                        Log($" -> Failed to rename .exp to .map: {ex.Message}");
                    }
                }

                // 3. Prepare SOBJ path
                string sobjPath = Path.Combine(mapOutputDir, $"{mapNameNoExt}.sobj");
                string txtPath = Path.Combine(mapOutputDir, $"{mapNameNoExt}.txt");

                if (!File.Exists(sobjPath))
                {
                    Log($" -> Error: extracted SOBJ not found at {sobjPath}");
                    continue;
                }

                Log($" -> Running SOBJ Dump...");

                // 4. Dump SOBJ directly preserving encoding
                try
                {
                    // Redirect through cmd to properly save output with encoding
                    var psi = new ProcessStartInfo("cmd.exe", $"/c \"\"{dumperExe}\" \"{sobjPath}\" > \"{txtPath}\"\"")
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
                    Log($" -> Error running SO18_Dumper: {ex.Message}");
                    continue;
                }

                // 5. Parse SOBJ txt directly
                Log(" -> Parsing entities...");
                List<ModelEntity> entities = new List<ModelEntity>();
                
                try 
                {
                    string[] lines = null;
                    
                    // Fallback try-catches for PowerShell redirects converting to UTF-8
                    try { lines = File.ReadAllLines(txtPath, System.Text.Encoding.UTF8); if(lines.Length == 0 || !lines[0].Contains("Class")) throw new Exception(); }
                    catch { try { lines = File.ReadAllLines(txtPath, System.Text.Encoding.Unicode); } catch { lines = File.ReadAllLines(txtPath); } }
                    
                    for (int i = 0; i < lines.Length; i++)
                    {
                        string line = lines[i].Trim();
                        if (string.IsNullOrEmpty(line)) continue;
                        
                        if (line.StartsWith("Class = ModelObject"))
                        {
                            ModelEntity ent = new ModelEntity();
                            i++;
                            while (i < lines.Length && !lines[i].Trim().StartsWith("Class ="))
                            {
                                string prop = lines[i].Trim();
                                if (!string.IsNullOrEmpty(prop) && prop.Contains("="))
                                {
                                    int eq = prop.IndexOf('=');
                                    string k = prop.Substring(0, eq).Trim();
                                    string v = prop.Substring(eq + 1).Trim();

                                    try {
                                        if (k == "Position") ent.Position = ParseVector3(v);
                                        else if (k == "Rotation") ent.Rotation = ParseVector3(v);
                                        else if (k == "Scale") ent.Scale = ParseVector3(v);
                                        else if (k == "MeshName") ent.MeshName = v;
                                        else if (k == "SkinName") ent.SkinName = v;
                                        else if (k == "Color0") ent.Color0 = ParseColor(v);
                                        else if (k == "Color1") ent.Color1 = ParseColor(v);
                                        else if (k == "Seed") ent.Seed = uint.Parse(v);
                                        else if (k == "required_tags") ent.RequiredTags = long.Parse(v);
                                        else if (k == "forbidden_tags") ent.ForbiddenTags = long.Parse(v);
                                    } catch { }
                                }
                                i++;
                            }
                            entities.Add(ent);
                            i--; // re-evaluate the next class
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log($" -> Failed to parse entities: {ex.Message}");
                }

                Log($" -> Parsed {entities.Count} ModelObjects.");

                // Cleanup temporary txt
                try
                {
                    if (File.Exists(txtPath)) File.Delete(txtPath);
                }
                catch { }

                // 6. Binary patch the .map file natively
                if (mapFile != null && File.Exists(mapFile) && entities.Count > 0)
                {
                    Log(" -> Building binary objects...");
                    try
                    {
                        List<byte> allObjects = new List<byte>();
                        uint baseId = (uint)(0x10000000 + new Random().Next(0, 0x0FFFFFFF));
                        
                        for (int i = 0; i < entities.Count; i++)
                        {
                            allObjects.AddRange(BuildModelObject(entities[i], baseId + (uint)i));
                        }
                        
                        byte[] fileData = File.ReadAllBytes(mapFile);
                        
                        int firstRootIndex = -1;
                        for (int i = 0; i <= fileData.Length - 8; i++)
                        {
                            if (fileData[i] == 0x76 && fileData[i+1] == 0x00 && fileData[i+6] == 0x77 && fileData[i+7] == 0x00)
                            {
                                firstRootIndex = i;
                                break;
                            }
                        }

                        if (firstRootIndex != -1)
                        {
                            int currentIndex = firstRootIndex;
                            while (currentIndex < fileData.Length - 6)
                            {
                                if (fileData[currentIndex] != 0x76 || fileData[currentIndex + 1] != 0x00)
                                    break;
                                
                                uint size = BitConverter.ToUInt32(fileData, currentIndex + 2);
                                currentIndex += 6 + (int)size;
                            }
                            
                            if (currentIndex >= fileData.Length) currentIndex = fileData.Length;

                            byte[] insertArr = allObjects.ToArray();
                            uint origSize = BitConverter.ToUInt32(fileData, 2);
                            byte[] newSizeBytes = BitConverter.GetBytes(origSize + (uint)insertArr.Length);
                            Array.Copy(newSizeBytes, 0, fileData, 2, 4);

                            byte[] patchedData = new byte[fileData.Length + insertArr.Length];
                            Array.Copy(fileData, 0, patchedData, 0, currentIndex);
                            Array.Copy(insertArr, 0, patchedData, currentIndex, insertArr.Length);
                            Array.Copy(fileData, currentIndex, patchedData, currentIndex + insertArr.Length, fileData.Length - currentIndex);
                            
                            File.WriteAllBytes(mapFile, patchedData);
                            Log($" -> Patched native map! Generated {insertArr.Length} bytes.");
                        }
                        else
                        {
                            Log(" -> Error: Root object pattern not found in .map");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($" -> Failed to patch binary .map: {ex.Message}");
                    }
                }

                Log($" -> Success! Map ready: {mapOutputDir}");
            }

            Log("=== ALL OPERATIONS COMPLETE ===");
        }


    }
}
