using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;
using Object = System.Object;

namespace GameData
{
    public abstract class DataObject<T>
    {
        [NonSerialized] private UnityEvent<T, T> _onValueChanged;

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

        public abstract void SetData(T newData);

        public virtual T Copy()
        {
            return default;
        }
    }
}