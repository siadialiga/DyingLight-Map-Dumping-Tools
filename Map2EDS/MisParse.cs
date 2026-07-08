using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Security.Claims;
using static Util;

public class MisParse
{
    public class Entity // Changed from struct to class
    {
        public string Class { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }
        public Vector3 Scale { get; set; }
        public string MeshName { get; set; }
        public string SkinName { get; set; }
        public Rgba Color0 { get; set; }
        public Rgba Color1 { get; set; }

        public int Seed { get; set; }
        public Int64 Required_Tags { get; set; }
        public Int64 Forbidden_Tags { get; set; }
    }

    public List<Entity> Entitys { get; set; } = new List<Entity>();

    public void Deserialize(Stream input)
    {
        using (var reader = new StreamReader(input))
        {
            string line;
            Entity currentEntity = null;

            while ((line = reader.ReadLine()) != null)
            {
                if (line.StartsWith("Class"))
                {
                    currentEntity = new Entity();
                    currentEntity.Class = ParseStringValue(line);
                    Entitys.Add(currentEntity);
                }
                else if (line.StartsWith("Position") && currentEntity != null)
                {
                    currentEntity.Position = ParseVector3(line);
                }
                else if (line.StartsWith("Rotation") && currentEntity != null)
                {
                    currentEntity.Rotation = ParseVector3(line);
                }
                else if (line.StartsWith("Scale") && currentEntity != null)
                {
                    currentEntity.Scale = ParseVector3(line);
                }
                else if (line.StartsWith("MeshName") && currentEntity != null)
                {
                    currentEntity.MeshName = ParseStringValue(line);
                }
                else if (line.StartsWith("SkinName") && currentEntity != null)
                {
                    currentEntity.SkinName = ParseStringValue(line);
                }
                else if (line.StartsWith("Color0") && currentEntity != null)
                {
                    currentEntity.Color0 = ParseRgba(line);
                }
                else if (line.StartsWith("Color1") && currentEntity != null)
                {
                    currentEntity.Color1 = ParseRgba(line);
                }
                else if (line.StartsWith("Seed") && currentEntity != null)
                {
                    currentEntity.Seed = ParseInt(line);
                }
                else if (line.StartsWith("required_tags") && currentEntity != null)
                {
                    currentEntity.Required_Tags = ParseInt64(line);
                }
                else if (line.StartsWith("forbidden_tags") && currentEntity != null)
                {
                    currentEntity.Forbidden_Tags = ParseInt64(line);
                }
            }
        }
    }
    private int ParseInt(string line)
    {
        var valueString = line.Split('=')[1].Trim();
        if (int.TryParse(valueString, out int result))
        {
            return result;
        }
        return 0; // Return a default value if parsing fails
    }

    private long ParseInt64(string line)
    {
        var valueString = line.Split('=')[1].Trim();
        if (long.TryParse(valueString, out long result))
        {
            return result;
        }
        return 0L; //parse fail default
    }

    private string ParseStringValue(string line)
    {
        return line.Split('=')[1].Trim();
    }

    private Vector3 ParseVector3(string line)
    {
        var vectorString = line.Split('=')[1].Trim().Trim('<', '>');
        var values = vectorString.Split(',');
        if (values.Length == 3 &&
            float.TryParse(values[0], out float x) &&
            float.TryParse(values[1], out float y) &&
            float.TryParse(values[2], out float z))
        {
            return new Vector3(x, y, z);
        }
        return new Vector3(0, 0, 0); //parse fail default
    }

    private Rgba ParseRgba(string line)
    {
        var vectorString = line.Split('=')[1].Trim().Trim('<', '>');
        var values = vectorString.Split(',');

        if (values.Length == 4 &&
            float.TryParse(values[0], out float r) &&
            float.TryParse(values[1], out float g) &&
            float.TryParse(values[2], out float b) &&
            float.TryParse(values[3], out float a))
        {
            // Normalize decimal RGBA values
            return new Rgba
            {
                R = r / 255.0f,
                G = g / 255.0f,
                B = b / 255.0f,
                A = a / 255.0f
            };
        }

        // Default to opaque black if parsing fails
        return new Rgba
        {
            R = 0,
            G = 0,
            B = 0,
            A = 1
        };
    }
}
