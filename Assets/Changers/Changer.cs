using UnityEngine;

namespace Changers
{
    public abstract class Changer : MonoBehaviour
    {
        protected int Number = 0;
        public bool IsDone { get; protected set; }
        public abstract void Initialize();
        public abstract void Increment();

        public abstract string FileName();
    }
}