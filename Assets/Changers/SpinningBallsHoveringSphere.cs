using System;
using Objects;
using UnityEngine;

namespace Changers
{
    public class SpinningBallsHoveringSphere : Changer
    {
        [SerializeField] private SphereObject sphere;
        [SerializeField] private GameObject sphereCollection;
        [SerializeField, Range(1, 360)] private float maxAngle;
        [SerializeField, Range(1, 360)] private float angleIncrement;
        [SerializeField, Range(0, 1)] private float maxHeight;
        [SerializeField, Range(-1, 0)] private float minHeight;
        [SerializeField, Range(0, 1)] private float heightIncrement;

        private Vector3 _ballsAngle;
        private bool _up;
        private Vector3 _spherePos;
        private Vector3 _angleIncrements;

        public override void Initialize()
        {
            _spherePos = sphere.transform.position;
            _ballsAngle = sphereCollection.transform.rotation.eulerAngles;
            _angleIncrements = Vector3.one * angleIncrement;
        }

        public override void Increment()
        {
            _ballsAngle += _angleIncrements;
            
            IsDone = _ballsAngle.x >= maxAngle;
            
            if (IsDone)
            {
                return;
            }
            
            if (_spherePos.y >= maxHeight)
                _up = false;
            else if (_spherePos.y <= minHeight)
            {
                _up = true;
            }
            
            ++NumberOfIterations;
            
            sphereCollection.transform.localRotation = Quaternion.Euler(_ballsAngle);
            
            _spherePos.y += _up ? heightIncrement : -heightIncrement;
            sphere.transform.position = _spherePos;
        }

        protected override string SetFileName()
        {
            return $"Screenshot_num{NumberOfIterations}";
        }
    }
}