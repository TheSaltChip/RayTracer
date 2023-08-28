// ReSharper disable InconsistentNaming

using System;
using DataTypes;
using UnityEngine;

namespace Util.Bvh
{
    public struct BVHNode
    {
        public Vector3 aabbMin, aabbMax;
        public int leftFirst, triCount;
    }

    public struct Bin
    {
        public BoundingBox bounds;
        public int triCount;
    }

    public class ImprovedBVH
    {
        private const int BINS = 8;

        public BVHNode[] _bvhNodes { private set;  get; }
        private int rootNodeIndex;
        private int nodesUsed;

        private Triangle[] _triangles;
        private int[] _triIndex;

        public ImprovedBVH(Triangle[] triangles)
        {
            _bvhNodes = new BVHNode[2 * triangles.Length - 1];
            rootNodeIndex = 0;
            nodesUsed = 1;
            _triangles = triangles;
            _triIndex = new int[triangles.Length];
        }

        public void Build()
        {
            for (var i = 0; i < _triangles.Length; i++)
            {
                _triIndex[i] = i;
            }

            _bvhNodes[rootNodeIndex].leftFirst = 0;
            _bvhNodes[rootNodeIndex].triCount = _triangles.Length;

            UpdateNodeBounds(rootNodeIndex);
            Subdivide(rootNodeIndex);
        }

        private void UpdateNodeBounds(int nodeIndex)
        {
            ref var node = ref _bvhNodes[nodeIndex];

            node.aabbMin = Vector3.positiveInfinity;
            node.aabbMax = Vector3.negativeInfinity;

            for (int first = node.leftFirst, i = 0; i < node.triCount; i++)
            {
                var leafTriIndex = _triIndex[first + i];
                var leafTri = _triangles[leafTriIndex];

                node.aabbMin = Vector3.Min(node.aabbMin, leafTri.posA);
                node.aabbMin = Vector3.Min(node.aabbMin, leafTri.posB);
                node.aabbMin = Vector3.Min(node.aabbMin, leafTri.posC);
                node.aabbMax = Vector3.Max(node.aabbMax, leafTri.posA);
                node.aabbMax = Vector3.Max(node.aabbMax, leafTri.posB);
                node.aabbMax = Vector3.Max(node.aabbMax, leafTri.posC);
            }
        }

        private void Subdivide(int nodeIndex)
        {
            ref var node = ref _bvhNodes[nodeIndex];

            var axis = 0;
            var splitPos = 0f;
            var splitCost = FindBestSplitPlane(node, ref axis, ref splitPos);
            var noSplitCost = CalculateNodeCost(node);

            if (splitCost >= noSplitCost) return;

            var i = node.leftFirst;
            var j = i + node.triCount - 1;

            while (i <= j)
            {
                if (_triangles[_triIndex[i]].centroid[axis] < splitPos)
                {
                    i++;
                    continue;
                }

                (_triIndex[i], _triIndex[j]) = (_triIndex[j--], _triIndex[i]);
            }

            var leftCount = i - node.leftFirst;

            if (leftCount == 0 || leftCount == node.triCount) return;

            var leftChildIdx = nodesUsed++;
            var rightChildIdx = nodesUsed++;

            _bvhNodes[leftChildIdx].leftFirst = node.leftFirst;
            _bvhNodes[leftChildIdx].triCount = leftCount;
            _bvhNodes[rightChildIdx].leftFirst = i;
            _bvhNodes[rightChildIdx].triCount = node.triCount - leftCount;

            node.leftFirst = leftChildIdx;
            node.triCount = 0;

            UpdateNodeBounds(leftChildIdx);
            UpdateNodeBounds(rightChildIdx);
            // recurse
            Subdivide(leftChildIdx);
            Subdivide(rightChildIdx);
        }

        private float CalculateNodeCost(BVHNode node)
        {
            var e = node.aabbMax - node.aabbMin;
            var surfaceArea = e.x * e.y + e.y * e.z + e.z * e.x;
            return node.triCount * surfaceArea;
        }

        private float FindBestSplitPlane(BVHNode node, ref int axis, ref float splitPos)
        {
            var bestCost = float.MaxValue;

            for (int a = 0; a < 3; a++)
            {
                var boundsMin = float.MaxValue;
                var boundsMax = float.MinValue;

                for (int i = 0; i < node.triCount; i++)
                {
                    var triangle = _triangles[_triIndex[node.leftFirst + i]];
                    boundsMin = Mathf.Min(triangle.centroid[a], boundsMin);
                    boundsMax = Mathf.Max(triangle.centroid[a], boundsMax);
                }

                if (boundsMin == boundsMax) continue;

                var bin = new Bin[BINS];
                var scale = BINS / (boundsMax - boundsMin);

                for (int i = 0; i < node.triCount; i++)
                {
                    var triangle = _triangles[_triIndex[node.leftFirst + i]];
                    var binIndex = Mathf.Min(BINS - 1, (int)((triangle.centroid[a] - boundsMin) * scale));
                    bin[binIndex].triCount++;
                    bin[binIndex].bounds.Grow(triangle.posA);
                    bin[binIndex].bounds.Grow(triangle.posB);
                    bin[binIndex].bounds.Grow(triangle.posC);
                }

                var leftArea = new float[BINS - 1];
                var rightArea = new float[BINS - 1];

                var leftCount = new int[BINS - 1];
                var rightCount = new int[BINS - 1];

                var leftBox = new BoundingBox();
                var rightBox = new BoundingBox();
                var leftSum = 0;
                var rightSum = 0;

                for (var i = 0; i < BINS - 1; i++)
                {
                    leftSum += bin[i].triCount;
                    leftCount[i] = leftSum;
                    leftBox.Grow(bin[i].bounds);
                    leftArea[i] = leftBox.Area();
                    rightSum += bin[BINS - 1 - i].triCount;
                    rightCount[BINS - 2 - i] = rightSum;
                    rightBox.Grow(bin[BINS - 1 - i].bounds);
                    rightArea[BINS - 2 - i] = rightBox.Area();
                }

                scale = (boundsMax - boundsMin) / BINS;

                for (var i = 0; i < BINS - 1; i++)
                {
                    var planeCost = leftCount[i] * leftArea[i] + rightCount[i] * rightArea[i];

                    if (!(planeCost < bestCost)) continue;

                    axis = a;
                    splitPos = boundsMin + scale * (i + 1);
                    bestCost = planeCost;
                }
            }

            return bestCost;
        }
    }
}