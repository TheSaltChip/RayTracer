using DataTypes;
using UnityEngine;


namespace Objects
{
    public class SphereObject : BaseObject
    {
        [SerializeField] private RayTracingMaterial material;

        public Sphere Sphere
        {
            get
            {
                var o = gameObject;
                return new Sphere(o.transform.position, o.transform.lossyScale.x / 2, material);
            }
        }

        public override RayTracingMaterial GetMaterial()
        {
            return material;
        }

        public override void SetMaterial(RayTracingMaterial material)
        {
            this.material = material;
        }

        protected override void UpdateValues()
        {
            throw new System.NotImplementedException();
        }
    }
}