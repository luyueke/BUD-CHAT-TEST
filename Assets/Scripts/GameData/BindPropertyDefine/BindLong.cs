using GameData.BindProperties;

namespace GameData.BindPropertyDefine
{
    public class BindLong : BindProperty<long>
    {
        public BindLong()
        {
        }

        public BindLong(long defaultValue) : base(defaultValue)
        {
        }

        public long Value
        {
            get => _value;

            set
            {
                if (_value != value)
                {
                    long oldV = _value;
                    _value = value;

                    OnValueChanged?.Invoke(oldV, _value);
                }
            }
        }

        public override BindProperty<long> Copy()
        {
            return new BindLong(Value);
        }

        public BindLong CopySelf()
        {
            return Copy() as BindLong;
        }
    }
}