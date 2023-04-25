using System;
using DataTypes;
using UnityEngine;

namespace Objects
{
    public abstract class BaseObject : MonoBehaviour
    {
        public bool ShouldUpdateValues { get; set; }
        protected Transform oldTransform;
        protected abstract void UpdateValues();

        public abstract RayTracingMaterial GetMaterial();
        public abstract void SetMaterial(RayTracingMaterial material);

        private void Start()
        {
            ShouldUpdateValues = true;
            oldTransform = transform;
        }

        private void Update()
        {
            
            if (transform == oldTransform) return;

            oldTransform = transform;
            ShouldUpdateValues = true;
        }

        private void OnValidate()
        {
            ShouldUpdateValues = true;
        }
    }
}