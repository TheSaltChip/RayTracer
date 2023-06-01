using DataTypes;
using Unity.VisualScripting;
using UnityEngine;

namespace RayTracingObjects
{
    [ExecuteAlways]
    public abstract class BaseObject : MonoBehaviour
    {
        [Header("Info")]
        public MeshRenderer meshRenderer;
        public MeshFilter meshFilter;
        
        [SerializeField, HideInInspector] private int materialObjectID;
        
        private Matrix4x4 _oldMatrix;
        protected bool shouldUpdateValues;

        protected BoundingBox boundingBox;

        public void Index(int index) => boundingBox.index = index;

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
            meshFilter = GetComponent<MeshFilter>();
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

            if (meshRenderer == null || meshFilter == null)
            {
                meshRenderer = GetComponent<MeshRenderer>();
                meshFilter = GetComponent<MeshFilter>();
            }

            SetUpMaterialDisplay();
        }

        private void SetUpMaterialDisplay()
        {
            if (gameObject.GetInstanceID() != materialObjectID)
            {
                materialObjectID = gameObject.GetInstanceID();
                var originalMaterials = meshRenderer.sharedMaterials;
                var newMaterials = new Material[originalMaterials.Length];
                var shader = Shader.Find("Standard");
                for (var i = 0; i < meshRenderer.sharedMaterials.Length; i++)
                {
                    newMaterials[i] = new Material(shader);
                }
                meshRenderer.sharedMaterials = newMaterials;
            }

            var mat = GetMaterial();
            
            foreach (var material in meshRenderer.sharedMaterials)
            {
                var displayEmissiveCol = mat.color.maxColorComponent < mat.emissionColor.maxColorComponent * mat.emissionStrength;
                var displayCol = displayEmissiveCol ? mat.emissionColor * mat.emissionStrength : mat.color;
                material.color = displayCol;
            }
        }
        

        private static bool CheckIfMatricesAreEqual(Matrix4x4 a, Matrix4x4 b)
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