using DataTypes;
using UnityEngine;
using UnityEngine.Serialization;

namespace Objects
{
    public class FogSphereObject : BaseObject
    {
        [SerializeField] private FogSphere fogSphere;

        public FogSphere GetFogSphere()
        {
            UpdateValues();

            return fogSphere;
        }

        protected override void UpdateValues()
        {
            var t = transform;
            fogSphere.center = t.position;
            fogSphere.radius = t.lossyScale.x / 2;
            fogSphere.negInvDensity = -1 / fogSphere.density;
            // ReSharper disable once ValueRangeAttributeViolation
            fogSphere.material.type = 3;
        }

        public override RayTracingMaterial GetMaterial()
        {
            return fogSphere.material;
        }

        public override void SetMaterial(RayTracingMaterial material)
        {
            fogSphere.material = material;
        }
    }
}