using UnityEngine;

namespace Changers
{
    public abstract class Changer : MonoBehaviour
    {
        public int NumberOfIterations { get; protected set; }
        public bool IsDone { get; protected set; }
        public abstract void Initialize();
        public abstract void Increment();

        protected abstract string SetFileName();

        public string FileName()
        {
            return $"{SetFileName()}.png";
        }
    }
}