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
            
            Vector3 min = _mesh.bounds.min, max = _mesh.bounds.max;

            var t = transform;
            var lossyScale = t.lossyScale;

            min.Scale(lossyScale);
            max.Scale(lossyScale);
            
            rect.rotation = Matrix4x4.Transpose(Matrix4x4.Rotate(t.rotation));
            rect.offset = t.position;

            rect.minPos = min;
            rect.maxPos = max;

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

        protected override void UpdateValues()
        {
            throw new System.NotImplementedException();
        }
    }
}