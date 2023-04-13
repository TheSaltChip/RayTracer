using Attributes;
using UnityEngine;

namespace Changers
{
    public class YAngleChanger : Changer
    {
        [SerializeField] private GameObject objectToRotate;
        [SerializeField, Min(0)] private float rotationPerIncrement;
        [SerializeField, Min(0)] private int maxAngle = 360;
        [SerializeField, ReadOnly] private float angle;

        public override void Increment()
        {
            angle += rotationPerIncrement;

            IsDone = angle >= maxAngle;

            if (!IsDone)
                objectToRotate.transform.Rotate(Vector3.up, rotationPerIncrement);
        }

        public override string FileName()
        {
            return $"/Screenshot_angle{angle:000_0}.png";
        }

        public override void Initialize()
        {
            angle = Mathf.Abs(objectToRotate.transform.rotation.eulerAngles.y);
        }
    }
}