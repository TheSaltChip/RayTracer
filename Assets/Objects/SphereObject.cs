using DataTypes;
using UnityEngine;


namespace Objects
{
    [ExecuteAlways]
    public class SphereObject : BaseObject
    {
        [SerializeField] private Sphere sphere;

        public Sphere GetSphere()
        {
            UpdateValues();

            return sphere;
        }

        private void UpdateValues()
        {
            if (!shouldUpdateValues) return;
            shouldUpdateValues = false;
            
            var t = transform;
            sphere.center = t.position;
            var lossyScale = t.lossyScale;
            sphere.radius = lossyScale.x / 2;
            
            var bounds = new Bounds(sphere.center, lossyScale);
            boundingBox.min = bounds.min;
            boundingBox.max = bounds.max;
            boundingBox.typeofElement = TypesOfElement.Sphere;
        }
        
        public override RayTracingMaterial GetMaterial()
        {
            return sphere.material;
        }

        public override void SetMaterial(RayTracingMaterial material)
        {
            sphere.material = material;
        }
    }
}