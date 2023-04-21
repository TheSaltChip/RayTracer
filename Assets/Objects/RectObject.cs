using DataTypes;
using UnityEngine;
using Rect = DataTypes.Rect;

namespace Objects
{
    public class RectObject : BaseObject
    {
        [SerializeField] private Rect rect;

        [SerializeField] private MeshFilter meshFilter;

        private Mesh _mesh;

        public Rect GetRect()
        {
            _mesh = meshFilter.sharedMesh;

            var vert = new Vector3[_mesh.vertexCount];

            var t = transform;

            var position = t.position;
            var rotation = t.rotation.eulerAngles;

            t.rotation = Quaternion.identity;
            t.position = Vector3.zero;

            Vector3 min = Vector3.positiveInfinity, max = Vector3.negativeInfinity;

            for (var index = 0; index < _mesh.vertices.Length; index++)
            {
                vert[index] = t.TransformPoint(_mesh.vertices[index]);
                for (var n = 0; n < 3; n++)
                {
                    max = Vector3.Max(vert[index], max);
                    min = Vector3.Min(vert[index], min);
                }
            }

            t.rotation = Quaternion.Euler(rotation);
            t.position = position;

            rotation *= Mathf.PI / 180f;

            rect.sinRotation = new Vector3(Mathf.Sin(rotation.x), Mathf.Sin(rotation.y), Mathf.Sin(rotation.z));
            rect.cosRotation = new Vector3(Mathf.Cos(rotation.x), Mathf.Cos(rotation.y), Mathf.Cos(rotation.z));
            rect.offset = position;

            switch (rect.orientation)
            {
                case 0:
                    rect.pos0 = min;
                    rect.pos1 = max;
                    rect.k = min.z;

                    rect.scale = t.lossyScale.x;
                    break;
                case 1:
                    rect.pos0 = min;
                    rect.pos1 = max;
                    rect.k = min.y;
                    rect.scale = t.lossyScale.x;
                    break;
                case 2:
                    rect.pos0 = min;
                    rect.pos1 = max;
                    rect.k = min.x;
                    rect.scale = t.lossyScale.x;
                    break;
            }

            return rect;
        }

        public override RayTracingMaterial GetMaterial()
        {
            return rect.material;
        }

        public override void SetMaterial(RayTracingMaterial material)
        {
            rect.material = material;
        }
    }
}