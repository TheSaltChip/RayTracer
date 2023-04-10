using UnityEngine;
using static Structs;

namespace Objects
{
    public abstract class BaseObject : MonoBehaviour
    {
        public abstract Mat GetMaterial();
        public abstract void SetMaterial(Mat material);
    }
}