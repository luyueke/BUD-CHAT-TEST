using GameData.BindProperties;

namespace GameData.BindPropertyDefine
{
    public class BindFloat : BindProperty<float>
    {
        public BindFloat()
        {
        }

        public BindFloat(float defaultValue) : base(defaultValue)
        {
        }

        public float Value
        {
            get => _value;

            set
            {
                if (_value != value)
                {
                    float oldV = _value;
                    _value = value;

                    OnValueChanged?.Invoke(oldV, _value);
                }
            }
        }

        public override BindProperty<float> Copy()
        {
            return new BindFloat(Value);
        }

        public BindFloat CopySelf()
        {
            return Copy() as BindFloat;
        }
    }
}