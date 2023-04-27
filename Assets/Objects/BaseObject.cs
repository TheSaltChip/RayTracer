using DataTypes;
using UnityEngine;

namespace Objects
{
    [ExecuteAlways]
    public abstract class BaseObject : MonoBehaviour
    {
        private Matrix4x4 _oldMatrix;
        protected bool shouldUpdateValues;

        protected BoundingBox boundingBox = new();

        public void Index(int index) => boundingBox.indexOfElement = index;

        public void ShouldUpdateValues()
        {
            shouldUpdateValues = true;
        }

        public abstract RayTracingMaterial GetMaterial();
        public abstract void SetMaterial(RayTracingMaterial material);

        public BoundingBox GetBoundingBox()
        {
            return boundingBox;
        }
        
        private void Start()
        {
            shouldUpdateValues = true;
            _oldMatrix = transform.localToWorldMatrix;
        }
        
        // private void OnDrawGizmos()
        // {
        //     var size = boundingBox.max - boundingBox.min;
        //     var center = size / 2 + boundingBox.min;
        //     Gizmos.DrawWireCube(center, size);
        // }

        private void Update()
        {
            if (CheckIfMatricesAreEqual(_oldMatrix, transform.localToWorldMatrix)) return;

            _oldMatrix = transform.localToWorldMatrix;
            shouldUpdateValues = true;
        }

        private void OnValidate()
        {
            shouldUpdateValues = true;
        }

        private bool CheckIfMatricesAreEqual(Matrix4x4 a, Matrix4x4 b)
        {
            for (var i = 0; i < 4; i++)
            {
                for (var j = 0; j < 4; j++)
                {
                    if (a[i, j] - b[i, j] != 0f)
                        return false;
                }
            }

            return true;
        }
        
        protected static (Vector3 min, Vector3 max) GetTransformedBounds(Vector3 oldMin, Vector3 oldMax,
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