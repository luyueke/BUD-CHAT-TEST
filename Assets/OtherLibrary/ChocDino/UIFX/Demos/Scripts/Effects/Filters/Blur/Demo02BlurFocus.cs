//--------------------------------------------------------------------------//
// Copyright 2023-2024 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;
using UnityEngine.UI;
using ChocDino.UIFX;

namespace ChocDino.UIFX.Demos
{
	/// <summary>
	/// Shift "focus" between two UI elements by changing the blur strength
	/// </summary>
	public class Demo02BlurFocus : MonoBehaviour
	{
		[SerializeField] BlurFilter _blurFront = null;
		[SerializeField] BlurFilter _blurBack = null;

		void Start()
		{
			Time.timeScale = 1f;
		}
		
		void Update()
		{
			float t = Mathf.Sin(Time.time * 2f);
			t = (t * 0.5f) + 0.5f;
			t = Mathf.Clamp01(t);
			t = DemoUtils.InOutExpo(t);

			_blurFront.Strength = t;
			_blurBack.Strength = 1f - t;
		}
	}
}
