using System;
using DataTypes;
using UnityEngine;

namespace Objects
{
    public abstract class BaseObject : MonoBehaviour
    {
        public bool ShouldUpdateValues { get; set; }
        
        public abstract RayTracingMaterial GetMaterial();
        public abstract void SetMaterial(RayTracingMaterial material);
        
        protected abstract void UpdateValues();

        private void Start()
        {
            ShouldUpdateValues = true;
        }
    }
}