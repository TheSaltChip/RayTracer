using DataTypes;
using UnityEngine;


namespace Objects
{
    public class SphereObject : BaseObject
    {
        [SerializeField] private Sphere sphere;

        public Sphere GetSphere()
        {
            UpdateValues();

            return sphere;
        }

        protected override void UpdateValues()
        {
            var t = transform;
            sphere.center = t.position;
            sphere.radius = t.lossyScale.x / 2;
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