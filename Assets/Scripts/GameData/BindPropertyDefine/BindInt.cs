using System;
using UnityEngine.Events;

namespace GameData.BindPropertyDefine
{
    public class BindInt
    {
        protected int _value;
        
        [NonSerialized]
        private UnityEvent<int, int> _onValueChanged;
        
        public UnityEvent<int, int> OnValueChanged
        {
            get
            {
                if (_onValueChanged == null)
                {
                    _onValueChanged = new UnityEvent<int, int>();
                }

                return _onValueChanged;
            }
        }
        public BindInt(int defaultValue)
        {
            _value = defaultValue;
        }

        public int Value
        {
            get => _value;

            set
            {
                if (_value != value)
                {
                    int oldV = _value;
                    _value = value;

                    OnValueChanged?.Invoke(oldV, _value);
                }
            }
        }

        public BindInt Copy()
        {
            return new BindInt(Value);
        }

        public BindInt CopySelf()
        {
            return Copy() as BindInt;
        }
    }
}