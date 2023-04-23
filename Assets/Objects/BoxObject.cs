using System.Collections.Generic;
using DataTypes;
using UnityEngine;

namespace Objects
{
    public class BoxObject : BaseObject
    {
        [SerializeField] private MeshFilter meshFilter;

        [SerializeField] private BoxInfo boxInfo;

        private BoxSide[] _sides;

        protected override void UpdateValues()
        {
            var mesh = meshFilter.sharedMesh;

            var t = transform;

            var scale = t.lossyScale;
            var rotation = t.rotation;
            var position = t.position;

            var bounds = mesh.bounds;
            var min = bounds.min;
            var max = bounds.max;

            min.Scale(scale);
            max.Scale(scale);

            var halfScaleX = new Vector3(-scale.x * 0.5f, 0, 0);
            var halfScaleY = new Vector3(0, -scale.y * 0.5f, 0);
            var halfScaleZ = new Vector3(0, 0, -scale.z * 0.5f);

            var rotationMatrix = Matrix4x4.Rotate(rotation);

            _sides = new BoxSide[6];

            for (var i = 0; i < _sides.Length; i++)
            {
                var offset = position;
                var tMin = min;
                var tMax = max;
                var rot = rotation;
                switch (i)
                {
                    case 0:
                        offset += rotationMatrix.MultiplyPoint3x4(halfScaleZ);
                        tMin = new Vector3(min.x, min.y, 0);
                        tMax = new Vector3(max.x, max.y, 0);
                        break;
                    case 1:
                        offset += rotationMatrix.MultiplyPoint3x4(-halfScaleZ);
                        tMin = new Vector3(min.x, min.y, 0);
                        tMax = new Vector3(max.x, max.y, 0);
                        break;
                    case 2:
                        offset += rotationMatrix.MultiplyPoint3x4(halfScaleY);
                        rot *= Quaternion.Euler(new Vector3(90, 0, 0));
                        tMin = new Vector3(min.x, min.z, 0);
                        tMax = new Vector3(max.x, max.z, 0);
                        break;
                    case 3:
                        offset += rotationMatrix.MultiplyPoint3x4(-halfScaleY);
                        rot *= Quaternion.Euler(new Vector3(90, 0, 0));
                        tMin = new Vector3(min.x, min.z, 0);
                        tMax = new Vector3(max.x, max.z, 0);
                        break;
                    case 4:
                        rot *= Quaternion.Euler(new Vector3(0, 90, 0));
                        offset += rotationMatrix.MultiplyPoint3x4(halfScaleX);
                        tMin = new Vector3(min.z, min.y, 0);
                        tMax = new Vector3(max.z, max.y, 0);
                        break;
                    case 5:
                        tMin = new Vector3(min.z, min.y, 0);
                        tMax = new Vector3(max.z, max.y, 0);
                        rot *= Quaternion.Euler(new Vector3(0, 90, 0));
                        offset += rotationMatrix.MultiplyPoint3x4(-halfScaleX);
                        break;
                }

                var rect = new BoxSide()
                {
                    minPos = tMin,
                    maxPos = tMax,
                    offset = offset,
                    rotation = Matrix4x4.Transpose(Matrix4x4.Rotate(rot))
                };

                _sides[i] = rect;
            }

            var localToWorldMatrix = Matrix4x4.TRS(position, rotation, scale);

            boxInfo.boundsMin = localToWorldMatrix.MultiplyPoint3x4(bounds.min);
            boxInfo.boundsMax = localToWorldMatrix.MultiplyPoint3x4(bounds.max);

            (boxInfo.boundsMin, boxInfo.boundsMax) = GetTransformedBounds(bounds.min, bounds.max, localToWorldMatrix);
        }

        public BoxInfo GetBoxInfo()
        {
            UpdateValues();

            return boxInfo;
        }

        public IEnumerable<BoxSide> GetSides()
        {
            UpdateValues();

            return _sides;
        }

        public override RayTracingMaterial GetMaterial() => boxInfo.material;


        public override void SetMaterial(RayTracingMaterial material)
        {
            boxInfo.material = material;
        }

        // Compute a new axis-aligned bounding box that will contain whatever the original
        // bounds did, after an affine transformation. (Note this is a lossy operation)
        private static (Vector3 min, Vector3 max) GetTransformedBounds(Vector3 oldMin, Vector3 oldMax,
            Matrix4x4 transformation)
        {
            var corners = new Vector3[8];

            corners[0] = oldMin;
            corners[1] = new Vector3(oldMin.x, oldMin.y, oldMax.z);
            corners[2] = new Vector3(oldMin.x, oldMax.y, oldMin.z);
            corners[3] = new Vector3(oldMax.x, oldMin.y, oldMin.z);
            corners[4] = new Vector3(oldMin.x, oldMax.y, oldMax.z);
            corners[5] = new Vector3(oldMax.x, oldMin.y, oldMax.z);
            corners[6] = new Vector3(oldMax.x, oldMax.y, oldMin.z);
            corners[7] = oldMax;

            var min = Vector3.positiveInfinity;
            var max = Vector3.negativeInfinity;

            for (var i = 0; i < 8; i++)
            {
                var transformed = transformation.MultiplyPoint3x4(corners[i]);
                min = Vector3.Min(min, transformed);
                max = Vector3.Max(max, transformed);
            }

            return (min, max);
        }
    }
}