using System;
using System.Collections.Generic;
using System.Linq;
using DataTypes;
using RayTracingObjects;
using UnityEngine;

// ReSharper disable InconsistentNaming

namespace Util.Bvh
{
    public class BoundingVolumeHierarchy
    {
        private BoundingBox[] _boxes;
        private int largestIndex;

        public BoundingVolumeHierarchy()
        {
            _boxes = new BoundingBox[10];
        }

        private BvhNode BVHRoot { get; set; }

        public BoundingBox[] Boxes
        {
            get
            {
                Array.Resize(ref _boxes, largestIndex + 1);
                return _boxes;
            }
        }

        public void CreateBVH(List<BaseObject> baseObjects)
        {
            if (!baseObjects.Any()) return;

            var nodes = baseObjects.Select(bo => bo.GetBoundingBox()).ToList();

            BVHRoot = new BvhNode(nodes, 0, nodes.Count);
            _boxes = new BoundingBox[baseObjects.Count * 3];
            SetupList();
        }

        public void DrawBVH(Color rootColor, Color leftChildrenColor, Color rightChildrenColor)
        {
            if (BVHRoot == null) return;

            var boundingBox = BVHRoot.BoundingBox;

            DebugVisualizer.DrawBox(boundingBox.min, boundingBox.max, rootColor);

            DrawBVHChildren(BVHRoot.Left, leftChildrenColor);

            DrawBVHChildren(BVHRoot.Right, rightChildrenColor);
        }

        private static void DrawBVHChildren(BvhNode node, Color color)
        {
            while (true)
            {
                if (node == null) return;

                var boundingBox = node.BoundingBox;

                DebugVisualizer.DrawBox(boundingBox.min, boundingBox.max, color);

                DrawBVHChildren(node.Left, color);

                node = node.Right;
            }
        }

        public void DrawArray(Color rootColor, Color leftChildrenColor, Color rightChildrenColor)
        {
            var root = _boxes[0];

            if (root.Equals(default(BoundingBox))) return;


            DebugVisualizer.DrawBox(root.min, root.max, rootColor);

            DrawArrayChildren(GetLeftIndex(0), leftChildrenColor);
            DrawArrayChildren(GetRightIndex(0), rightChildrenColor);
        }

        private void DrawArrayChildren(int index, Color color)
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

        private int GetLeftIndex(int index)
        {
            if (_boxes[index].Equals(default(BoundingBox)))
                return -1;

            return 2 * index + 1;
        }

        private int GetRightIndex(int index)
        {
            if (_boxes[index].Equals(default(BoundingBox)))
                return -1;

            return 2 * index + 2;
        }

        private void SetupList()
        {
            TreeToList(BVHRoot, 0);
        }

        private void TreeToList(BvhNode node, int index)
        {
            while (true)
            {
                if (node == null || index == -1 || index >= _boxes.Length) return;

                _boxes[index] = node.BoundingBox;
                largestIndex = index;

                var leftIndex = GetLeftIndex(index);

                if (node.BoundingBox.typeofElement == TypesOfElement.AABB)
                    _boxes[index].index = leftIndex;

                TreeToList(node.Left, leftIndex);
                node = node.Right;
                index = GetRightIndex(index);
            }
        }
    }
}