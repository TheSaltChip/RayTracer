using System;
using System.Runtime.InteropServices;
using UnityEngine;

public static class Structs
{
    [StructLayout(LayoutKind.Sequential), Serializable]
    public struct Sphere
    {
        public Vector3 center;
        public float radius;
        public Mat mat;

        public Sphere(Vector3 center, float radius, Mat mat)
        {
            this.center = center;
            this.radius = radius;
            this.mat = mat;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Triangle
    {
        public Vector3 posA, posB, posC;
        public Vector3 normalA, normalB, normalC;

        public Triangle(Vector3 posA, Vector3 posB, Vector3 posC, Vector3 normalA, Vector3 normalB, Vector3 normalC)
        {
            this.posA = posA;
            this.posB = posB;
            this.posC = posC;
            this.normalA = normalA;
            this.normalB = normalB;
            this.normalC = normalC;
        }
    }

    [StructLayout(LayoutKind.Sequential), Serializable]
    public struct Mat
    {
        [Range(0, 2)] public uint type;
        public Color color;
        [Min(0)] public float fuzz;
        [Min(0)] public float ref_idx;
        public Color emissionColor;
        [Min(0)] public float emissionStrength;
    }

    [StructLayout(LayoutKind.Sequential), Serializable]
    public struct MeshInfo
    {
        [NonSerialized] public int firstTriangleIndex;
        [NonSerialized] public int numTriangles;
        [NonSerialized] public Vector3 boundsMin;
        [NonSerialized] public Vector3 boundsMax;
        public Mat material;

        public MeshInfo(int firstTriangleIndex, int numTriangles, Vector3 boundsMin, Vector3 boundsMax, Mat material)
        {
            this.firstTriangleIndex = firstTriangleIndex;
            this.numTriangles = numTriangles;
            this.boundsMin = boundsMin;
            this.boundsMax = boundsMax;
            this.material = material;
        }

        public override string ToString()
        {
            return
                $"{nameof(firstTriangleIndex)}: {firstTriangleIndex}, {nameof(numTriangles)}: {numTriangles}, {nameof(boundsMin)}: {boundsMin}, {nameof(boundsMax)}: {boundsMax}, {nameof(material)}: {material}";
        }
    }

    [StructLayout(LayoutKind.Sequential), Serializable]
    public struct Rect
    {
        [Range(0, 2)] public int orientation;
        [NonSerialized] public Vector3 pos0;
        [NonSerialized] public Vector3 pos1;
        [NonSerialized] public float k;
        [NonSerialized] public Vector3 sinRotation;
        [NonSerialized] public Vector3 cosRotation;
        [NonSerialized] public Vector3 offset;
        [NonSerialized] public float scale;
        public Mat material;
    }
}