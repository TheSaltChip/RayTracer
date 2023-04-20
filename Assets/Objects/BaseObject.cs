using DataTypes;
using UnityEngine;

namespace Objects
{
    public abstract class BaseObject : MonoBehaviour
    {
        public abstract RayTracingMaterial GetMaterial();
        public abstract void SetMaterial(RayTracingMaterial material);
    }
}