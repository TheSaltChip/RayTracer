using System.Collections.Generic;
using DataTypes;
using UnityEngine;

namespace Util.Bvh
{
    public class BvhNode
    {
        public BoundingBox BoundingBox { get; }

        public BvhNode Left { get; }

        public BvhNode Right { get; }
        
        public BvhNode(List<BoundingBox> srcObjects, int start, int end)
        {
            var axis = Random.Range(0, 3);

            var comparer = Comparer<BoundingBox>.Create((a, b) => a.min[axis].CompareTo(b.min[axis]));

            var objectSpan = end - start;

            switch (objectSpan)
            {
                case 1:
                    Left = new BvhNode(srcObjects[start]);
                    Right = new BvhNode(srcObjects[start]);
                    break;
                case 2:
                {
                    var a = srcObjects[start];
                    var b = srcObjects[start + 1];

                    Left = new BvhNode(a);
                    Right = new BvhNode(b);

                    if (comparer.Compare(a, b) > 0)
                    {
                        (Left, Right) = (Right, Left);
                    }

                    break;
                }
                default:
                {
                    srcObjects.Sort(start, end - start, comparer);

                    var mid = start + objectSpan / 2;

                    Left = new BvhNode(srcObjects, start, mid);
                    Right = new BvhNode(srcObjects, mid, end);
                    break;
                }
            }

            BoundingBox = SurroundingBox(Left.BoundingBox, Right.BoundingBox);
        }

        private BvhNode(BoundingBox box)
        {
            box.isLeafNode = 1;
            BoundingBox = box;
        }

        public override string ToString()
        {
            return $"{nameof(BoundingBox)}: {BoundingBox}";
        }

        private static BoundingBox SurroundingBox(BoundingBox a, BoundingBox b) =>
            new()
            {
                min = Vector3.Min(a.min, b.min),
                max = Vector3.Max(a.max, b.max)
            };
    }
}