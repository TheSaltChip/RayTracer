

using Objects;
using UnityEngine;

namespace Changers
{
    public class SpinningRoomHoveringSphere : Changer
    {
        [SerializeField] private SphereObject sphere;
        [SerializeField] private GameObject room;
        [SerializeField] private GameObject sphereCollection;

        private int _roomAngle;
        private Vector3 _ballsAngle;
        private float _sphereHeight;
        
        public override void Initialize()
        {
            
        }

        public override void Increment()
        {
            
        }

        public override string FileName()
        {
            return $"Screenshot_num{Number}";
        }
    }
}