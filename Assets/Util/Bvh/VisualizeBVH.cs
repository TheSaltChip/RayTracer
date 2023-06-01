using DataTypes;
using UnityEngine;

namespace Util.Bvh
{
    // ReSharper disable once InconsistentNaming
    public static class VisualizeBVH
    {
        private static BoundingBox[] _boxes;

        public static void DrawArray(BoundingBox[] boxes, Color rootColor, Color leftChildrenColor,
            Color rightChildrenColor)
        {
            if (boxes.Length == 0) return;

            _boxes = boxes;
            var root = _boxes[0];

            if (root.Equals(default(BoundingBox))) return;


            DebugVisualizer.DrawBox(root.min, root.max, rootColor);

            DrawArrayChildren(GetLeftIndex(0), leftChildrenColor);
            DrawArrayChildren(GetRightIndex(0), rightChildrenColor);
        }

        private static void DrawArrayChildren(int index, Color color)
        {
            while (true)
            {
                if (index == -1 || index >= _boxes.Length) return;

                var box = _boxes[index];
                if (box.Equals(default(BoundingBox))) return;

                DebugVisualizer.DrawBox(box.min, box.max, color);

                DrawArrayChildren(GetLeftIndex(index), color);
                index = GetRightIndex(index);
            }
        }

        private static int GetLeftIndex(int index)
        {
            if (_boxes[index].Equals(default(BoundingBox)))
                return -1;

            return 2 * index + 1;
        }

        private static int GetRightIndex(int index)
        {
            if (_boxes[index].Equals(default(BoundingBox)))
                return -1;

            return 2 * index + 2;
        }
    }
}