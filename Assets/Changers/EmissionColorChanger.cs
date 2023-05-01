using DataTypes;
using RayTracingObjects;
using UnityEngine;

namespace Changers
{
    public class EmissionColorChanger : Changer
    {
        [SerializeField] private BaseObject whatToChangeColorOn;
        [SerializeField, Range(0, 359)] private float stepSize;

        private float _stepSize;

        private float _h;
        private float _s;
        private float _v;

        private RayTracingMaterial _material;

        private Color _initialColor;
        
        private void OnValidate()
        {
            SetStepSize();
            Initialize();
        }

        private void OnEnable()
        {
            Initialize();
        }

        private void Start()
        {
            Initialize();
        }

        public override void Increment()
        {
            _h += _stepSize;
            _h = _h >= 1f ? _h - 1f : _h;

            ++NumberOfIterations;
            _material.emissionColor = Color.HSVToRGB(_h, _s, _v);
            whatToChangeColorOn.SetMaterial(_material);
        }
        private void SetStepSize() => _stepSize = stepSize % 360f / 360f;


        public override void Initialize()
        {
            _material = whatToChangeColorOn.GetMaterial();
            _initialColor = _material.emissionColor;
            Color.RGBToHSV(_material.emissionColor, out _h, out _s, out _v);
        }

        public override void ResetValues()
        {
            _material.emissionColor = _initialColor;
            Color.RGBToHSV(_material.emissionColor, out _h, out _s, out _v);
        }

    }
}