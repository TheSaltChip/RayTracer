using System;
using System.Collections.Generic;
using Attributes;
using Changers;
using UnityEngine;

namespace Manager
{
    public class ChangerManager : MonoBehaviour
    {
        [SerializeField] private List<Changer> changers;
        [SerializeField] private int numberOfImages;
        [SerializeField] private bool increment;
        [SerializeField] private bool reset;
        [SerializeField, ReadOnly] private int numOfIterations;

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

        public int NumberOfIterations { get; private set; }
        public bool IsDone { get; private set; }

        public void Initialize()
        {
            numOfIterations = NumberOfIterations;
            foreach (var changer in changers) changer.Initialize();
        }

        public void Increment()
        {
            ++NumberOfIterations;
            numOfIterations = NumberOfIterations;
            IsDone = NumberOfIterations > numberOfImages-2;

            if (IsDone) return;

            foreach (var changer in changers) changer.Increment();
        }

        private void ResetValues()
        {
            NumberOfIterations = 0;
            numOfIterations = NumberOfIterations;
            
            foreach (var changer in changers) changer.ResetValues();
        }

        public string FileName()
        {
            return $"Screenshot_num{NumberOfIterations:0000}.png";
        }
    }
}