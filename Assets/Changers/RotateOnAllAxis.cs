using UnityEngine;
using UnityEngine.Serialization;

namespace Changers
{
    public class RotateOnAllAxis : Changer
    {
        [SerializeField] private GameObject whatToSpin;
        [SerializeField, Range(0, 360)] private float angleIncrementX;
        [SerializeField, Range(0, 360)] private float angleIncrementY;
        [SerializeField, Range(0, 360)] private float angleIncrementZ;

        private Vector3 _angle;
        private bool _up;
        private Vector3 _angleIncrements;
        
        private Quaternion _initialRotation;

        public override void Initialize()
        {
            _initialRotation = whatToSpin.transform.rotation;
            _angle = _initialRotation.eulerAngles;
            _angleIncrements = new Vector3(angleIncrementX, angleIncrementY, angleIncrementZ);
        }

        public override void Increment()
        {
            _angle += _angleIncrements;

            whatToSpin.transform.localRotation = Quaternion.Euler(_angle);
        }

        public override void ResetValues()
        {
            whatToSpin.transform.rotation = _initialRotation;
            Initialize();
        }
    }
}