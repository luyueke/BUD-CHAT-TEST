using System;
using Newtonsoft.Json;
using UnityEngine.Events;

namespace GameData.BindProperties
{
    public class BindProperty<T>
    {
        public BindProperty()
        {
        }

        public BindProperty(T defaultValue)
        {
            this.Value = defaultValue;
        }

        [NonSerialized]
        private UnityEvent<T, T> _onValueChanged;
        
        [JsonIgnore]
        public UnityEvent<T, T> OnValueChanged
        {
            get
            {
                if (_onValueChanged == null)
                {
                    _onValueChanged = new UnityEvent<T, T>();
                }

                return _onValueChanged;
            }
        }

        protected T _value;

        public T Value
        {
            get => _value;

            set
            {
                // 值类型会导致装箱，使用BindInt等继承类 
                if (!object.Equals(_value, value))
                {
                    T oldV = _value;
                    _value = value;

                    OnValueChanged?.Invoke(oldV, _value);
                }
            }
        }

        public virtual BindProperty<T> Copy()
        {
            return new BindProperty<T>(_value);
        }
    }
}