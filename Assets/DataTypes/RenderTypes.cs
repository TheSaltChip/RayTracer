using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Serialization;

namespace DataTypes
{
    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct Sphere
    {
        [HideInInspector] public Vector3 center;
        [HideInInspector] public float radius;
        public RayTracingMaterial material;

        public Sphere(Vector3 center, float radius, RayTracingMaterial material)
        {
            this.center = center;
            this.radius = radius;
            this.material = material;
        }
    }

    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct Triangle
    {
        [HideInInspector] public Vector3 posA, posB, posC;
        [HideInInspector] public Vector3 normalA, normalB, normalC;
        [HideInInspector] public Vector3 centroid;

        public Triangle(Vector3 posA, Vector3 posB, Vector3 posC, Vector3 normalA, Vector3 normalB, Vector3 normalC)
        {
            this.posA = posA;
            this.posB = posB;
            this.posC = posC;
            this.normalA = normalA;
            this.normalB = normalB;
            this.normalC = normalC;
            centroid = (posA+posB+posC) * 0.33333f; //new Vector3((posA.x + posB.x + posC.x) / 3f, (posA.y + posB.y + posC.y) / 3f,
            //(posA.z + posB.z + posC.z) / 3f);
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
    public struct BoxInfo
    {
        [HideInInspector] public Vector3 boundsMin;
        [HideInInspector] public Vector3 boundsMax;
        [HideInInspector] public int firstSideIndex;
        public RayTracingMaterial material;
    }

    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct BoxSide
    {
        [HideInInspector] public Vector3 minPos;
        [HideInInspector] public Vector3 maxPos;
        [HideInInspector] public Matrix4x4 rotation;
        [HideInInspector] public Vector3 offset;
    }

    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct FogBox
    {
        [HideInInspector] public Vector3 boundsMin;
        [HideInInspector] public Vector3 boundsMax;
        [HideInInspector] public BoxSide sideX1;
        [HideInInspector] public BoxSide sideX2;
        [HideInInspector] public BoxSide sideY1;
        [HideInInspector] public BoxSide sideY2;
        [HideInInspector] public BoxSide sideZ1;
        [HideInInspector] public BoxSide sideZ2;

        [Range(0, 2)] public float density;
        [HideInInspector] public float negInvDensity;

        public RayTracingMaterial material;
    }

    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct FogSphere
    {
        [HideInInspector] public Vector3 center;
        [HideInInspector] public float radius;

        [Range(0, 2)] public float density;
        [HideInInspector] public float negInvDensity;

        public RayTracingMaterial material;
    }

    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct BoundingBox
    {
        [HideInInspector] public Vector3 min;
        [HideInInspector] public Vector3 max;
        [HideInInspector] public TypesOfElement typeofElement;
        [HideInInspector] public int index;

        public void Grow(Vector3 p)
        {
            min = Vector3.Min(min, p);
            max = Vector3.Max(max, p);
        }

        public void Grow(BoundingBox b)
        {
            Grow(b.min);
            Grow(b.max);
        }

        public float Area()
        {
            var e = max - min; // box extent
            return e.x * e.y + e.y * e.z + e.z * e.x;
        }

        public override string ToString()
        {
            return $"{nameof(typeofElement)}: {typeofElement}, {nameof(index)}: {index}";
        }
    }

    public enum TypesOfElement
    {
        Sphere,
        Rect,
        Box,
        FogSphere,
        FogBox,
        Mesh,
        AABB
    }
}