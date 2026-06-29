//--------------------------------------------------------------------------//
// Copyright 2023-2024 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityInternal = UnityEngine.Internal;

namespace ChocDino.UIFX
{
	/// <summary>
	/// A blur filter for uGUI components
	/// </summary>
	[ExecuteInEditMode]
	[AddComponentMenu("UI/Chocolate Dinosaur UIFX/Filters/UIFX - Blur Filter")]
	public partial class BlurFilter : FilterBase
	{
		[Tooltip("How much to downsample before blurring")]
		[SerializeField] Downsample _downSample = Downsample.Auto;

		[Tooltip("Which axes to blur")]
		[SerializeField] BlurAxes2D _blurAxes2D = BlurAxes2D.Default;

		[Tooltip("The maximum size of the blur kernel as a fraction of the diagonal length.  So 0.01 would be a kernel with pixel dimensions of 1% of the diagonal length.")]
		[Range(0f, 128f)]
		[SerializeField] float _blur = 4f;

		[Tooltip("Toggle the use of the alpha curve to fade to transparent as blur Strength increases")]
		[SerializeField] bool _applyAlphaCurve = true;

		[Tooltip("An optional curve to allow the Graphic to fade to transparent as the blur Strength property increases")]
		[SerializeField] AnimationCurve _alphaCurve = null;

		/// <summary>How much to downsample before blurring</summary>
		public Downsample Downsample { get { return _downSample; } set { ChangeProperty(ref _downSample, value); } }

		/// <summary>Which axes to blur</summary>
		public BlurAxes2D BlurAxes2D { get { return _blurAxes2D; } set { ChangeProperty(ref _blurAxes2D, value); } }

		/// <summary>The maximum size of the blur kernel as a fraction of the diagonal length.  So 0.01 would be a kernel with pixel dimensions of 1% of the diagonal length.</summary>
		public float Blur { get { return _blur; } set { ChangeProperty(ref _blur, value); } }

		/// <summary>Toggle the use of the alpha curve to fade to transparent as blur Strength increases</summary>
		public bool ApplyAlphaCurve { get { return _applyAlphaCurve; } set { ChangeProperty(ref _applyAlphaCurve, value); } }

		/// <summary>An optional curve to allow the Graphic to fade to transparent as the blur Strength property increases</summary>
		public AnimationCurve AlphaCurve { get { return _alphaCurve; } set { ChangePropertyRef(ref _alphaCurve, value); } }

		private float _lastGlobalStrength = 1f;

		/// <summary>A global scale for Strength which can be useful to easily adjust Strength across all instances of BlurFilter.  Range [0..1] Default is 1.0</summary>
		public static float GlobalStrength = 1f;

		private const string Keyword_BlendOver = "BLEND_OVER";
		private const string Keyword_BlendUnder = "BLEND_UNDER";

		private ITextureBlur _blurfx = null;

		protected override bool CanApplyFilter()
		{
			if (_blurfx == null ) return false;
			return base.CanApplyFilter();
		}

		protected override bool DoParametersModifySource()
		{
			if (_blur <= 0f) return false;
			if (GetStrength() <= 0f) return false;
			return true;
		}

		protected override void OnEnable()
		{
			_blurfx = new BoxBlurReference(this);
			base.OnEnable();
		}

		protected override void OnDisable()
		{
			if (_blurfx != null)
			{
				_blurfx.FreeResources();
				_blurfx = null;
			}
			base.OnDisable();
		}

		#if UNITY_EDITOR
		protected override void OnValidate()
		{
			// OnValidate is called when the scene is saved, which causes the materials to lose their properties, so we force update them here.
			if (_blurfx != null)
			{
				float strength = _blurfx.Strength;
				_blurfx.Strength = 0f;
				_blurfx.Strength = strength;
			}
			
			base.OnValidate();
		}
		#endif

		protected override void Update()
		{
			// If GlobalStrength has changed, then force update
			if (GlobalStrength != _lastGlobalStrength)
			{
				_lastGlobalStrength = GlobalStrength;
				OnPropertyChange();
			}

			base.Update();
		}

		/// <summary>
		/// SetGlobalStrength() allows Unity Events "Dynamic Float" to set the Global Strength static property
		/// </summary>
		public void SetGlobalStrength(float value)
		{
			GlobalStrength = value;
		}

		private float GetStrength()
		{
			return _strength * GlobalStrength;
		}

		protected override float GetAlpha()
		{
			float alpha = 1f;
			if (_alphaCurve != null && _applyAlphaCurve)
			{
				if (_alphaCurve.length > 0)
				{
					alpha = _alphaCurve.Evaluate(GetStrength());
				}
			}
			return alpha;
		}
/*
		protected override void SetupDisplayMaterial(Texture source, Texture result)
		{
			//_displayMaterial.EnableKeyword(Keyword_BlendOver);
			//_displayMaterial.DisableKeyword(Keyword_BlendUnder);

			_displayMaterial.DisableKeyword(Keyword_BlendOver);
			_displayMaterial.EnableKeyword(Keyword_BlendUnder);

			base.SetupDisplayMaterial(source, result);
		}
*/

		protected override void GetFilterAdjustSize(ref Vector2Int leftDown, ref Vector2Int rightUp)
		{
			if (_blurfx != null)
			{
				SetupFilterParams();
				_blurfx.AdjustBoundsSize(ref leftDown, ref rightUp);
			}
		}

		private void SetupFilterParams()
		{
			if (_blurfx != null)
			{
				_blurfx.BlurAxes2D = _blurAxes2D;
				_blurfx.Downsample = _downSample;
				_blurfx.SetBlurSize(_blur);
				_blurfx.Strength = GetStrength();
			}
		}

		protected override RenderTexture RenderFilters(RenderTexture source)
		{
			SetupFilterParams();
			return _blurfx.Process(source);
		}

		internal override string GetDebugString()
		{
			string result = base.GetDebugString();
			{
				Vector2Int leftDown = Vector2Int.zero;
				Vector2Int rightUp = Vector2Int.zero;
				GetFilterAdjustSize(ref leftDown, ref rightUp);
				result += "Expand: [" + leftDown + " - " + rightUp + "]\n";
			}
			if (_blurfx != null)
			{
				result += "Kernel: " + _blurfx.GetKernelRadius().ToString() + "\n";
			}
			return result;
		}
	}
}