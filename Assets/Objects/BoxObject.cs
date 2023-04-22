using DataTypes;
using UnityEngine;
using Rect = DataTypes.Rect;

namespace Objects
{
    public class BoxObject : BaseObject
    {
        [SerializeField] private MeshFilter meshFilter;

        [SerializeField] private Box box;

        public Box GetBox()
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

            var sides = new Rect[6];

            for (var i = 0; i < sides.Length; i++)
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

                var rect = new Rect
                {
                    minPos = tMin,
                    maxPos = tMax,
                    offset = offset,
                    rotation = Matrix4x4.Transpose(Matrix4x4.Rotate(rot)),
                    material = box.material,
                };

                sides[i] = rect;
            }

            box.pos0 = min + position;
            box.pos1 = max + position;

            box.sideX1 = sides[0];
            box.sideX2 = sides[1];
            box.sideY1 = sides[2];
            box.sideY2 = sides[3];
            box.sideZ1 = sides[4];
            box.sideZ2 = sides[5];

            return box;
        }

        public override RayTracingMaterial GetMaterial()
        {
            throw new System.NotImplementedException();
        }

        public override void SetMaterial(RayTracingMaterial material)
        {
            throw new System.NotImplementedException();
        }
    }
}