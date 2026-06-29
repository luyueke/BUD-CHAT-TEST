//--------------------------------------------------------------------------//
// Copyright 2023-2024 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;

namespace ChocDino.UIFX
{
	public class BoxBlurReference : ITextureBlur
	{
		const string BlurShaderId = "Hidden/ChocDino/UIFX/BoxBlur-Reference";

		internal static class ShaderProp
		{
			internal static readonly int KernelRadius = Shader.PropertyToID("_KernelRadius");
		}
		internal static class ShaderPass
		{
			internal const int Horizontal = 0;
			internal const int Vertical = 1;
		}

		public BlurAxes2D BlurAxes2D { get { return _blurAxes2D; } set { _blurAxes2D = value; } }
		public Downsample Downsample { get { return _downSample; } set { if (_downSample != value) { _downSample = value; _kernelDirty = true; } } }
		public float Strength { get { return _strength; } set { value = Mathf.Clamp01(value); if (_strength != value) { _strength = value; _kernelDirty = true; } } }

		const int _iterationCount = 3;
		private Downsample _downSample = Downsample.Auto;
		private float _blurSize = 0.05f;
		private float _strength = 1f;
		private Material _materialBlur;
		private RenderTexture _rtBlurH;
		private RenderTexture _rtBlurV;
		private RenderTexture _sourceTexture;
		private BlurAxes2D _blurAxes2D = BlurAxes2D.Default;

		private bool _materialsDirty = true;
		private bool _kernelDirty = true;
		private float _kernelRadius;
		private FilterBase _parentFilter = null;

		private BoxBlurReference() { }

		public BoxBlurReference(FilterBase parentFilter)
		{
			Debug.Assert(parentFilter != null);
			_parentFilter = parentFilter;
		}

		public void SetBlurSize(float diagonalPercent)
		{
			if (diagonalPercent != _blurSize)
			{
				_blurSize = diagonalPercent;
				_kernelDirty = true;
			}
		}

		public void AdjustBoundsSize(ref Vector2Int leftDown, ref Vector2Int rightUp)
		{
			// Get radius for box blur
			float radius = GetScaledRadius() * Strength;

			// Multiple iterations increases the radius area (simulating gaussian blur)
			// NOTE: This size is based off a solid white box which is the worst case, if
			// the contents of the image is dark then this radius could be significantly shrunk,
			// but there is no easy way to detect this. Also if the image is HDR then this expand
			// may be too small - perhaps expose option to the user.
			radius *= _iterationCount;
			radius *= GetDownsampleFactor();

			int x = Mathf.CeilToInt(radius);
			if (x > 0)
			{
				Vector2Int result = new Vector2Int(x, x);
				if (_blurAxes2D == BlurAxes2D.Horizontal)
				{
					result.y = 0;
				}
				else if (_blurAxes2D == BlurAxes2D.Vertical)
				{
					result.x = 0;
				}
				leftDown += result;
				rightUp += result;
			}
		}

		public Vector2Int GetKernelRadius()
		{
			int x = Mathf.CeilToInt(_kernelRadius);
			Vector2Int result = new Vector2Int(x, x);
			if (_blurAxes2D == BlurAxes2D.Horizontal)
			{
				result.y = 0;
			}
			else if (_blurAxes2D == BlurAxes2D.Vertical)
			{
				result.x = 0;
			}
			return result;
		}

		public RenderTexture Process(RenderTexture sourceTexture)
		{
			Debug.Assert(sourceTexture != null);

			RenderTexture prevRT = RenderTexture.active;

			SetupResources(sourceTexture);

			if (_kernelDirty)
			{
				UpdateKernel(_sourceTexture);
			}

			if (_materialsDirty)
			{
				UpdateMaterials();
			}

			RenderTexture src = _sourceTexture;

			if (GetScaledRadius() > 0f)
			{
				// Have to downsample first otherwise it will be biased in the first blur pass direction
				// leading to slightly stretched result
				if (GetDownsampleFactor() > 1)
				{
					Graphics.Blit(src, _rtBlurV);
					_rtBlurV.IncrementUpdateCount();
					src = _rtBlurV;
				}

				// Blur
				for (int i = 0; i < _iterationCount; i++)
				{
					if (_blurAxes2D == BlurAxes2D.Default)
					{
						Graphics.Blit(src, _rtBlurH, _materialBlur, 0);
						_rtBlurH.IncrementUpdateCount();
						Graphics.Blit(_rtBlurH, _rtBlurV, _materialBlur, 1);
						_rtBlurV.IncrementUpdateCount();
						src = _rtBlurV;
					}
					else
					{
						int pass = (_blurAxes2D == BlurAxes2D.Horizontal) ? ShaderPass.Horizontal : ShaderPass.Vertical;
						bool isOdd = (i&1) != 0;
						if (!isOdd)
						{
							Graphics.Blit(src, _rtBlurH, _materialBlur, pass);
							_rtBlurH.IncrementUpdateCount();
							src = _rtBlurH;
						}
						else
						{
							Graphics.Blit(src, _rtBlurV, _materialBlur, pass);
							_rtBlurV.IncrementUpdateCount();
							src = _rtBlurV;
						}
					}
				}

				// TODO: should we upsample here, or just rely on bilinear filtering?
			}

			RenderTexture.active = prevRT;

			return src;
		}

		public void FreeResources()
		{
			FreeShaders();
			FreeTextures();
		}

		private uint _currentTextureHash;

		private uint CreateTextureHash(int width, int height)
		{
			uint hash = 0;
			hash = (hash | (uint)width) << 13;
			hash = (hash | (uint)height) << 13;
			return hash;
		}

		void SetupResources(RenderTexture sourceTexture)
		{
			uint desiredTextureProps = 0;
			if (sourceTexture != null)
			{
				desiredTextureProps = CreateTextureHash(sourceTexture.width / GetDownsampleFactor(), sourceTexture.height / GetDownsampleFactor());
			}

			if (desiredTextureProps != _currentTextureHash)
			{
				FreeTextures();
				_kernelDirty = true;
			}
			if (_sourceTexture == null && sourceTexture != null)
			{
				CreateTextures(sourceTexture);
				_currentTextureHash = desiredTextureProps;
			}
			
			if (_sourceTexture != sourceTexture)
			{
				_materialsDirty = true;
				_sourceTexture = sourceTexture;
			}
			if (_materialBlur == null)
			{
				CreateShaders();
			}
		}

		private float GetScaledRadius()
		{
			float downsampleScale = 1f / (float)GetDownsampleFactor();
			return _blurSize * _parentFilter.ResolutionScalingFactor * downsampleScale;
		}

		void UpdateKernel(RenderTexture sampledTexture)
		{
			_kernelRadius = GetScaledRadius() * Strength;
			_kernelDirty = false;
			_materialsDirty = true;
		}

		void UpdateMaterials()
		{
			_materialBlur.SetFloat(ShaderProp.KernelRadius, _kernelRadius);
			_materialsDirty = false;
		}

		static Material CreateMaterialFromShader(string shaderName)
		{
			Material result = null;
			Shader shader = Shader.Find(shaderName);
			if (shader != null)
			{
				result = new Material(shader);
			}
			return result;
		}

		void CreateShaders()
		{
			_materialBlur = CreateMaterialFromShader(BlurShaderId);
			Debug.Assert(_materialBlur != null);
			_materialsDirty = true;
		}

		void CreateTextures(RenderTexture sourceTexture)
		{
			int w = sourceTexture.width / GetDownsampleFactor();
			int h = sourceTexture.height / GetDownsampleFactor();

			RenderTextureFormat format = sourceTexture.format;
			if ((Filters.PerfHint & PerformanceHint.UseMorePrecision) != 0)
			{
				// TODO: create based on the input texture format, but just with more precision
				format = RenderTextureFormat.ARGBHalf;
			}

			_rtBlurH = RenderTexture.GetTemporary(w, h, 0, format, RenderTextureReadWrite.Linear);
			_rtBlurV = RenderTexture.GetTemporary(w, h, 0, format, RenderTextureReadWrite.Linear);

			#if UNITY_EDITOR
			_rtBlurH.name = "BlurH";
			_rtBlurV.name = "BlurV";
			#endif
		}

		void FreeShaders()
		{
			ObjectHelper.Destroy(ref _materialBlur);
		}

		void FreeTextures()
		{
			RenderTextureHelper.ReleaseTemporary(ref _rtBlurV);
			RenderTextureHelper.ReleaseTemporary(ref _rtBlurH);
			_currentTextureHash = 0;
			_sourceTexture = null;
		}

		private int GetDownsampleFactor()
		{
			int result = 1;
			if (_downSample == Downsample.Auto)
			{
				if ((Filters.PerfHint & PerformanceHint.AllowDownsampling) != 0)
				{
					result = 2;
				}
			}
			else
			{
				result = (int)_downSample;
			}
			return result;
		}
	}
}