//--------------------------------------------------------------------------//
// Copyright 2023-2024 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;

namespace ChocDino.UIFX
{
	public class ErodeDilate
	{
		const string ShaderId = "Hidden/ChocDino/UIFX/ErodeDilate";

		private static class ShaderProp
		{
			internal static int ErodeRadius = Shader.PropertyToID("_ErodeRadius");
			internal static int DilateRadius = Shader.PropertyToID("_DilateRadius");
		}
		private static class ShaderKeyword
		{
			public const string DistSquare = "DIST_SQUARE";
			public const string DistDiamond = "DIST_DIAMOND";
			public const string DistCircle = "DIST_CIRCLE";
		}
		private static class ShaderPass
		{
			internal const int ErodeAlpha = 0;
			internal const int DilateAlpha = 1;
			internal const int ErodeDilateAlpha = 2;
			internal const int Dilate = 3;
			internal const int CopyAlpha = 4;
			internal const int Null = 5;
		}

		public float ErodeSize { get { return _erodeSize; } set { ChangeProperty(ref _erodeSize, value); } }
		public float DilateSize { get { return _dilateSize; } set { ChangeProperty(ref _dilateSize, value); } }
		public DistanceShape DistanceShape { get { return _distanceShape; } set { ChangeProperty(ref _distanceShape, value); } }
		public bool AlphaOnly { get { return _alphaOnly; } set { ChangeProperty(ref _alphaOnly, value); } }

		internal RenderTexture OutputTexture { get { return _output; } }

		private float _erodeSize = 0f;
		private float _dilateSize = 0f;
		private DistanceShape _distanceShape = DistanceShape.Circle;
		private bool _alphaOnly = false;

		private Material _material;
		private RenderTexture _rtAlphaSource;
		private RenderTexture _rtResult;
		private RenderTexture _sourceTexture;
		private RenderTexture _output;

		private bool _materialsDirty = true;
		private FilterBase _parentFilter = null;

		private ErodeDilate() { }

		public ErodeDilate(FilterBase parentFilter)
		{
			Debug.Assert(parentFilter != null);
			_parentFilter = parentFilter;
		}

		private void ChangeProperty<T>(ref T backing, T value) where T : struct
		{ 
			if (ObjectHelper.ChangeProperty(ref backing, value))
			{
				backing = value;
				_materialsDirty = true;
			}
		}

		public void AdjustBoundsSize(ref Vector2Int leftDown, ref Vector2Int rightUp)
		{
			float maxSize = GetScaledSize(_dilateSize);
			int size = Mathf.CeilToInt(maxSize);
			if (size > 0)
			{
				leftDown += new Vector2Int(size, size);
				rightUp += new Vector2Int(size, size);
			}
		}

		public RenderTexture Process(RenderTexture sourceTexture)
		{
			Debug.Assert(sourceTexture != null);

			RenderTexture prevRT = RenderTexture.active;

			SetupResources(sourceTexture);

			if (_materialsDirty)
			{
				UpdateMaterials();
			}

			RenderTexture source = _sourceTexture;
			_output = _sourceTexture;

			if (_alphaOnly)
			{
				Graphics.Blit(_sourceTexture, _rtAlphaSource, _material, ShaderPass.CopyAlpha);
				_rtAlphaSource.IncrementUpdateCount();
				_output = source = _rtAlphaSource;
			}

			if (_erodeSize <= 0f)
			{
				if (_dilateSize > 0f)
				{
					Graphics.Blit(source, _rtResult, _material, _alphaOnly?ShaderPass.DilateAlpha:ShaderPass.Dilate);
					_rtResult.IncrementUpdateCount();
					_output = _rtResult;
				}
				else
				{
					//Graphics.Blit(_sourceTexture, _rtH, _material, ShaderPass.Null);
					//_rtH.IncrementUpdateCount();
					//_output = _rtH;
				}
			}
			else
			{
				if (_dilateSize > 0f)
				{
					Graphics.Blit(source, _rtResult, _material, ShaderPass.ErodeDilateAlpha);
					_rtResult.IncrementUpdateCount();
					_output = _rtResult;
				}
				else
				{
					Graphics.Blit(source, _rtResult, _material, ShaderPass.ErodeAlpha);
					_rtResult.IncrementUpdateCount();
					_output = _rtResult;
				}
			}

			RenderTexture.active = prevRT;

			return _output;
		}

		public void FreeResources()
		{
			FreeShaders();
			FreeTextures();
		}

		private uint _currentTextureHash;

		private uint CreateTextureHash(int width, int height, bool alphaOnly)
		{
			uint hash = 0;
			hash = (hash | (uint)width) << 13;
			hash = (hash | (uint)height) << 13;
			hash = (hash | (uint)(alphaOnly?0:1));
			return hash;
		}

		void SetupResources(RenderTexture sourceTexture)
		{
			uint desiredTextureProps = 0;
			if (sourceTexture != null)
			{
				desiredTextureProps = CreateTextureHash(sourceTexture.width, sourceTexture.height, _alphaOnly);
			}

			if (desiredTextureProps != _currentTextureHash)
			{
				FreeTextures();
				_materialsDirty = true;
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
			if (_material == null)
			{
				CreateShaders();
			}
		}

		private float GetScaledSize(float size)
		{
			return size * _parentFilter.ResolutionScalingFactor;
		}

		void UpdateMaterials()
		{
			_material.SetFloat(ShaderProp.ErodeRadius, GetScaledSize(_erodeSize));
			_material.SetFloat(ShaderProp.DilateRadius, GetScaledSize(_dilateSize));
			if (_distanceShape == DistanceShape.Square)
			{
				_material.DisableKeyword(ShaderKeyword.DistDiamond);
				_material.DisableKeyword(ShaderKeyword.DistCircle);
				_material.EnableKeyword(ShaderKeyword.DistSquare);
			}
			else if (_distanceShape == DistanceShape.Diamond)
			{
				_material.DisableKeyword(ShaderKeyword.DistSquare);
				_material.DisableKeyword(ShaderKeyword.DistCircle);
				_material.EnableKeyword(ShaderKeyword.DistDiamond);
			}
			else if (_distanceShape == DistanceShape.Circle)
			{
				_material.DisableKeyword(ShaderKeyword.DistSquare);
				_material.DisableKeyword(ShaderKeyword.DistDiamond);
				_material.EnableKeyword(ShaderKeyword.DistCircle);
			}
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
			_material = CreateMaterialFromShader(ShaderId);
			Debug.Assert(_material != null);
			_materialsDirty = true;
		}

		void CreateTextures(RenderTexture sourceTexture)
		{
			int w = sourceTexture.width / 1;
			int h = sourceTexture.height / 1;

			RenderTextureFormat format = RenderTextureFormat.ARGBHalf;
			if ((Filters.PerfHint & PerformanceHint.UseLessPrecision) != 0)
			{
				format = RenderTextureFormat.ARGB32;
			}
			if (_alphaOnly)
			{
				format = RenderTextureFormat.RHalf;
				if ((Filters.PerfHint & PerformanceHint.UseLessPrecision) != 0)
				{
					format = RenderTextureFormat.R8;
				}
			}

			if (_alphaOnly)
			{
				_rtAlphaSource = RenderTexture.GetTemporary(w, h, 0, format, RenderTextureReadWrite.Linear);
				#if UNITY_EDITOR
				_rtAlphaSource.name = "AlphaSource";
				#endif
			}

			_rtResult = RenderTexture.GetTemporary(w, h, 0, format, RenderTextureReadWrite.Linear);

			#if UNITY_EDITOR
			_rtResult.name = "ErodeDilate";
			#endif
		}

		void FreeShaders()
		{
			ObjectHelper.Destroy(ref _material);
		}

		void FreeTextures()
		{
			RenderTextureHelper.ReleaseTemporary(ref _rtAlphaSource);
			RenderTextureHelper.ReleaseTemporary(ref _rtResult);
			_currentTextureHash = 0;
			_sourceTexture = null;
			_output = null;
		}
	}
}