using UnityEngine;

namespace Objects
{
    public class RectObject : BaseObject
    {
        [SerializeField] private Structs.Rect rect;

        [SerializeField] private MeshFilter meshFilter;

        private Mesh _mesh;

        public Structs.Rect GetRect()
        {
            _mesh = meshFilter.sharedMesh;

            var vert = new Vector3[_mesh.vertexCount];

            var t = transform;

            var position = t.position;
            var rotation = t.rotation.eulerAngles;

            var q = Quaternion.identity;

            switch (rect)
            {
                case {orientation: 0}:
                    break;
                case {orientation: 1}:
                    q.eulerAngles = new Vector3(90, 0, 0);
                    break;
                case {orientation: 2}:
                    q.eulerAngles = new Vector3(0, 90, 0);
                    break;
            }

            t.rotation = q;
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

            rotation -= q.eulerAngles;
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

                    rect.scale = t.localScale.x;
                    break;
                case 1:
                    rect.pos0 = min;
                    rect.pos1 = max;
                    rect.k = min.y;
                    rect.scale = t.localScale.x;
                    break;
                case 2:
                    rect.pos0 = min;
                    rect.pos1 = max;
                    rect.k = min.x;
                    rect.scale = t.localScale.x;
                    break;
            }

            return rect;
        }

        public override Structs.Mat GetMaterial()
        {
            return rect.material;
        }

        public override void SetMaterial(Structs.Mat material)
        {
            rect.material = material;
        }
    }
}