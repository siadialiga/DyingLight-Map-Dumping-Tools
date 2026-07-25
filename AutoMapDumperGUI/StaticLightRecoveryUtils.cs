using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

// i put my hands on the code, to see if i still think
// and the maps arent dark anymore, i fell kinda freeeeee


namespace AutoMapDumperGUI
{
    public sealed class StaticLightMaterializationResult
    {
        public bool HadStaticLightChunk { get; internal set; }
        public int SourceStaticLightCount { get; internal set; }
        public int AddedLightCount { get; internal set; }
        public int AlreadyRecoveredCount { get; internal set; }
        public int RemovedStaticChunkCount { get; internal set; }
        public MapBinaryInventory Inventory { get; internal set; } = new MapBinaryInventory();
    }

    public static class StaticLightRecoveryUtils
    {
        private const ushort ObjectTag = 0x0076;
        private const ushort ClassTag = 0x0077;
        private const ushort ObjectIdTag = 0x0014;
        private const ushort ObjectNameTag = 0x0016;
        private const ushort GuidTag = 0x007D;
        private const ushort InstanceDataTag = 0x0413;
        private const ushort TagDataTag = 0x00CB;
        private const ushort TransformTag = 0x00DF;
        private const ushort LightDataTag = 0x0130;
        private const ushort StaticLightsTag = 0x0139;
        private const string MarkerPrefix = "__SLI_v2_";

        private sealed class TlvField
        {
            public ushort Tag { get; init; }
            public int DataOffset { get; init; }
            public int DataLength { get; init; }
            public int EndOffset => DataOffset + DataLength;
        }

        private sealed class MapObject
        {
            public int Offset { get; init; }
            public int Length { get; init; }
            public string ClassName { get; init; } = string.Empty;
            public uint ObjectId { get; set; }
            public List<TlvField> Fields { get; } = new List<TlvField>();
        }

        private sealed class StaticLightRecord
        {
            public byte[] LightData { get; init; } = Array.Empty<byte>();
            public byte[] PrimaryMatrix { get; init; } = Array.Empty<byte>();
            public byte[] SecondaryMatrix { get; init; } = Array.Empty<byte>();
            public uint ExtraData { get; init; }
            public string ProjectorTexture { get; init; } = string.Empty;
            public string Marker { get; init; } = string.Empty;
        }

        private sealed class StaticLightChunk
        {
            public int Offset { get; init; }
            public List<StaticLightRecord> Records { get; init; } = new List<StaticLightRecord>();
        }

        public static StaticLightMaterializationResult RecoverCompiledStaticLights(
            string mapFilePath,
            Action<string>? logCallback)
        {
            byte[] sourceData = File.ReadAllBytes(mapFilePath);
            MapBinaryInventory before = BinaryPatchUtils.InspectMapData(sourceData);
            if (!before.HasValidOuterLength)
            {
                throw new InvalidDataException(
                    $"Outer LevelDI length is invalid: declared {before.DeclaredPayloadLength}, " +
                    $"actual {sourceData.Length - 6}.");
            }

            List<StaticLightChunk> chunks = ParseStaticLightChunks(sourceData);
            var result = new StaticLightMaterializationResult
            {
                HadStaticLightChunk = chunks.Count > 0,
                SourceStaticLightCount = chunks.Sum(chunk => chunk.Records.Count),
                Inventory = before
            };
            if (chunks.Count == 0)
                return result;

            List<MapObject> objects = ParseObjects(sourceData);
            MapObject? shell = objects.FirstOrDefault(IsUsableLightShell);
            if (shell == null)
            {
                throw new InvalidDataException(
                    "The EXP contains SLIs records but no native LightObject shell.");
            }

            HashSet<string> existingMarkers = objects
                .Select(obj => ReadObjectName(sourceData, obj))
                .Where(name => name.StartsWith(MarkerPrefix, StringComparison.Ordinal))
                .ToHashSet(StringComparer.Ordinal);
            HashSet<uint> usedObjectIds = objects
                .Where(obj => obj.ObjectId != 0)
                .Select(obj => obj.ObjectId)
                .ToHashSet();
            HashSet<ulong> usedGuids = objects
                .Select(obj => ReadUInt64Field(sourceData, obj, GuidTag))
                .Where(value => value.HasValue)
                .Select(value => value!.Value)
                .ToHashSet();
            HashSet<uint> usedInstanceIds = objects
                .Select(obj => ReadTrailingUInt32Field(sourceData, obj, InstanceDataTag))
                .Where(value => value.HasValue)
                .Select(value => value!.Value)
                .ToHashSet();

            uint nextObjectId = FindInitialId(usedObjectIds);
            uint nextInstanceId = FindInitialId(usedInstanceIds);
            var generatedBlocks = new List<byte[]>();
            var generatedIds = new List<uint>();
            foreach (StaticLightRecord record in chunks.SelectMany(chunk => chunk.Records))
            {
                if (existingMarkers.Contains(record.Marker))
                {
                    result.AlreadyRecoveredCount++;
                    continue;
                }

                uint objectId = TakeNextId(usedObjectIds, ref nextObjectId);
                uint instanceId = TakeNextId(usedInstanceIds, ref nextInstanceId);
                generatedBlocks.Add(BuildLightObject(
                    sourceData,
                    shell,
                    record,
                    objectId,
                    instanceId,
                    usedGuids));
                generatedIds.Add(objectId);
                existingMarkers.Add(record.Marker);
            }

            byte[] patchedData = ReplaceStaticChunks(sourceData, chunks, generatedBlocks);
            MapBinaryInventory after = BinaryPatchUtils.InspectMapData(patchedData);
            if (!after.HasValidOuterLength)
                throw new InvalidDataException("Recovered map has an invalid LevelDI length.");

            Dictionary<uint, MapObject> patchedById = ParseObjects(patchedData)
                .Where(obj => obj.ObjectId != 0)
                .GroupBy(obj => obj.ObjectId)
                .ToDictionary(group => group.Key, group => group.First());
            if (generatedIds.Any(id =>
                    !patchedById.TryGetValue(id, out MapObject? obj) ||
                    obj.ClassName != "LightObject"))
            {
                throw new InvalidDataException(
                    "Static-light verification failed: a generated LightObject is missing.");
            }
            if (ParseStaticLightChunks(patchedData).Count != 0)
            {
                throw new InvalidDataException(
                    "Static-light verification failed: an SLIs chunk remained.");
            }

            result.AddedLightCount = generatedBlocks.Count;
            result.RemovedStaticChunkCount = chunks.Count;
            result.Inventory = after;
            WriteAtomically(mapFilePath, patchedData);
            logCallback?.Invoke(
                $" -> Exact static-light recovery: materialized {result.AddedLightCount} " +
                $"editable LightObject(s) from {result.SourceStaticLightCount} SLIs record(s) " +
                $"and removed {result.RemovedStaticChunkCount} compiled chunk(s) to avoid double lighting.");
            if (result.AlreadyRecoveredCount > 0)
            {
                logCallback?.Invoke(
                    $" -> Static-light recovery: {result.AlreadyRecoveredCount} record(s) " +
                    "already had deterministic editable counterparts.");
            }
            return result;
        }

        private static List<StaticLightChunk> ParseStaticLightChunks(byte[] data)
        {
            var chunks = new List<StaticLightChunk>();
            int offset = 6;
            int globalRecordIndex = 0;
            while (offset <= data.Length - 6)
            {
                ushort tag = BitConverter.ToUInt16(data, offset);
                int blockEnd = GetBlockEnd(data, offset);
                int payloadLength = blockEnd - offset - 6;
                if (tag == StaticLightsTag && payloadLength >= 16)
                {
                    int chunkOffset = offset + 6;
                    string name = Encoding.ASCII.GetString(data, chunkOffset, 4);
                    uint version = BitConverter.ToUInt32(data, chunkOffset + 4);
                    uint chunkLength = BitConverter.ToUInt32(data, chunkOffset + 8);
                    if (name == "SLIs" && version == 2)
                    {
                        if (chunkOffset + 12L + chunkLength != blockEnd)
                            throw new InvalidDataException($"Invalid SLIs length at 0x{offset:X}.");

                        uint recordCount = BitConverter.ToUInt32(data, chunkOffset + 12);
                        int recordOffset = chunkOffset + 16;
                        var records = new List<StaticLightRecord>(checked((int)recordCount));
                        for (uint i = 0; i < recordCount; i++)
                        {
                            if (recordOffset > blockEnd - 138)
                                throw new InvalidDataException($"Truncated SLIs record {i}.");
                            ushort projectorLength = BitConverter.ToUInt16(data, recordOffset + 136);
                            int recordLength = 138 + projectorLength;
                            if (recordOffset + (long)recordLength > blockEnd)
                                throw new InvalidDataException($"Invalid SLIs projector at 0x{recordOffset:X}.");

                            byte[] recordBytes = CopyBytes(data, recordOffset, recordLength);
                            byte[] hash = SHA256.HashData(recordBytes);
                            records.Add(new StaticLightRecord
                            {
                                LightData = CopyBytes(data, recordOffset, 36),
                                PrimaryMatrix = CopyBytes(data, recordOffset + 36, 48),
                                SecondaryMatrix = CopyBytes(data, recordOffset + 84, 48),
                                ExtraData = BitConverter.ToUInt32(data, recordOffset + 132),
                                ProjectorTexture = Encoding.ASCII.GetString(
                                    data,
                                    recordOffset + 138,
                                    projectorLength),
                                Marker = MarkerPrefix + globalRecordIndex.ToString("D6") + "_" +
                                         Convert.ToHexString(hash, 0, 8)
                            });
                            globalRecordIndex++;
                            recordOffset += recordLength;
                        }

                        if (recordOffset != blockEnd)
                            throw new InvalidDataException("SLIs records do not end at chunk EOF.");
                        chunks.Add(new StaticLightChunk { Offset = offset, Records = records });
                    }
                }
                offset = blockEnd;
            }
            return chunks;
        }

        private static List<MapObject> ParseObjects(byte[] data)
        {
            var objects = new List<MapObject>();
            int offset = 6;
            while (offset <= data.Length - 6)
            {
                int blockEnd = GetBlockEnd(data, offset);
                if (BitConverter.ToUInt16(data, offset) == ObjectTag &&
                    offset + 14 <= blockEnd &&
                    BitConverter.ToUInt16(data, offset + 6) == ClassTag)
                {
                    objects.Add(ParseObject(data, offset, blockEnd));
                }
                offset = blockEnd;
            }
            return objects;
        }

        private static MapObject ParseObject(byte[] data, int objectOffset, int blockEnd)
        {
            ushort classLength = BitConverter.ToUInt16(data, objectOffset + 12);
            if (objectOffset + 14L + classLength > blockEnd)
                throw new InvalidDataException($"Invalid class name at 0x{objectOffset:X}.");

            var result = new MapObject
            {
                Offset = objectOffset,
                Length = blockEnd - objectOffset,
                ClassName = Encoding.ASCII.GetString(data, objectOffset + 14, classLength)
            };
            uint objectId = 0;
            int fieldOffset = objectOffset + 6;
            while (fieldOffset <= blockEnd - 6)
            {
                ushort tag = BitConverter.ToUInt16(data, fieldOffset);
                uint length = BitConverter.ToUInt32(data, fieldOffset + 2);
                long fieldEnd = fieldOffset + 6L + length;
                if (fieldEnd > blockEnd)
                    break;
                var field = new TlvField
                {
                    Tag = tag,
                    DataOffset = fieldOffset + 6,
                    DataLength = checked((int)length)
                };
                result.Fields.Add(field);
                if (tag == ObjectIdTag && length == 4)
                    objectId = BitConverter.ToUInt32(data, field.DataOffset);
                fieldOffset = (int)fieldEnd;
            }

            result.ObjectId = objectId;
            return result;
        }

        private static bool IsUsableLightShell(MapObject obj)
        {
            return obj.ClassName == "LightObject" &&
                   obj.Fields.Any(field => field.Tag == ObjectIdTag && field.DataLength == 4) &&
                   obj.Fields.Any(field => field.Tag == TransformTag && field.DataLength == 48) &&
                   obj.Fields.Any(field => field.Tag == LightDataTag);
        }

        private static byte[] BuildLightObject(
            byte[] sourceData,
            MapObject shell,
            StaticLightRecord record,
            uint objectId,
            uint instanceId,
            HashSet<ulong> usedGuids)
        {
            using var payload = new MemoryStream(shell.Length + record.ProjectorTexture.Length + 128);
            bool wroteName = false;
            foreach (TlvField field in shell.Fields)
            {
                byte[] fieldData = CopyBytes(sourceData, field.DataOffset, field.DataLength);
                switch (field.Tag)
                {
                    case ObjectIdTag when field.DataLength == 4:
                        fieldData = BitConverter.GetBytes(objectId);
                        break;
                    case ObjectNameTag:
                        fieldData = EncodeAscii(record.Marker);
                        wroteName = true;
                        break;
                    case GuidTag when field.DataLength == 8:
                        fieldData = BitConverter.GetBytes(TakeNextGuid(usedGuids));
                        break;
                    case InstanceDataTag when field.DataLength >= 4:
                        WriteTrailingUInt32(fieldData, instanceId);
                        break;
                    case TagDataTag when field.DataLength >= 4:
                        Buffer.BlockCopy(
                            RandomNumberGenerator.GetBytes(4),
                            0,
                            fieldData,
                            fieldData.Length - 4,
                            4);
                        break;
                    case TransformTag when field.DataLength == 48:
                        fieldData = record.PrimaryMatrix;
                        break;
                    case LightDataTag:
                        fieldData = BuildLightData(record);
                        break;
                }
                WriteTlv(payload, field.Tag, fieldData);
            }

            if (!wroteName)
                WriteTlv(payload, ObjectNameTag, EncodeAscii(record.Marker));
            byte[] payloadBytes = payload.ToArray();
            using var output = new MemoryStream(payloadBytes.Length + 6);
            WriteTlv(output, ObjectTag, payloadBytes);
            return output.ToArray();
        }

        private static byte[] BuildLightData(StaticLightRecord record)
        {
            using var output = new MemoryStream(256 + record.ProjectorTexture.Length);
            WriteChunk(output, "LDat", 1, record.LightData);

            byte[] projector = Encoding.ASCII.GetBytes(record.ProjectorTexture);
            using var lght = new MemoryStream(158 + projector.Length);
            lght.Write(record.PrimaryMatrix);
            WriteVector3(lght, 0.0f, 0.0f, 0.0f);
            WriteVector3(lght, 1.0f, 1.0f, 1.0f);
            lght.Write(record.SecondaryMatrix);
            WriteVector3(lght, 0.0f, 0.0f, 0.0f);
            WriteVector3(lght, 1.0f, 1.0f, 1.0f);
            lght.Write(BitConverter.GetBytes((ushort)projector.Length));
            lght.Write(projector);
            lght.Write(RandomNumberGenerator.GetBytes(8));
            lght.Write(BitConverter.GetBytes(record.ExtraData));
            WriteChunk(output, "Lght", 1, lght.ToArray());
            return output.ToArray();
        }

        private static byte[] ReplaceStaticChunks(
            byte[] sourceData,
            IReadOnlyCollection<StaticLightChunk> chunks,
            IReadOnlyCollection<byte[]> generatedBlocks)
        {
            HashSet<int> chunkOffsets = chunks.Select(chunk => chunk.Offset).ToHashSet();
            int insertionOffset = FindObjectChainEnd(sourceData);
            int removedLength = chunks.Sum(chunk => GetBlockEnd(sourceData, chunk.Offset) - chunk.Offset);
            int addedLength = generatedBlocks.Sum(block => block.Length);
            using var output = new MemoryStream(sourceData.Length - removedLength + addedLength);
            output.Write(sourceData, 0, 6);
            bool inserted = false;
            int offset = 6;
            while (offset <= sourceData.Length - 6)
            {
                if (!inserted && offset == insertionOffset)
                {
                    foreach (byte[] block in generatedBlocks)
                        output.Write(block);
                    inserted = true;
                }

                int blockEnd = GetBlockEnd(sourceData, offset);
                if (!chunkOffsets.Contains(offset))
                    output.Write(sourceData, offset, blockEnd - offset);
                offset = blockEnd;
            }
            if (!inserted)
            {
                foreach (byte[] block in generatedBlocks)
                    output.Write(block);
            }

            byte[] result = output.ToArray();
            Buffer.BlockCopy(BitConverter.GetBytes((uint)(result.Length - 6)), 0, result, 2, 4);
            return result;
        }

        private static int FindObjectChainEnd(byte[] data)
        {
            int offset = 6;
            bool foundObject = false;
            while (offset <= data.Length - 6)
            {
                ushort tag = BitConverter.ToUInt16(data, offset);
                if (tag == ObjectTag)
                    foundObject = true;
                else if (foundObject)
                    return offset;
                offset = GetBlockEnd(data, offset);
            }
            return data.Length;
        }

        private static int GetBlockEnd(byte[] data, int offset)
        {
            uint payloadLength = BitConverter.ToUInt32(data, offset + 2);
            long end = offset + 6L + payloadLength;
            if (end > data.Length)
                throw new InvalidDataException($"Invalid top-level TLV at 0x{offset:X}.");
            return checked((int)end);
        }

        private static string ReadObjectName(byte[] data, MapObject obj)
        {
            TlvField? field = obj.Fields.FirstOrDefault(item => item.Tag == ObjectNameTag);
            if (field == null || field.DataLength < 2)
                return string.Empty;
            ushort length = BitConverter.ToUInt16(data, field.DataOffset);
            return length <= field.DataLength - 2
                ? Encoding.ASCII.GetString(data, field.DataOffset + 2, length)
                : string.Empty;
        }

        private static ulong? ReadUInt64Field(byte[] data, MapObject obj, ushort tag)
        {
            TlvField? field = obj.Fields.FirstOrDefault(item =>
                item.Tag == tag && item.DataLength == 8);
            return field == null ? null : BitConverter.ToUInt64(data, field.DataOffset);
        }

        private static uint? ReadTrailingUInt32Field(byte[] data, MapObject obj, ushort tag)
        {
            TlvField? field = obj.Fields.FirstOrDefault(item =>
                item.Tag == tag && item.DataLength >= 4);
            return field == null ? null : BitConverter.ToUInt32(data, field.EndOffset - 4);
        }

        private static uint FindInitialId(HashSet<uint> usedIds)
        {
            if (usedIds.Count == 0)
                return 1;
            uint maximum = usedIds.Max();
            return maximum < uint.MaxValue ? maximum + 1 : 1;
        }

        private static uint TakeNextId(HashSet<uint> usedIds, ref uint candidate)
        {
            uint start = candidate;
            do
            {
                if (candidate != 0 && usedIds.Add(candidate))
                {
                    uint result = candidate;
                    candidate = candidate == uint.MaxValue ? 1 : candidate + 1;
                    return result;
                }
                candidate = candidate == uint.MaxValue ? 1 : candidate + 1;
            }
            while (candidate != start);
            throw new InvalidDataException("No free 32-bit ID is available.");
        }

        private static ulong TakeNextGuid(HashSet<ulong> usedGuids)
        {
            while (true)
            {
                ulong guid = BitConverter.ToUInt64(RandomNumberGenerator.GetBytes(8));
                if (guid != 0 && usedGuids.Add(guid))
                    return guid;
            }
        }

        private static byte[] CopyBytes(byte[] source, int offset, int length)
        {
            byte[] result = new byte[length];
            Buffer.BlockCopy(source, offset, result, 0, length);
            return result;
        }

        private static byte[] EncodeAscii(string value)
        {
            byte[] text = Encoding.ASCII.GetBytes(value);
            byte[] result = new byte[text.Length + 2];
            Buffer.BlockCopy(BitConverter.GetBytes((ushort)text.Length), 0, result, 0, 2);
            Buffer.BlockCopy(text, 0, result, 2, text.Length);
            return result;
        }

        private static void WriteTrailingUInt32(byte[] data, uint value)
        {
            Buffer.BlockCopy(BitConverter.GetBytes(value), 0, data, data.Length - 4, 4);
        }

        private static void WriteTlv(Stream output, ushort tag, byte[] payload)
        {
            output.Write(BitConverter.GetBytes(tag));
            output.Write(BitConverter.GetBytes((uint)payload.Length));
            output.Write(payload);
        }

        private static void WriteChunk(Stream output, string name, uint version, byte[] payload)
        {
            byte[] chunkName = Encoding.ASCII.GetBytes(name);
            output.Write(chunkName);
            output.Write(BitConverter.GetBytes(version));
            output.Write(BitConverter.GetBytes((uint)payload.Length));
            output.Write(payload);
        }

        private static void WriteVector3(Stream output, float x, float y, float z)
        {
            output.Write(BitConverter.GetBytes(x));
            output.Write(BitConverter.GetBytes(y));
            output.Write(BitConverter.GetBytes(z));
        }

        private static void WriteAtomically(string mapFilePath, byte[] data)
        {
            string tempPath = mapFilePath + ".static-light-recovery";
            try
            {
                File.WriteAllBytes(tempPath, data);
                File.Move(tempPath, mapFilePath, true);
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }
    }
}
