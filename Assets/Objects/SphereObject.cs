using UnityEngine;
using static Structs;


namespace Objects
{
    public class SphereObject : BaseObject
    {
        [SerializeField] private Mat material;

        public Sphere Sphere
        {
            get
            {
                var o = gameObject;
                return new Sphere(o.transform.position, o.transform.lossyScale.x / 2, material);
            }
        }

        public override Mat GetMaterial()
        {
            return material;
        }

        public override void SetMaterial(Mat material)
        {
            this.material = material;
        }
    }
}