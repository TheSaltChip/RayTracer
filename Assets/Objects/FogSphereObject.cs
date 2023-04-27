using System;
using DataTypes;
using UnityEngine;

namespace Objects
{
    [ExecuteAlways]
    public class FogSphereObject : BaseObject
    {
        [SerializeField] private FogSphere fogSphere;

        public FogSphere GetFogSphere()
        {
            UpdateValues();

            return fogSphere;
        }


        private void UpdateValues()
        {
            if (!shouldUpdateValues) return;
            shouldUpdateValues = false;

            var t = transform;
            fogSphere.center = t.position;

            var lossyScale = t.lossyScale;

            fogSphere.radius = lossyScale.x / 2;
            fogSphere.negInvDensity = -1 / fogSphere.density;
            // ReSharper disable once ValueRangeAttributeViolation
            fogSphere.material.type = 3;

            var rad3 = Vector3.one * fogSphere.radius;
            boundingBox.min = fogSphere.center - rad3;
            boundingBox.max = fogSphere.center + rad3;
            boundingBox.typeofElement = TypesOfElement.FogSphere;
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