using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DataTypes
{
    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct Sphere
    {
        public Vector3 center;
        public float radius;
        public RayTracingMaterial rayTracingMaterial;

        public Sphere(Vector3 center, float radius, RayTracingMaterial rayTracingMaterial)
        {
            this.center = center;
            this.radius = radius;
            this.rayTracingMaterial = rayTracingMaterial;
        }
    }

    [Serializable, StructLayout(LayoutKind.Sequential)]
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

    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct RayTracingMaterial
    {
        [Range(0, 2)] public uint type;
        public Color color;
        [Min(0)] public float fuzz;
        [Min(0)] public float refIdx;
        public Color emissionColor;
        [Min(0)] public float emissionStrength;
    }

    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct MeshInfo
    {
        [HideInInspector] public int firstTriangleIndex;
        [HideInInspector] public int numTriangles;
        [HideInInspector] public Vector3 boundsMin;
        [HideInInspector] public Vector3 boundsMax;
        public RayTracingMaterial material;

        public MeshInfo(int firstTriangleIndex, int numTriangles, Vector3 boundsMin, Vector3 boundsMax,
            RayTracingMaterial material)
        {
            this.firstTriangleIndex = firstTriangleIndex;
            this.numTriangles = numTriangles;
            this.boundsMin = boundsMin;
            this.boundsMax = boundsMax;
            this.material = material;
        }
    }

    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct MeshChunk
    {
        public Triangle[] triangles;
        public Bounds bounds;
        public int subMeshIndex;

        public MeshChunk(Triangle[] triangles, Bounds bounds, int subMeshIndex)
        {
            this.triangles = triangles;
            this.bounds = bounds;
            this.subMeshIndex = subMeshIndex;
        }
    }

    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct Rect
    {
        [HideInInspector] public Vector3 minPos;
        [HideInInspector] public Vector3 maxPos;
        [HideInInspector] public Matrix4x4 rotation;
        [HideInInspector] public Vector3 offset;
        public RayTracingMaterial material;
    }

    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct Box
    {
        [HideInInspector] public Vector3 pos0;
        [HideInInspector] public Vector3 pos1;
        [HideInInspector] public Rect sideX1;
        [HideInInspector] public Rect sideX2;
        [HideInInspector] public Rect sideY1;
        [HideInInspector] public Rect sideY2;
        [HideInInspector] public Rect sideZ1;
        [HideInInspector] public Rect sideZ2;
        public RayTracingMaterial material;
    }
}