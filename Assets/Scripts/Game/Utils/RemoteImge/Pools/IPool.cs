using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

namespace Game.Utils
{
	public interface IPool
	{
		int Capacity { get; }

		object Get(object key);
		void Relase(object key);
		void Put(object key, object value);
		void Clear();
	}
}
