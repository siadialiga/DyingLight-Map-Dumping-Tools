using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Diagnostics;

namespace AutoMapDumperGUI
{
    public static class DumpManager
    {
        public static void ProcessDump(
            List<(string pakPath, string mapEntry, string groupName)> mapsToDump, 
            string outputBasePath, 
            bool useAutomated,
            string dumperExe,
            string map2edsExe,
            Action<string> Log)
        {
            Log($"Starting dump process for {mapsToDump.Count} map(s)...");

            if (dumperExe == null)
            {
                Log("ERROR: SO18_Dumper.exe not found.");
                return;
            }
            if (!useAutomated && map2edsExe == null)
            {
                Log("ERROR: Map2EDS.exe not found. Required for Manual mode.");
                return;
            }

            foreach (var map in mapsToDump)
            {
                string mapName = Path.GetFileName(map.mapEntry); // e.g. slums.sobj
                string mapNameNoExt = Path.GetFileNameWithoutExtension(mapName); // slums
                
                string mapOutputDir;

                bool isWorkshop = outputBasePath.ToLower().Contains(@"devtools\workshop") || outputBasePath.ToLower().EndsWith(@"data\maps");

                string edsOutputDir;

                if (isWorkshop)
                {
                    string dataDir = outputBasePath;
                    while (!string.IsNullOrEmpty(dataDir) && Path.GetFileName(dataDir).ToLower() != "data")
                    {
                        dataDir = Path.GetDirectoryName(dataDir);
                    }
                    
                    if (!string.IsNullOrEmpty(dataDir))
                    {
                        edsOutputDir = dataDir; 
                        mapOutputDir = Path.Combine(dataDir, "maps", mapNameNoExt);
                    }
                    else
                    {
                        edsOutputDir = Path.GetDirectoryName(outputBasePath); 
                        mapOutputDir = Path.Combine(outputBasePath, mapNameNoExt);
                    }
                }
                else
                {
                    string groupOutputDir = Path.Combine(outputBasePath, map.groupName);
                    edsOutputDir = groupOutputDir;
                    mapOutputDir = Path.Combine(groupOutputDir, mapNameNoExt);
                }
                Directory.CreateDirectory(edsOutputDir);

                string mapFile = null;

                if (useAutomated)
                {
                    Directory.CreateDirectory(mapOutputDir);
                    Log($"[{map.groupName}] Extracting map folder to {mapOutputDir}...");

                    // 1. Extract entire map folder from PAK
                    string mapFolderInPak = Path.GetDirectoryName(map.mapEntry).Replace('\\', '/').TrimEnd('/') + "/";
                    int extractedFileCount = 0;
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
                                    extractedFileCount++;
                                }
                            }
                        }
                        Log($" -> Extracted {extractedFileCount} file(s) from the map folder.");
                    }
                    catch (Exception ex)
                    {
                        Log($" -> Extraction failed: {ex.Message}");
                        continue;
                    }

                    // 2. Preserve the source EXP and use it as the closest available MAP
                    // baseline. Retail EXP is compiled output, not a full editor-source MAP;
                    // its static editable lights are serialized in the embedded SLIs chunk.
                    string expFile = Path.Combine(mapOutputDir, $"{mapNameNoExt}.exp");
                    if (File.Exists(expFile))
                    {
                        mapFile = Path.Combine(mapOutputDir, $"{mapNameNoExt}.map");
                        try
                        {
                            File.Copy(expFile, mapFile, true);

                            MapBinaryInventory sourceInventory =
                                BinaryPatchUtils.InspectMapFile(mapFile);
                            if (!sourceInventory.HasValidOuterLength)
                            {
                                throw new InvalidDataException(
                                    "The source EXP has an invalid outer LevelDI length.");
                            }

                            Log(
                                $" -> Source EXP inventory: {sourceInventory.ObjectCount} top-level " +
                                $"exported objects, {sourceInventory.TopLevelLightCount} light record(s), " +
                                $"{sourceInventory.Count("ModelObject")} native ModelObject(s).");

                            Log(
                                " -> Note: retail EXP is compiled map output, not the complete " +
                                "editor-source MAP; its light count is only the exported top-level subset.");

                            if (sourceInventory.TopLevelLightCount == 0)
                            {
                                Log(
                                    " -> Warning: this source EXP contains no top-level LightObject " +
                                    "records; SOBJ cannot supply dynamic lights.");
                            }
                        }
                        catch (Exception ex)
                        {
                            Log($" -> Failed to prepare .map from source .exp: {ex.Message}");
                            continue;
                        }
                    }
                    else
                    {
                        Log($" -> Error: exact source EXP not found: {expFile}");
                        continue;
                    }

                    // Compiled static/probe lighting is stored loose beside the PAK hierarchy.
                    // Workshop's Install Project copies these files verbatim with the MAP.
                    CopyLooseLightingFiles(
                        map.pakPath,
                        mapNameNoExt,
                        mapOutputDir,
                        Log);
                }
                else
                {
                    Log($"[{map.groupName}] Extracting {mapNameNoExt}.sobj for EDS generation...");
                    try
                    {
                        using (var archive = ZipFile.OpenRead(map.pakPath))
                        {
                            var entry = archive.Entries.FirstOrDefault(e => e.FullName.Equals(map.mapEntry, StringComparison.OrdinalIgnoreCase));
                            if (entry != null)
                            {
                                entry.ExtractToFile(Path.Combine(edsOutputDir, Path.GetFileName(map.mapEntry)), true);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($" -> Extraction failed: {ex.Message}");
                        continue;
                    }
                }

                // 3. Prepare SOBJ path
                string sobjPath = useAutomated ? Path.Combine(mapOutputDir, $"{mapNameNoExt}.sobj") : Path.Combine(edsOutputDir, $"{mapNameNoExt}.sobj");
                string txtPath = useAutomated ? Path.Combine(mapOutputDir, $"{mapNameNoExt}.txt") : Path.Combine(edsOutputDir, $"{mapNameNoExt}.txt");

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
                    Log($" -> Error running SO_Dumper: {ex.Message}");
                    continue;
                }

                // 5. Parse SOBJ txt directly
                string edsName = $"{mapNameNoExt}.eds";
                string edsPath = Path.Combine(edsOutputDir, edsName);

                if (!useAutomated)
                {
                    Log(" -> Running Map2EDS...");
                    try
                    {
                        var pMap2EDS = new ProcessStartInfo(map2edsExe, $"\"{txtPath}\" \"{edsPath}\"")
                        {
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        using (var process = Process.Start(pMap2EDS))
                        {
                            process.WaitForExit();
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($" -> Error running Map2EDS: {ex.Message}");
                    }
                }
                
                Log(" -> Parsing entities...");
                List<ModelEntity> entities = new List<ModelEntity>();
                int skippedModelObjectsWithoutMesh = 0;
                
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
                                        if (k == "Position") ent.Position = BinaryPatchUtils.ParseVector3(v);
                                        else if (k == "Rotation") ent.Rotation = BinaryPatchUtils.ParseVector3(v);
                                        else if (k == "Scale") ent.Scale = BinaryPatchUtils.ParseVector3(v);
                                        else if (k == "MeshName") ent.MeshName = v;
                                        else if (k == "SkinName") ent.SkinName = v;
                                        else if (k == "Color0") ent.Color0 = BinaryPatchUtils.ParseColor(v);
                                        else if (k == "Color1") ent.Color1 = BinaryPatchUtils.ParseColor(v);
                                        else if (k == "Seed") ent.Seed = uint.Parse(v);
                                        else if (k == "required_tags") ent.RequiredTags = long.Parse(v);
                                        else if (k == "forbidden_tags") ent.ForbiddenTags = long.Parse(v);
                                    } catch { }
                                }
                                i++;
                            }
                            if (string.IsNullOrWhiteSpace(ent.MeshName))
                            {
                                skippedModelObjectsWithoutMesh++;
                            }
                            else
                            {
                                entities.Add(ent);
                            }
                            i--; // re-evaluate the next class
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log($" -> Failed to parse entities: {ex.Message}");
                }

                Log($" -> Parsed {entities.Count} ModelObjects.");
                if (skippedModelObjectsWithoutMesh > 0)
                {
                    Log(
                        $" -> Skipped {skippedModelObjectsWithoutMesh} ModelObject(s) " +
                        "without MeshName instead of replacing them with dummy_box.msh.");
                }

                string[] buildTerrainMeshes = entities
                    .Select(entity => entity.MeshName)
                    .Where(meshName =>
                        !string.IsNullOrWhiteSpace(meshName) &&
                        meshName.Contains(
                            "_buildterrain_",
                            StringComparison.OrdinalIgnoreCase))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                if (buildTerrainMeshes.Length > 0)
                {
                    Log(
                        $" -> Found {buildTerrainMeshes.Length} built-terrain mesh " +
                        "reference(s) in SOBJ.");
                    StageEditorResourcePack(
                        map.pakPath,
                        mapNameNoExt,
                        outputBasePath,
                        isWorkshop,
                        Log);
                }

                // Cleanup temporary txt
                try
                {
                    if (File.Exists(txtPath)) File.Delete(txtPath);
                    if (!useAutomated && File.Exists(sobjPath)) File.Delete(sobjPath);
                }
                catch { }

                // 6. Binary patch the .map file natively
                if (useAutomated && mapFile != null && File.Exists(mapFile) && entities.Count > 0)
                {
                    Log(" -> Building binary objects...");
                    try
                    {
                        List<byte[]> objectBlocks = new List<byte[]>();
                        uint baseId = (uint)(0x10000000 + new Random().Next(0, 0x0FFFFFFF));
                        
                        for (int i = 0; i < entities.Count; i++)
                        {
                            uint objectId = baseId + (uint)i;
                            objectBlocks.Add(
                                BinaryPatchUtils.BuildModelObject(
                                    entities[i],
                                    objectId));
                        }

                        MapBinaryInventory patchedInventory =
                            BinaryPatchUtils.PatchModelObjects(
                                mapFile,
                                objectBlocks,
                                Log);
                        Log(
                            $" -> Patched native map: {patchedInventory.ObjectCount} editable " +
                            $"objects in the 0x0076 chain.");
                    }
                    catch (Exception ex)
                    {
                        Log($" -> Failed to patch binary .map: {ex.Message}");
                        continue;
                    }
                }

                // 7. Retail export moves static editor lights out of the object
                // hierarchy and into the embedded SLIs v2 table. Materialize those
                // authoritative records as editable LightObjects. Every retail EXP
                // with SLIs also carries a native local LightObject shell, so no
                // game-wide asset/name inference or 100-MiB template catalog is needed.
                if (useAutomated && mapFile != null && File.Exists(mapFile))
                {
                    try
                    {
                        StaticLightRecoveryUtils.RecoverCompiledStaticLights(
                            mapFile,
                            Log);
                    }
                    catch (Exception ex)
                    {
                        Log($" -> Failed to recover exact SLIs LightObjects: {ex.Message}");
                        continue;
                    }
                }

                Log($" -> Success! {(useAutomated ? "Map ready: " + mapOutputDir : "EDS ready: " + edsOutputDir)}");
            }

            Log("=== ALL OPERATIONS COMPLETE ===");
        }

        private static void CopyLooseLightingFiles(
            string pakPath,
            string mapName,
            string mapOutputDir,
            Action<string> Log)
        {
            string pakDirectory = Path.GetDirectoryName(pakPath);
            if (string.IsNullOrEmpty(pakDirectory))
            {
                Log(" -> Warning: could not resolve the PAK directory for loose lighting.");
                return;
            }

            string looseMapDirectory =
                Path.Combine(pakDirectory, "Data", "Maps", mapName);
            if (!Directory.Exists(looseMapDirectory))
            {
                Log(
                    $" -> No loose runtime-lighting directory found for {mapName} " +
                    $"under {pakDirectory}.");
                return;
            }

            string[] lightingFiles = Directory
                .GetFiles(looseMapDirectory, "*.light", SearchOption.TopDirectoryOnly)
                .Where(path =>
                    Path.GetFileName(path).StartsWith(
                        mapName,
                        StringComparison.OrdinalIgnoreCase))
                .ToArray();

            foreach (string sourcePath in lightingFiles)
            {
                string destinationPath =
                    Path.Combine(mapOutputDir, Path.GetFileName(sourcePath));
                File.Copy(sourcePath, destinationPath, true);
            }

            Log(
                $" -> Copied {lightingFiles.Length} loose .light file(s) " +
                "(compiled static/probe lighting).");

            if (lightingFiles.Length > 0)
            {
                Log(
                    " -> Keep these retail .light files for the first test build. " +
                    "Generating light probes from the reconstructed EXP-based MAP may " +
                    "overwrite them using an incomplete editor-light set.");
            }
        }

        private static void StageEditorResourcePack(
            string pakPath,
            string mapName,
            string outputBasePath,
            bool isWorkshop,
            Action<string> Log)
        {
            string pakDirectory = Path.GetDirectoryName(pakPath);
            if (string.IsNullOrEmpty(pakDirectory))
            {
                Log(
                    " -> Warning: could not resolve the source directory for " +
                    "built-terrain resources.");
                return;
            }

            string sourceResourcePack = Path.Combine(
                pakDirectory,
                "Data",
                $"{mapName}_PC.rpack");
            if (!File.Exists(sourceResourcePack))
            {
                Log(
                    $" -> Warning: built-terrain references exist, but the expected " +
                    $"resource pack was not found: {sourceResourcePack}");
                return;
            }

            if (!isWorkshop)
            {
                Log(
                    $" -> Built-terrain assets are stored in {sourceResourcePack}. " +
                    "Dump directly into a DevTools Workshop project to stage this " +
                    "resource pack for the editor.");
                return;
            }

            string? devToolsDirectory =
                FindAncestorNamed(outputBasePath, "DevTools");
            if (string.IsNullOrEmpty(devToolsDirectory))
            {
                Log(
                    " -> Warning: the output was recognized as Workshop data, but its " +
                    "DevTools root could not be resolved; built-terrain assets were not staged.");
                return;
            }

            string editorDataDirectory =
                Path.Combine(devToolsDirectory, "DW", "Data");
            string destinationResourcePack = Path.Combine(
                editorDataDirectory,
                Path.GetFileName(sourceResourcePack));

            try
            {
                Directory.CreateDirectory(editorDataDirectory);

                if (File.Exists(destinationResourcePack))
                {
                    long sourceLength = new FileInfo(sourceResourcePack).Length;
                    long destinationLength =
                        new FileInfo(destinationResourcePack).Length;

                    if (sourceLength == destinationLength)
                    {
                        Log(
                            $" -> Editor resource pack already staged: " +
                            $"{destinationResourcePack}");
                    }
                    else
                    {
                        Log(
                            $" -> Warning: a different resource pack already exists at " +
                            $"{destinationResourcePack}; it was not overwritten.");
                    }

                    return;
                }

                File.Copy(
                    sourceResourcePack,
                    destinationResourcePack,
                    overwrite: false);
                Log(
                    $" -> Staged editor resource pack for built terrain: " +
                    $"{destinationResourcePack}");
            }
            catch (Exception ex)
            {
                Log(
                    $" -> Warning: failed to stage the built-terrain resource pack: " +
                    $"{ex.Message}");
            }
        }

        private static string? FindAncestorNamed(
            string path,
            string directoryName)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            DirectoryInfo? current = new DirectoryInfo(
                Path.GetFullPath(path));
            while (current != null)
            {
                if (current.Name.Equals(
                    directoryName,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }

            return null;
        }
    }
}
