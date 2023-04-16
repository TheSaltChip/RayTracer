using UnityEngine;

namespace Changers
{
    public abstract class Changer : MonoBehaviour
    {
        public int NumberOfIterations { get; protected set; }
        public abstract void Initialize();
        public abstract void Increment();

        public abstract void ResetValues();
    }
}