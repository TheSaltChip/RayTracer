using System.Collections.Generic;
using Attributes;
using Changers;
using UnityEngine;

namespace Managers
{
    public class ChangerManager : MonoBehaviour
    {
        [SerializeField] private List<Changer> changers;
        [SerializeField] private int maxNumberOfImages;
        [SerializeField] private bool increment;
        [SerializeField] private bool reset;
        [SerializeField, ReadOnly] private int numOfIterations;

        public int NumberOfImages => numOfIterations + 1;

        private void OnValidate()
        {
            if (reset)
            {
                reset = false;
                ResetValues();
            }

            if (!increment) return;

            Increment();
            increment = false;
        }
        
        public bool IsDone { get; private set; }

        public void Initialize()
        {
            numOfIterations = 0;
            foreach (var changer in changers) changer?.Initialize();
        }

        public void Increment()
        {
            ++numOfIterations;
            IsDone = numOfIterations >= maxNumberOfImages - 1;

            if (IsDone) return;

            foreach (var changer in changers) changer.Increment();
        }

        private void ResetValues()
        {
            numOfIterations = 0;

            foreach (var changer in changers) changer.ResetValues();
        }

        public string FileName()
        {
            return $"Screenshot_num{numOfIterations:0000}.png";
        }
    }
}