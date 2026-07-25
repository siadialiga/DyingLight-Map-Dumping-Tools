using System;

namespace AutoMapDumperGUI
{
    public class ModelEntity
    {
        public float[] Position = new float[3] { 0, 0, 0 };
        public float[] Rotation = new float[3] { 0, 0, 0 };
        public float[] Scale = new float[3] { 1, 1, 1 };
        public string MeshName = "";
        public string SkinName = "";
        public float[] Color0 = new float[4] { 1, 1, 1, 1 };
        public float[] Color1 = new float[4] { 0, 0, 0, 0 };
        public long RequiredTags = 0;
        public long ForbiddenTags = 0;
        public uint Seed = 0;
    }
}
