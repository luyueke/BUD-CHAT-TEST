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
	public enum DropShadowMode
	{
		Default,
		Inset,
		Glow,
	}

	/// <summary>
	/// A drop shadow filter for uGUI components
	/// </summary>
	[ExecuteInEditMode]
	[AddComponentMenu("UI/Chocolate Dinosaur UIFX/Filters/UIFX - Drop Shadow Filter")]
	public class DropShadowFilter : FilterBase
	{
		[Tooltip("How much to downsample before blurring")]
		[SerializeField] Downsample _downSample = Downsample.Auto;

		[Tooltip("The maximum size of the blur kernel as a fraction of the diagonal length.  So 0.01 would be a kernel with pixel dimensions of 1% of the diagonal length.")]
		[Range(0f, 32f)]
		[SerializeField] float _blur = 0f;
		
		[Tooltip("The transparency of the source content. Set to zero to make only the outline show.")]
		[Range(0f, 1f)]
		[SerializeField] float _sourceAlpha = 1.0f;

		[Tooltip("The clockwise angle the shadow is cast at. Range is [0..360]. Default is 135.0")]
		[Range(0f, 360f)]
		[SerializeField] float _angle = 180f;

		[Tooltip("The distance the shadow is cast. Range is [0..1]. Default is 0.03")]
		[Range(0f, 256f)]
		[SerializeField] float _distance = 3f;

		[Tooltip("The hardness of the shadow [0..4]. Default is 1.0")]
		[Range(0f, 4f)]
		[SerializeField] float _hardness = 1f;

		[Tooltip("The color of the shadow")]
		[SerializeField] Color _color = Color.black;

		[Tooltip("The mode to use for rendering. Default casts the shadow outside, Inset casts the shadow inside.")]
		[SerializeField] DropShadowMode _mode = DropShadowMode.Default;

		/// <summary>How much to downsample before blurring</summary>
		public Downsample Downsample { get { return _downSample; } set { ChangeProperty(ref _downSample, value); } }

		/// <summary>The maximum size of the blur kernel as a fraction of the diagonal length.  So 0.01 would be a kernel with pixel dimensions of 1% of the diagonal length.</summary>
		public float Blur { get { return _blur; } set { ChangeProperty(ref _blur, value); } }

		/// <summary>The transparency of the source content. Set to zero to make only the outline show. Range is [0..1] Default is 1.0</summary>
		public float SourceAlpha { get { return _sourceAlpha; } set { ChangeProperty(ref _sourceAlpha, Mathf.Clamp01(value)); } }

		/// <summary>The clockwise angle the shadow is cast at. Range is [0..360]. Default is 135.0</summary>
		public float Angle { get { return _angle; } set { ChangeProperty(ref _angle, value); } }

		/// <summary>The distance the shadow is cast. Range is [0..1]. Default is 0.03</summary>
		public float Distance { get { return _distance; } set { ChangeProperty(ref _distance, value); } }

		/// <summary>The hardness of the shadow [0..2]. Default is 0.5</summary>
		public float Hardness { get { return _hardness; } set { ChangeProperty(ref _hardness, value); } }

		/// <summary>The color of the shadow</summary>
		public Color Color { get { return _color; } set { ChangeProperty(ref _color, value); } }

		/// <summary>The mode to use for rendering. Default casts the shadow outside, Inset casts the shadow inside.</summary>
		public DropShadowMode Mode { get { return _mode; } set { ChangeProperty(ref _mode, value); } }

		private const string BlendDropShadowShaderPath = "Hidden/ChocDino/UIFX/Blend-DropShadow";

		private ITextureBlur _blurfx = null;

		static new class ShaderProp
		{
			public readonly static int SourceAlpha = Shader.PropertyToID("_SourceAlpha");
			public readonly static int ShadowOffset = Shader.PropertyToID("_ShadowOffset");
			public readonly static int ShadowHardness = Shader.PropertyToID("_ShadowHardness");
			public readonly static int ShadowColor = Shader.PropertyToID("_ShadowColor");
		}
		static class ShaderKeyword
		{
			public const string Inset = "INSET";
			public const string Glow = "GLOW";
		}

		protected override bool CanApplyFilter()
		{
			if (_blurfx == null ) return false;
			return base.CanApplyFilter();
		}

		protected override bool DoParametersModifySource()
		{
			if (_sourceAlpha < 1f) return true;
			if (_hardness <= 0f) return false;
			if (_color.a <= 0f) return false;
			if (this.Strength <= 0f) return false;
			if (_distance > 0f) return true;
			if (_blur <= 0f) return false;
			return base.DoParametersModifySource();
		}

		protected override string GetDisplayShaderPath()
		{
			return BlendDropShadowShaderPath;
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
		
		private static Vector2 AngleToOffset(float angle, Vector2 scale)
		{
			return new Vector2(Mathf.Sin(-angle * Mathf.Deg2Rad) * scale.x, Mathf.Cos(-angle * Mathf.Deg2Rad + Mathf.PI) * scale.y);
		}

		protected override void GetFilterAdjustSize(ref Vector2Int leftDown, ref Vector2Int rightUp)
		{
			float maxDistance = _distance * _strength * ResolutionScalingFactor;
			if (maxDistance > 0f)
			{
				Vector2 offset = -AngleToOffset(_angle, Vector2.one) * maxDistance;
				leftDown += new Vector2Int(Mathf.CeilToInt(Mathf.Abs(Mathf.Min(0f, offset.x))), Mathf.CeilToInt(Mathf.Abs(Mathf.Min(0f, offset.y))));
				rightUp += new Vector2Int(Mathf.CeilToInt(Mathf.Max(0f, offset.x)), Mathf.CeilToInt(Mathf.Max(0f, offset.y)));
			}
			if (_blurfx != null)
			{
				SetupFilterParams();
				_blurfx.AdjustBoundsSize(ref leftDown, ref rightUp);
			}
		}

		protected override void SetupDisplayMaterial(Texture source, Texture result)
		{
			{
				Vector2 shadowPixelOffset = AngleToOffset(_angle, Vector2.one);
				shadowPixelOffset *= _distance * _strength;
				shadowPixelOffset *= ResolutionScalingFactor;
				Vector2 texelStep = new Vector2(1f / source.width, 1f / source.height);
				shadowPixelOffset *= texelStep;
				_displayMaterial.SetVector(ShaderProp.ShadowOffset, shadowPixelOffset);
			}

			_displayMaterial.SetFloat(ShaderProp.SourceAlpha, Mathf.LerpUnclamped(1f, _sourceAlpha, this.Strength));
			_displayMaterial.SetFloat(ShaderProp.ShadowHardness, _hardness);

			{
				Color color = _color;
				//color.a = Mathf.LerpUnclamped(0f, color.a, this.Strength);
				_displayMaterial.SetColor(ShaderProp.ShadowColor, color);
			}

			switch(_mode)
			{
				case DropShadowMode.Default:
					_displayMaterial.DisableKeyword(ShaderKeyword.Inset);
					_displayMaterial.DisableKeyword(ShaderKeyword.Glow);
					break;
				case DropShadowMode.Inset:
					_displayMaterial.EnableKeyword(ShaderKeyword.Inset);
					_displayMaterial.DisableKeyword(ShaderKeyword.Glow);
					break;
				case DropShadowMode.Glow:
					_displayMaterial.DisableKeyword(ShaderKeyword.Inset);
					_displayMaterial.EnableKeyword(ShaderKeyword.Glow);
					break;
			}

			base.SetupDisplayMaterial(source, result);
		}

		private void SetupFilterParams()
		{
			_blurfx.Downsample = _downSample;
			_blurfx.SetBlurSize(_blur * _strength);
		}

		protected override RenderTexture RenderFilters(RenderTexture source)
		{
			if (_blur > 0f)
			{
				SetupFilterParams();
				return _blurfx.Process(source);
			}
			return source;
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