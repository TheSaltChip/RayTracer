using UnityEngine;

namespace Util
{
    public static class DebugVisualizer
    {
        public static void DrawBox(Vector3 min, Vector3 max, Color color)
        {
            var corners = new Vector3[8];

            corners[0] = min;
            corners[1] = new Vector3(min.x, min.y, max.z);
            corners[2] = new Vector3(min.x, max.y, min.z);
            corners[3] = new Vector3(max.x, min.y, min.z);
            corners[4] = new Vector3(min.x, max.y, max.z);
            corners[5] = new Vector3(max.x, min.y, max.z);
            corners[6] = new Vector3(max.x, max.y, min.z);
            corners[7] = max;

            Debug.DrawLine(corners[0], corners[1], color);
            Debug.DrawLine(corners[0], corners[2], color);
            Debug.DrawLine(corners[0], corners[3], color);
            Debug.DrawLine(corners[0], corners[3], color);

            Debug.DrawLine(corners[2], corners[4], color);
            Debug.DrawLine(corners[2], corners[6], color);

            Debug.DrawLine(corners[1], corners[5], color);
            Debug.DrawLine(corners[3], corners[5], color);

            Debug.DrawLine(corners[1], corners[4], color);
            Debug.DrawLine(corners[3], corners[6], color);

            Debug.DrawLine(corners[4], corners[7], color);
            Debug.DrawLine(corners[5], corners[7], color);
            Debug.DrawLine(corners[6], corners[7], color);
        }
    }
}