using System.Collections.Generic;

namespace Game.Utils
{
	public class FIFOCachingPool : IPool
	{
		public delegate void ObjectDestroyer(object key, object value);

		public int Capacity { get; private set; }
		public int CurrentCount { get { return _Keys.Count; } }

		readonly List<object> _Keys;
		readonly Dictionary<object, object> _Cache;
		readonly ObjectDestroyer _ObjectDestroyer;


		/// <summary>First in, First out caching</summary>
		/// <param name="capacity"></param>
		/// <param name="objectDestroyer">
		/// When an object is kicked out of the cache, this will be used to process its destruction, 
		/// in case special code needs to be executed. This is also called for each value when the cache is cleared usinc <see cref="Clear"/>
		/// </param>
		public FIFOCachingPool(int capacity, ObjectDestroyer objectDestroyer = null)
		{
			Capacity = capacity;
			_Keys = new List<object>(capacity);
			_Cache = new Dictionary<object, object>(capacity);
			_ObjectDestroyer = objectDestroyer;
		}


		public object Get(object key)
		{
			object value;
			if (_Keys.Contains(key))
			{
				_Keys.Remove(key);
				_Keys.Add(key);
			}
			if (_Cache.TryGetValue(key, out value))
			{
				return value;
			}
			return null;
		}

		public void Relase(object key)
		{
			var index = _Keys.IndexOf(key);
			if (index >= 0)
			{
				object keyToDiscard = _Keys[index];
				_Keys.RemoveAt(index);
				object oldValue = _Cache[keyToDiscard];
				_Cache.Remove(keyToDiscard);
				if (_ObjectDestroyer != null)
					_ObjectDestroyer(keyToDiscard, oldValue);
			}
		}

		public void Put(object key, object value)
		{
			if (CurrentCount == Capacity&& _Keys.Count > 0)
			{
				object keyToDiscard = _Keys[0];
				_Keys.RemoveAt(0);
				object oldValue = _Cache[keyToDiscard];
				_Cache.Remove(keyToDiscard);

				if (_ObjectDestroyer != null)
					_ObjectDestroyer(keyToDiscard, oldValue);
			}

			_Keys.Add(key);
			_Cache[key] = value;
		}

		public void Clear()
		{
			if (_ObjectDestroyer != null)
			{
				foreach (var kv in _Cache)
				{
					if (kv.Value != null)
						_ObjectDestroyer(kv.Key, kv.Value);
				}
			}
			_Keys.Clear();
			_Cache.Clear();
		}
	}
}
