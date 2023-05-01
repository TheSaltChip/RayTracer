using DataTypes;
using UnityEngine;

namespace RayTracingObjects
{
    [ExecuteAlways]
    public class FogBoxObject : BaseObject
    {
        [SerializeField] private MeshFilter meshFilter;

        [SerializeField] private FogBox fogBox;

        public FogBox GetFogBox()
        {
            UpdateValues();

            return fogBox;
        }

        private void UpdateValues()
        {
            if (!shouldUpdateValues) return;
            shouldUpdateValues = false;

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

            var sides = new BoxSide[6];

            for (var i = 0; i < sides.Length; i++)
            {
                var offset = position;
                var tMin = Vector3.zero;
                var tMax = Vector3.zero;
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
                        offset += rotationMatrix.MultiplyPoint3x4(halfScaleX);
                        rot *= Quaternion.Euler(new Vector3(0, 90, 0));
                        tMin = new Vector3(min.z, min.y, 0);
                        tMax = new Vector3(max.z, max.y, 0);
                        break;
                    case 5:
                        offset += rotationMatrix.MultiplyPoint3x4(-halfScaleX);
                        rot *= Quaternion.Euler(new Vector3(0, 90, 0));
                        tMin = new Vector3(min.z, min.y, 0);
                        tMax = new Vector3(max.z, max.y, 0);
                        break;
                }

                var rect = new BoxSide
                {
                    minPos = tMin,
                    maxPos = tMax,
                    offset = offset,
                    rotation = Matrix4x4.Transpose(Matrix4x4.Rotate(rot))
                };

                sides[i] = rect;
            }

            fogBox.sideX1 = sides[0];
            fogBox.sideX2 = sides[1];
            fogBox.sideY1 = sides[2];
            fogBox.sideY2 = sides[3];
            fogBox.sideZ1 = sides[4];
            fogBox.sideZ2 = sides[5];

            var localToWorldMatrix = t.localToWorldMatrix;

            fogBox.boundsMin = localToWorldMatrix.MultiplyPoint3x4(bounds.min);
            fogBox.boundsMax = localToWorldMatrix.MultiplyPoint3x4(bounds.max);

            (fogBox.boundsMin, fogBox.boundsMax) = GetTransformedBounds(bounds.min, bounds.max, localToWorldMatrix);
            
            boundingBox.min = fogBox.boundsMin;
            boundingBox.max = fogBox.boundsMax;
            boundingBox.typeofElement = TypesOfElement.FogBox;
            
            fogBox.negInvDensity = -1 / fogBox.density;
            // ReSharper disable once ValueRangeAttributeViolation
            fogBox.material.type = 3;
        }

        public override RayTracingMaterial GetMaterial()
        {
            return fogBox.material;
        }

        public override void SetMaterial(RayTracingMaterial material)
        {
            fogBox.material = material;
        }

        
    }
}