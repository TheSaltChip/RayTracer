using RayTracingObjects;
using UnityEngine;

namespace Changers
{
    public class Hovering : Changer
    {
        [Header("Will hover an object based on the following formula:\n" +
                "amplitude * sin(PI/period * numberOfIterations + PI/phase) + verticalShift")]
        [Space, SerializeField]
        private BaseObject baseObject;

        [SerializeField] private float amplitude = 1;

        [SerializeField, Tooltip("PI / (this number)")]
        private float period;

        [SerializeField, Tooltip("PI / (this number)")]
        private float phase;

        [SerializeField] private float verticalShift;

        private Vector3 _objectPos;
        private float _period;
        private float _phase;

        private Vector3 _initialPosition;

        public override void Initialize()
        {
            _initialPosition = baseObject.transform.position;
            _objectPos = _initialPosition;
            _period = period == 0 ? 0 : Mathf.PI / period;
            _phase = phase == 0 ? 0 : Mathf.PI / phase;
        }

        public override void Increment()
        {
            ++NumberOfIterations;
            _objectPos.y = amplitude * Mathf.Sin(_period * NumberOfIterations + _phase) + verticalShift;
            baseObject.transform.position = _objectPos;
        }

        public override void ResetValues()
        {
            baseObject.transform.position = _initialPosition;
            _objectPos = _initialPosition;
        }
    }
}