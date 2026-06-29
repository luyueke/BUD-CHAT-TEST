using GameData.BindProperties;
using UnityEngine;

namespace GameData.BindPropertyDefine
{
    public class BindVector3 : BindProperty<Vec3>
    {
        public BindVector3()
        {
        }

        public BindVector3(float x, float y, float z)
        {
            this.Value = new Vec3(x, y, z);
        }

        public BindVector3(Vec3 defaultValue) : base(defaultValue)
        {
        }

        public Vec3 Value
        {
            get => _value;

            set
            {
                if (_value == null || !_value.Equals(value))
                {
                    var oldV = _value == null ? null : new Vec3(_value.x, _value.y, _value.z);
                    _value = value;

                    OnValueChanged?.Invoke(oldV, _value);
                }
            }
        }

        public override BindProperty<Vec3> Copy()
        {
            return new BindVector3(Value);
        }

        public BindVector3 CopySelf()
        {
            return Copy() as BindVector3;
        }
    }
}