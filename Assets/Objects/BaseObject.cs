using DataTypes;
using UnityEngine;
using Matrix4x4 = UnityEngine.Matrix4x4;

namespace Objects
{
    [ExecuteAlways]
    public abstract class BaseObject : MonoBehaviour
    {
        private Matrix4x4 _oldMatrix;
        protected bool shouldUpdateValues;

        public void ShouldUpdateValues()
        {
            shouldUpdateValues = true;
        }

        public abstract RayTracingMaterial GetMaterial();
        public abstract void SetMaterial(RayTracingMaterial material);

        private void Start()
        {
            shouldUpdateValues = true;
            _oldMatrix = transform.localToWorldMatrix;
        }

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
    }
}