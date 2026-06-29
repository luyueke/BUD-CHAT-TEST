//--------------------------------------------------------------------------//
// Copyright 2023-2024 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ChocDino.UIFX;

namespace ChocDino.UIFX.Demos
{
	[RequireComponent(typeof(RectTransform))]
	[RequireComponent(typeof(FilterBase))]
	public class FilterHoverStrength : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
	{
		private FilterBase _filter;
		private bool _isOver = false;

		void Awake()
		{
			_filter = GetComponent<FilterBase>();

			UpdateAnimation(true);
		}

		void Update()
		{
			UpdateAnimation(false);
		}

		void UpdateAnimation(bool force)
		{
			const float dampSpeedFall = 6f;
			const float dampSpeedOver = 8f;

			float target = 0f;
			float dampSpeed = dampSpeedFall;
			if (_isOver)
			{
				target = 1f;
				dampSpeed = dampSpeedOver;
			}

			if (force)
			{
				_filter.Strength = target;
			}
			else if (Mathf.Abs(_filter.Strength - target) > 0.001f)
			{
				_filter.Strength = MathUtils.DampTowards(_filter.Strength, target, dampSpeed, Time.deltaTime);
			}
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			_isOver = true;
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			_isOver = false;
		}
	}
}