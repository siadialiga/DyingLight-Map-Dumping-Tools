using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AutoMapDumperGUI
{
    public sealed class MapBinaryInventory
    {
        public long FileLength { get; internal set; }
        public uint DeclaredPayloadLength { get; internal set; }
        public int FirstObjectOffset { get; internal set; } = -1;
        public int Type76EndOffset { get; internal set; } = -1;
        public int TopLevelBlockCount { get; internal set; }
        public int ObjectCount { get; internal set; }
        public Dictionary<string, int> Classes { get; } = new Dictionary<string, int>(StringComparer.Ordinal);

        public bool HasValidOuterLength =>
            FileLength >= 6 && DeclaredPayloadLength == FileLength - 6;

        public int Count(string className) =>
            Classes.TryGetValue(className, out int count) ? count : 0;

        public int TopLevelLightCount =>
            Count("LightObject") + Count("LightObjectWithEffect");
    }

    public static class BinaryPatchUtils
    {
        public static float[] ParseVector3(string val)
        {
            var parts = val.Trim('<', '>').Split(',');
            return new float[] { float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]) };
        }

        public static float[] ParseColor(string val)
        {
            var parts = val.Trim('<', '>').Split(',');
            return new float[] { 
                float.Parse(parts[0]) / 255f, 
                float.Parse(parts[1]) / 255f, 
                float.Parse(parts[2]) / 255f, 
                float.Parse(parts[3]) / 255f 
            };
        }

        public static byte[] CreateTransformMatrix(float[] rot, float[] scale, float[] pos)
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

        public static void WriteTLV(List<byte> buf, ushort tag, byte[] data)
        {
            buf.AddRange(BitConverter.GetBytes(tag));
            buf.AddRange(BitConverter.GetBytes((uint)data.Length));
            buf.AddRange(data);
        }

        public static MapBinaryInventory InspectMapFile(string mapFilePath)
        {
            return InspectMapData(File.ReadAllBytes(mapFilePath));
        }

        public static MapBinaryInventory InspectMapData(byte[] fileData)
        {
            if (fileData == null)
                throw new ArgumentNullException(nameof(fileData));
            if (fileData.Length < 14)
                throw new InvalidDataException("Map binary is too small.");

            var inventory = new MapBinaryInventory
            {
                FileLength = fileData.LongLength,
                DeclaredPayloadLength = BitConverter.ToUInt32(fileData, 2),
                TopLevelBlockCount = ValidateTopLevelTlvStream(fileData)
            };

            int firstRootIndex = FindFirstType76Object(fileData);
            inventory.FirstObjectOffset = firstRootIndex;
            if (firstRootIndex < 0)
                return inventory;

            int currentIndex = firstRootIndex;
            while (currentIndex <= fileData.Length - 14 &&
                   fileData[currentIndex] == 0x76 &&
                   fileData[currentIndex + 1] == 0x00)
            {
                uint blockSize = BitConverter.ToUInt32(fileData, currentIndex + 2);
                long blockEnd = currentIndex + 6L + blockSize;
                if (blockSize < 8 || blockEnd > fileData.Length)
                {
                    throw new InvalidDataException(
                        $"Invalid 0x0076 object size {blockSize} at 0x{currentIndex:X}.");
                }

                string className = ReadClassName(fileData, currentIndex, (int)blockEnd);
                if (!inventory.Classes.ContainsKey(className))
                    inventory.Classes[className] = 0;
                inventory.Classes[className]++;
                inventory.ObjectCount++;

                currentIndex = (int)blockEnd;
            }

            inventory.Type76EndOffset = currentIndex;
            return inventory;
        }

        private static int ValidateTopLevelTlvStream(byte[] fileData)
        {
            int blockCount = 0;
            int currentIndex = 6;

            while (currentIndex < fileData.Length)
            {
                if (currentIndex > fileData.Length - 6)
                {
                    throw new InvalidDataException(
                        $"Truncated top-level TLV header at 0x{currentIndex:X}.");
                }

                uint blockSize = BitConverter.ToUInt32(fileData, currentIndex + 2);
                long blockEnd = currentIndex + 6L + blockSize;
                if (blockEnd > fileData.Length)
                {
                    ushort tag = BitConverter.ToUInt16(fileData, currentIndex);
                    throw new InvalidDataException(
                        $"Top-level tag 0x{tag:X4} at 0x{currentIndex:X} extends " +
                        "past the end of the file.");
                }

                currentIndex = (int)blockEnd;
                blockCount++;
            }

            if (currentIndex != fileData.Length)
                throw new InvalidDataException("Top-level TLV stream does not end at EOF.");

            return blockCount;
        }

        private static int FindFirstType76Object(byte[] fileData)
        {
            for (int i = 0; i <= fileData.Length - 14; i++)
            {
                if (fileData[i] == 0x76 &&
                    fileData[i + 1] == 0x00 &&
                    fileData[i + 6] == 0x77 &&
                    fileData[i + 7] == 0x00)
                {
                    uint blockSize = BitConverter.ToUInt32(fileData, i + 2);
                    long blockEnd = i + 6L + blockSize;
                    if (blockSize >= 8 && blockEnd <= fileData.Length)
                        return i;
                }
            }

            return -1;
        }

        private static string ReadClassName(byte[] fileData, int objectOffset, int blockEnd)
        {
            if (objectOffset + 14 > blockEnd ||
                fileData[objectOffset + 6] != 0x77 ||
                fileData[objectOffset + 7] != 0x00)
            {
                throw new InvalidDataException(
                    $"Object at 0x{objectOffset:X} does not start with a class-name block.");
            }

            uint classBlockSize = BitConverter.ToUInt32(fileData, objectOffset + 8);
            ushort classNameLength = BitConverter.ToUInt16(fileData, objectOffset + 12);
            if (classBlockSize != classNameLength + 2 ||
                classNameLength == 0 ||
                objectOffset + 14L + classNameLength > blockEnd)
            {
                throw new InvalidDataException(
                    $"Invalid class-name block at 0x{objectOffset:X}.");
            }

            return Encoding.ASCII.GetString(fileData, objectOffset + 14, classNameLength);
        }

        public static MapBinaryInventory PatchModelObjects(
            string mapFilePath,
            IReadOnlyCollection<byte[]> objectBlocks,
            Action<string> logCallback)
        {
            if (objectBlocks == null)
                throw new ArgumentNullException(nameof(objectBlocks));
            if (objectBlocks.Count == 0)
                return InspectMapFile(mapFilePath);

            byte[] sourceData = File.ReadAllBytes(mapFilePath);
            MapBinaryInventory before = InspectMapData(sourceData);
            if (!before.HasValidOuterLength)
            {
                throw new InvalidDataException(
                    $"Outer LevelDI length is invalid: declared {before.DeclaredPayloadLength}, " +
                    $"actual {sourceData.Length - 6}.");
            }
            if (before.FirstObjectOffset < 0 || before.Type76EndOffset < 0)
                throw new InvalidDataException("No editable 0x0076 object chain was found.");

            int insertLength = objectBlocks.Sum(block => block?.Length ?? 0);
            if (insertLength <= 0)
                throw new InvalidDataException("Generated object payload is empty.");

            foreach (byte[] block in objectBlocks)
            {
                if (block == null || block.Length < 14)
                    throw new InvalidDataException("A generated object block is empty or truncated.");

                MapBinaryInventory generatedInventory = InspectMapData(
                    BuildMinimalMapEnvelope(block));
                if (generatedInventory.ObjectCount != 1 ||
                    generatedInventory.Count("ModelObject") != 1)
                {
                    throw new InvalidDataException("A generated block is not one valid ModelObject.");
                }
            }

            byte[] patchedData = new byte[sourceData.Length + insertLength];
            int injectionPoint = before.Type76EndOffset;
            Buffer.BlockCopy(sourceData, 0, patchedData, 0, injectionPoint);

            int writeOffset = injectionPoint;
            foreach (byte[] block in objectBlocks)
            {
                Buffer.BlockCopy(block, 0, patchedData, writeOffset, block.Length);
                writeOffset += block.Length;
            }

            Buffer.BlockCopy(
                sourceData,
                injectionPoint,
                patchedData,
                writeOffset,
                sourceData.Length - injectionPoint);

            byte[] newSizeBytes = BitConverter.GetBytes(checked((uint)(patchedData.Length - 6)));
            Buffer.BlockCopy(newSizeBytes, 0, patchedData, 2, newSizeBytes.Length);

            VerifySourceBytesWerePreserved(sourceData, patchedData, injectionPoint, insertLength);

            MapBinaryInventory after = InspectMapData(patchedData);
            if (!after.HasValidOuterLength)
                throw new InvalidDataException("Patched map has an invalid outer LevelDI length.");
            if (after.TopLevelLightCount != before.TopLevelLightCount)
            {
                throw new InvalidDataException(
                    $"Top-level exported lights changed during patching: " +
                    $"{before.TopLevelLightCount} -> {after.TopLevelLightCount}.");
            }
            if (after.Count("ModelObject") != before.Count("ModelObject") + objectBlocks.Count)
            {
                throw new InvalidDataException(
                    $"ModelObject verification failed: expected " +
                    $"{before.Count("ModelObject") + objectBlocks.Count}, " +
                    $"found {after.Count("ModelObject")}.");
            }
            if (after.TopLevelBlockCount != before.TopLevelBlockCount + objectBlocks.Count)
            {
                throw new InvalidDataException(
                    $"Top-level TLV verification failed: expected " +
                    $"{before.TopLevelBlockCount + objectBlocks.Count}, " +
                    $"found {after.TopLevelBlockCount}.");
            }

            string tempPath = mapFilePath + ".patching";
            try
            {
                File.WriteAllBytes(tempPath, patchedData);
                File.Move(tempPath, mapFilePath, true);
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }

            logCallback?.Invoke(
                $" -> Binary verification passed: preserved {after.TopLevelLightCount} top-level " +
                $"exported light record(s), added {objectBlocks.Count} ModelObject(s).");
            return after;
        }

        private static byte[] BuildMinimalMapEnvelope(byte[] objectBlock)
        {
            byte[] envelope = new byte[6 + objectBlock.Length];
            Buffer.BlockCopy(BitConverter.GetBytes((uint)objectBlock.Length), 0, envelope, 2, 4);
            Buffer.BlockCopy(objectBlock, 0, envelope, 6, objectBlock.Length);
            return envelope;
        }

        private static void VerifySourceBytesWerePreserved(
            byte[] sourceData,
            byte[] patchedData,
            int injectionPoint,
            int insertLength)
        {
            for (int i = 0; i < injectionPoint; i++)
            {
                // Bytes 2..5 are the outer payload length and are intentionally changed.
                if (i >= 2 && i <= 5)
                    continue;
                if (sourceData[i] != patchedData[i])
                    throw new InvalidDataException($"Source byte changed unexpectedly at 0x{i:X}.");
            }

            for (int i = injectionPoint; i < sourceData.Length; i++)
            {
                if (sourceData[i] != patchedData[i + insertLength])
                    throw new InvalidDataException($"Source tail changed unexpectedly at 0x{i:X}.");
            }
        }

        public static byte[] BuildModelObject(ModelEntity ent, uint objId)
        {
            if (ent == null)
                throw new ArgumentNullException(nameof(ent));
            if (string.IsNullOrWhiteSpace(ent.MeshName))
                throw new InvalidDataException(
                    "Cannot build a ModelObject without a MeshName.");

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

        public static void PatchMapFile(string mapFilePath, string edsFileName, Action<string> logCallback)
        {
            byte[] fileData = File.ReadAllBytes(mapFilePath);
            
            int firstRootIndex = -1;
            for (int i = 0; i <= fileData.Length - 8; i++)
            {
                if (fileData[i] == 0x76 && fileData[i+1] == 0x00 && fileData[i+6] == 0x77 && fileData[i+7] == 0x00)
                {
                    firstRootIndex = i;
                    break;
                }
            }

            int injectionPoint = -1;
            if (firstRootIndex != -1)
            {
                int currentIndex = firstRootIndex;
                while (currentIndex < fileData.Length - 6)
                {
                    if (fileData[currentIndex] != 0x76 || fileData[currentIndex + 1] != 0x00)
                    {
                        injectionPoint = currentIndex;
                        break;
                    }
                    uint size = BitConverter.ToUInt32(fileData, currentIndex + 2);
                    currentIndex += 6 + (int)size;
                }
                if (currentIndex >= fileData.Length) injectionPoint = fileData.Length;
            }
            
            if (injectionPoint == -1)
            {
                logCallback?.Invoke(" -> Warning: Root object pattern not found in .map file. Skipping binary patch.");
                return;
            }
            
            byte[] templateBlock = new byte[] {
                0x76, 0x00, 0x1F, 0x01, 0x00, 0x00, 0x77, 0x00, 0x11, 0x00, 0x00, 0x00, 0x0F, 0x00, 0x53, 0x65,
                0x6C, 0x65, 0x63, 0x74, 0x69, 0x6F, 0x6E, 0x4F, 0x62, 0x6A, 0x65, 0x63, 0x74, 0x3D, 0x00, 0x04,
                0x00, 0x00, 0x00, 0x04, 0x40, 0x00, 0x00, 0x5A, 0x14, 0x18, 0x00, 0x00, 0x00, 0x90, 0x01, 0x00,
                0x00, 0x2C, 0x01, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x13, 0x04, 0x1A, 0x00, 0x00, 0x00, 0x80, 0x02, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x0B, 0x00, 0x00, 0x00, 0x2F, 0x01, 0x35, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01,
                0x01, 0x01, 0x00, 0x00, 0x00
            };
            
            byte[] templateRest = new byte[] {
                0x01, 0x00, 0x00, 0x00, 0x3A, 0x00, 0x18, 0x00, 0x00, 0x00, 0xC2, 0xF6, 0x2A, 0xBF, 0x00, 0x00,
                0x00, 0x00, 0xE0, 0xE8, 0xE9, 0xBD, 0xBA, 0xAC, 0x2F, 0x3F, 0x27, 0xD8, 0xC8, 0x3F, 0xA8, 0x11,
                0x91, 0x3F, 0xDF, 0x00, 0x30, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x3F, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x3F,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x80, 0x3F, 0x00, 0x00, 0x00, 0x00, 0xBE, 0x00, 0x08, 0x00, 0x00, 0x00, 0x68, 0x20,
                0x00, 0x40, 0x00, 0x00, 0x00, 0x00, 0x14, 0x00, 0x04, 0x00, 0x00, 0x00, 0x27, 0x00, 0x00, 0x00,
                0x16, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x79, 0x00, 0x01, 0x00, 0x00, 0x00, 0xFF, 0x7C,
                0x00, 0x04, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF
            };
            
            byte[] edsNameBytes = System.Text.Encoding.ASCII.GetBytes(edsFileName);
            byte[] edsNameLenBytes = BitConverter.GetBytes((ushort)edsNameBytes.Length);
            
            uint totalSize = (uint)(templateBlock.Length + edsNameLenBytes.Length + edsNameBytes.Length + templateRest.Length - 6);
            byte[] sizeBytes = BitConverter.GetBytes(totalSize);
            templateBlock[2] = sizeBytes[0];
            templateBlock[3] = sizeBytes[1];
            templateBlock[4] = sizeBytes[2];
            templateBlock[5] = sizeBytes[3];

            System.Collections.Generic.List<byte> insertBlock = new System.Collections.Generic.List<byte>();
            insertBlock.AddRange(templateBlock);
            insertBlock.AddRange(edsNameLenBytes);
            insertBlock.AddRange(edsNameBytes);
            insertBlock.AddRange(templateRest);
            
            byte[] insertBlockArr = insertBlock.ToArray();
            
            uint origSize = BitConverter.ToUInt32(fileData, 2);
            byte[] newSizeBytes = BitConverter.GetBytes(origSize + (uint)insertBlockArr.Length);
            Array.Copy(newSizeBytes, 0, fileData, 2, 4);
            
            byte[] patchedData = new byte[fileData.Length + insertBlockArr.Length];
            Array.Copy(fileData, 0, patchedData, 0, injectionPoint);
            Array.Copy(insertBlockArr, 0, patchedData, injectionPoint, insertBlockArr.Length);
            Array.Copy(fileData, injectionPoint, patchedData, injectionPoint + insertBlockArr.Length, fileData.Length - injectionPoint);
            
            File.WriteAllBytes(mapFilePath, patchedData);
            logCallback?.Invoke($" -> Patched binary .map successfully (SelectionObject): {Path.GetFileName(mapFilePath)}");
        }

        public static void PatchMisFile(string misFilePath, string edsFileName, Action<string> logCallback)
        {
            string selectionObjectBlock = $"\r\nSelectionObject{{SelectionObject}}\r\n" +
                                          $"\tworld_position = <0, 0, 0>\r\n" +
                                          $"\tworld_dir = <0, 0, 1>\r\n" +
                                          $"\tlocal_scale = <1, 1, 1>\r\n" +
                                          $"\tID\t=\t39\r\n" +
                                          $"\tlocal ID\t=\t11\r\n" +
                                          $"\tSeed\t=\t0\r\n" +
                                          $"\tEds_table\t=\t\r\n" +
                                          $"\t\t{edsFileName}\t=\t1\r\n";
                                          
            File.AppendAllText(misFilePath, selectionObjectBlock);
            logCallback?.Invoke($" -> Patched text .mis successfully: {Path.GetFileName(misFilePath)}");
        }
    }
}
