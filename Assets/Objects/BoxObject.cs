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

            var sides = new Rect[6];

            var t = transform;

            var scale = t.lossyScale;
            var rotation = t.rotation;
            var position = t.position;

            var bounds = mesh.bounds;
            var min = bounds.min + new Vector3(0,0,0.5f);
            var max = bounds.max - new Vector3(0,0,0.5f);

            min.Scale(scale);
            max.Scale(scale);

            var halfScaleX = -scale.x * 0.5f;
            var halfScaleY = -scale.y * 0.5f;
            var halfScaleZ = -scale.z * 0.5f;

            for (var i = 0; i < sides.Length; i++)
            {
                var offset = position;
                var rot = rotation.eulerAngles;
                switch (i)
                {
                    case 0:
                        offset += new Vector3(0, 0, halfScaleZ);
                        break;
                    case 1:
                        break;
                    case 2:
                        rot = new Vector3(rot.x,rot.y ,rot.z);
                        break;
                    case 3:
                        //rot += new Vector3(0, 0, -90);
                        break;
                    case 4:
                        //rot += new Vector3(0, 90, 0);
                        break;
                    case 5:
                        //rot += new Vector3(0, -90, 0);
                        break;
                }
                

                var rect = new Rect
                {
                    minPos = min,
                    maxPos = max,
                    offset = offset,
                    rotation = Matrix4x4.Transpose(Matrix4x4.Rotate(Quaternion.Euler(rot))),
                    material = box.material,
                };

                sides[i] = rect;
            }

            box.pos0 = bounds.min + position;
            box.pos1 = bounds.max + position;

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