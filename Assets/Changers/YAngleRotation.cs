using UnityEngine;

namespace Changers
{
    public class YAngleRotation : Changer
    {
        [SerializeField] private GameObject objectToRotate;
        [SerializeField, Min(0)] private float rotationPerIncrement;

        private Quaternion _initialRotation;
        
        public override void Increment()
        {
            objectToRotate.transform.Rotate(Vector3.up, rotationPerIncrement);
        }

        public override void ResetValues()
        {
            objectToRotate.transform.rotation = _initialRotation;
        }

        public override void Initialize()
        {
            _initialRotation = objectToRotate.transform.rotation;
        }
    }
}