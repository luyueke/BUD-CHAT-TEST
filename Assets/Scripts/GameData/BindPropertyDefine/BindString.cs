using GameData.BindProperties;

namespace GameData.BindPropertyDefine
{
    public class BindString : BindProperty<string>
    {
        public BindString()
        {
        }

        public BindString(string defaultValue) : base(defaultValue)
        {
        }

        public string Value
        {
            get => _value;

            set
            {
                if (!string.Equals(_value, value))
                {
                    var oldV = _value;
                    _value = value;

                    OnValueChanged?.Invoke(oldV, _value);
                }
            }
        }

        public override BindProperty<string> Copy()
        {
            return new BindString(Value);
        }

        public BindString CopySelf()
        {
            return Copy() as BindString;
        }
    }
}