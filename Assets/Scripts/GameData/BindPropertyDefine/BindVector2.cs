using GameData.BindProperties;
using UnityEngine;

namespace GameData.BindPropertyDefine
{
    public class BindVector2 : BindProperty<Vec2>
    {
        public BindVector2()
        {
        }

        public BindVector2(float x, float y)
        {
            this.Value = new Vec2(x, y);
        }

        public BindVector2(Vec2 defaultValue) : base(defaultValue)
        {
        }

        public Vec2 Value
        {
            get => _value;

            set
            {
                if (_value == null || !_value.Equals(value))
                {
                    var oldV = _value == null ? null : new Vec2(_value.x, _value.y);
                    _value = value;

                    OnValueChanged?.Invoke(oldV, _value);
                }
            }
        }

        public override BindProperty<Vec2> Copy()
        {
            return new BindVector2(Value);
        }

        public BindVector2 CopySelf()
        {
            return Copy() as BindVector2;
        }
    }
}