using System.Collections.Generic;
using DataTypes;
using Manager;
using UnityEngine;

namespace Objects
{
    public class MeshObject : BaseObject
    {
        [SerializeField] private MeshFilter meshFilter;

        public MeshInfo info;

        private Mesh _mesh;
        private List<Triangle> _triangles;

        public (MeshInfo, List<Triangle>) GetInfoAndList()
        {
            _mesh = meshFilter.sharedMesh;

            _triangles = new List<Triangle>();

            var vert = new Vector3[_mesh.vertexCount];
            var normals = new Vector3[_mesh.vertexCount];

            Vector3 min = Vector3.positiveInfinity, max = Vector3.negativeInfinity;

            for (var index = 0; index < _mesh.vertices.Length; index++)
            {
                vert[index] = transform.TransformPoint(_mesh.vertices[index]);
                normals[index] = transform.TransformDirection(_mesh.normals[index]);
                for (var n = 0; n < 3; n++)
                {
                    max = Vector3.Max(vert[index], max);
                    min = Vector3.Min(vert[index], min);
                }
            }

            var triIndex = _mesh.triangles;

            for (var i1 = 0; i1 < triIndex.Length; i1 += 3)
            {
                int i2 = i1 + 1, i3 = i1 + 2;
                var tri = new Triangle(vert[triIndex[i1]], vert[triIndex[i2]], vert[triIndex[i3]],
                    normals[triIndex[i1]], normals[triIndex[i2]], normals[triIndex[i3]]);
                _triangles.Add(tri);
            }
            
            info.boundsMax = max;
            info.boundsMin = min;
            info.numTriangles = _triangles.Count;
            
            return (info, _triangles);
        }
        
        public override RayTracingMaterial GetMaterial()
        {
            return info.material;
        }

        public override void SetMaterial(RayTracingMaterial material)
        {
            info.material = material;
        }

        protected override void UpdateValues()
        {
            throw new System.NotImplementedException();
        }
    }
}