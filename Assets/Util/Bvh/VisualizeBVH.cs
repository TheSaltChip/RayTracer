using DataTypes;
using UnityEngine;

namespace Util.Bvh
{
    // ReSharper disable once InconsistentNaming
    public static class VisualizeBVH
    {
        private static BoundingBox[] _boxes;
        private static BVHNode[] _nodes;


        public static void DrawMultipleBVHNodes(BVHNode[][] nodes, Color rootColor,
            Color leftChildrenColor,
            Color rightChildrenColor)
        {
            foreach (var bvhNode in nodes)
            {
                DrawArray(bvhNode, rootColor, leftChildrenColor, rightChildrenColor);
            }
        }

        public static void DrawArray(BVHNode[] nodes, Color rootColor,
            Color leftChildrenColor,
            Color rightChildrenColor)
        {
            var root = nodes[0];

            DebugVisualizer.DrawBox(root.aabbMin,
                root.aabbMax, rootColor);

            _nodes = nodes;

            DrawArrayChildrenNode(root.leftFirst, leftChildrenColor, Matrix4x4.identity);
            DrawArrayChildrenNode(root.leftFirst + 1, rightChildrenColor, Matrix4x4.identity);
        }

        private static void DrawArrayChildrenNode(int index, Color color, Matrix4x4 localToWorldMatrix)
        {
            if (index > _nodes.Length) return;

            var node = _nodes[index];

            if (node.Equals(default(BVHNode)) || node.triCount != 0) return;

            DebugVisualizer.DrawBox(node.aabbMin,node.aabbMax, color);

            DrawArrayChildrenNode(node.leftFirst, color, localToWorldMatrix);
            DrawArrayChildrenNode(node.leftFirst + 1, color, localToWorldMatrix);
        }

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