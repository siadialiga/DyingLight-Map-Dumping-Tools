using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Map2EDS.Program;
using System.Numerics;

public static class Util
{
    public struct Rgba
    {
        public float R;
        public float G;
        public float B;
        public float A;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct EntityTransformMatrix //row major
    {
        public float m11;
        public float m12;
        public float m13;
        public float posX;

        public float m21;
        public float m22;
        public float m23;
        public float posY;

        public float m31;
        public float m32;
        public float m33;
        public float posZ;
    }
    public static EntityTransformMatrix CreateTransformMatrix(Vector3 rotation, Vector3 scale)
    {
        // Convert degrees to radians
        float radX = rotation.X * (float)Math.PI / 180f;
        float radY = rotation.Y * (float)Math.PI / 180f;
        float radZ = rotation.Z * (float)Math.PI / 180f;

        // Assuming rotation is in radians for X, Y, Z Euler angles.
        float cosX = (float)Math.Cos(radX);
        float sinX = (float)Math.Sin(radX);

        float cosY = (float)Math.Cos(radY);
        float sinY = (float)Math.Sin(radY);

        float cosZ = (float)Math.Cos(radZ);
        float sinZ = (float)Math.Sin(radZ);

        // Rotation matrix (Euler XYZ order)
        float m11 = cosY * cosZ;
        float m12 = -cosY * sinZ;
        float m13 = sinY;

        float m21 = (cosX * sinZ + sinX * sinY * cosZ);
        float m22 = (cosX * cosZ - sinX * sinY * sinZ);
        float m23 = -sinX * cosY;

        float m31 = (sinX * sinZ - cosX * sinY * cosZ);
        float m32 = (sinX * cosZ + cosX * sinY * sinZ);
        float m33 = cosX * cosY;

        // Apply scaling to the rotation matrix
        m11 *= scale.X;
        m12 *= scale.Y;
        m13 *= scale.Z;

        m21 *= scale.X;
        m22 *= scale.Y;
        m23 *= scale.Z;

        m31 *= scale.X;
        m32 *= scale.Y;
        m33 *= scale.Z;

        // Return the resulting EntityTransformMatrix
        return new EntityTransformMatrix
        {
            m11 = m11,
            m12 = m12,
            m13 = m13,
            m21 = m21,
            m22 = m22,
            m23 = m23,
            m31 = m31,
            m32 = m32,
            m33 = m33
        };
    }

    static public void WriteTransform(Stream stream, EntityTransformMatrix Transform)
    {
        var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

        // Write each field of the ObjectTransform struct
        writer.Write(Transform.m11 == 0.0f ? 0.0f : Transform.m11);
        writer.Write(Transform.m12 == 0.0f ? 0.0f : Transform.m12);
        writer.Write(Transform.m13 == 0.0f ? 0.0f : Transform.m13);
        writer.Write(Transform.posX == 0.0f ? 0.0f : Transform.posX);

        writer.Write(Transform.m21 == 0.0f ? 0.0f : Transform.m21);
        writer.Write(Transform.m22 == 0.0f ? 0.0f : Transform.m22);
        writer.Write(Transform.m23 == 0.0f ? 0.0f : Transform.m23);
        writer.Write(Transform.posX == 0.0f ? 0.0f : Transform.posY);

        writer.Write(Transform.m31 == 0.0f ? 0.0f : Transform.m31);
        writer.Write(Transform.m32 == 0.0f ? 0.0f : Transform.m32);
        writer.Write(Transform.m33 == 0.0f ? 0.0f : Transform.m33);
        writer.Write(Transform.posZ == 0.0f ? 0.0f : Transform.posZ);

        writer.Flush(); // Ensure data is written to the stream
    }


    /*
    //Testing function to make sure I know what I'm doing (I don't)
    static public void convert()
    {
        float R11 = 8.660254f;
        float R12 = 1.606969f;
        float R13 = 3.064178f;

        float R21 = 0f;
        float R22 = 3.830222f;
        float R23 = -5.142301f;

        float R31 = -5f;
        float R32 = 2.783352f;
        float R33 = 5.307312f;
        // Normalize the rotation matrix (to remove scale)


        float scaleX = (float)Math.Sqrt(R11 * R11 + R21 * R21 + R31 * R31);
        float scaleY = (float)Math.Sqrt(R12 * R12 + R22 * R22 + R32 * R32);
        float scaleZ = (float)Math.Sqrt(R13 * R13 + R23 * R23 + R33 * R33);

        R11 /= scaleX; R12 /= scaleY; R13 /= scaleZ;
        R21 /= scaleX; R22 /= scaleY; R23 /= scaleZ;
        R31 /= scaleX; R32 /= scaleY; R33 /= scaleZ;

        double roll, pitch, yaw;
        // Handle gimbal lock
        if (Math.Abs(R31) >= 1.0)
        {
            pitch = R31 > 0 ? -Math.PI / 2 : Math.PI / 2;
            yaw = Math.Atan2(-R12, R22);
            roll = 0;
        }
        else
        {
            yaw = Math.Atan2(R21, R11);
            pitch = Math.Asin(R31);
            roll = Math.Atan2(R32, R33);
        }


        roll = roll * (180.0 / Math.PI);
        pitch = pitch * (180.0 / Math.PI);
        yaw = yaw * (180.0 / Math.PI);

        Console.WriteLine($"scaleX: {scaleX}");
        Console.WriteLine($"scaleY: {scaleY}");
        Console.WriteLine($"scaleZ: {scaleZ}");

        Console.WriteLine($"Pitch: {pitch}");
        Console.WriteLine($"Roll: {roll}");
        Console.WriteLine($"Yaw: {yaw}");
    }
    */

    static public void WriteBytes(Stream stream, byte[] hexBytes)
    {
        var writer = new BinaryWriter(stream, System.Text.Encoding.Default, leaveOpen: true);
        writer.Write(hexBytes);
        writer.Flush();
    }
    static public void WriteFloat(Stream stream, float value)
    {
        var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        writer.Write(value);
        writer.Flush();
    }
    static public void WriteInt(Stream stream, int value)
    {
        var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        writer.Write(value);
        writer.Flush();
    }
    static public void WriteInt64(Stream stream, Int64 value)
    {
        var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        writer.Write(value);
        writer.Flush();
    }

    static public void WriteString(Stream stream, string text, bool isLong = false, bool includeNullTerminator = false)
    {
        var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

        // Convert the string to bytes
        byte[] stringBytes = Encoding.UTF8.GetBytes(text);

        // length + 1 if including a null terminator
        int lengthToWrite = stringBytes.Length + (includeNullTerminator ? 1 : 0);

        // Write the length of the string
        if (isLong)
            writer.Write(lengthToWrite);    // Write as 4-byte integer
        else
            writer.Write((short)lengthToWrite); // Write as 2-byte short

        // Write the string bytes
        writer.Write(stringBytes);


        if (includeNullTerminator)
        {
            writer.Write((byte)0x00);
        }

        writer.Flush();
    }

    static public void WriteRGBA(Stream stream, Rgba rgba)
    {
        var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

        // Write RGBA Values
        writer.Write(rgba.R);
        writer.Write(rgba.G);
        writer.Write(rgba.B);
        writer.Write(rgba.A);
    }
}