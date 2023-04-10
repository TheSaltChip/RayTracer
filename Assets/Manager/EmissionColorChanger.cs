using Objects;
using UnityEngine;
using static Structs;

namespace Manager
{
    public class EmissionColorChanger : Changer
    {
        [SerializeField] private BaseObject whatToChangeColorOn;
        [SerializeField, Range(0,359)] private float stepSize;

        private float _stepSize;

        private float _h;
        private float _s;
        private float _v;

        private float _startH;

        private Mat _material;

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
            IsDone = _h >= 1f + _startH;

            if (IsDone) return;
            
            ++Number;
            _material.emissionColor = Color.HSVToRGB(_h, _s, _v);
            whatToChangeColorOn.SetMaterial(_material);
        }

        private void SetStepSize() => _stepSize = stepSize % 360f / 360f;

        public override string FileName()
        {
            return $"/Screenshot_num{Number:0000}.png";
        }


        public override void Initialize()
        {
            _material = whatToChangeColorOn.GetMaterial();
            Color.RGBToHSV(_material.emissionColor, out _h, out _s, out _v);
            _startH = _h;
        }
    }
}