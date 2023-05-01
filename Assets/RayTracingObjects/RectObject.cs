using DataTypes;
using UnityEngine;
using Rect = DataTypes.Rect;

namespace RayTracingObjects
{
    [ExecuteAlways]
    public class RectObject : BaseObject
    {
        [SerializeField] private Rect rect;

        [SerializeField] private MeshFilter meshFilter;

        private Mesh _mesh;

        public Rect GetRect()
        {
            UpdateValues();

            return rect;
        }

        private void UpdateValues()
        {
            if (!shouldUpdateValues) return;
            shouldUpdateValues = false;

            _mesh = meshFilter.sharedMesh;

            Vector3 min = _mesh.bounds.min, max = _mesh.bounds.max;

            var t = transform;
            var lossyScale = t.lossyScale;

            min.Scale(lossyScale);
            max.Scale(lossyScale);

            var rotation = t.rotation;
            rect.rotation = Matrix4x4.Transpose(Matrix4x4.Rotate(rotation));
            rect.offset = t.position;

            rect.minPos = min;
            rect.maxPos = max;

            (boundingBox.min, boundingBox.max) =
                GetTransformedBounds(_mesh.bounds.min, _mesh.bounds.max, t.localToWorldMatrix);
            var padding = new Vector3(0, 0, 0.01f);
            boundingBox.min += padding;
            boundingBox.max += padding;
            boundingBox.typeofElement = TypesOfElement.Rect;
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