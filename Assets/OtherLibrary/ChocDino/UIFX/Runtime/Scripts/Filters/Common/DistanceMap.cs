//--------------------------------------------------------------------------//
// Copyright 2023-2024 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;

namespace ChocDino.UIFX
{
	public enum DistanceShape
	{
		Square,
		Diamond,
		Circle,
	}

	public enum DistanceMapResult
	{
		Outside,
		Inside,
		InOutMax,
		SDF,
	}

	/// <summary>
	/// Convert alpha channel to a distance map
	/// </summary>
	public class DistanceMap
	{
		const string ShaderId = "Hidden/ChocDino/UIFX/DistanceMap";

		static class ShaderProp
		{
			public static int StepSize = Shader.PropertyToID("_StepSize");
			public static int DownSample = Shader.PropertyToID("_DownSample");
			public static int InsideTex = Shader.PropertyToID("_InsideTex");
		}
		static class ShaderPass
		{
			public const int AlphaToUV = 0;
			public const int InvAlphaToUV = 1;
			public const int JumpFlood = 2;
			public const int JumpFloodSingleAxis = 3;
			public const int ResolveDistance = 4;
			public const int ResolveDistanceInOutMax = 5;
			public const int ResolveDistanceSDF = 6;
		}
		static class ShaderKeyword
		{
			public const string DistSquare = "DIST_SQUARE";
			public const string DistDiamond = "DIST_DIAMOND";
			public const string DistCircle = "DIST_CIRCLE";
		}

		public DistanceMapResult Result { get { return _resultType; } set { ChangeProperty(ref _resultType, value); } }
		public DistanceShape DistanceShape { get { return _distanceShape; } set { ChangeProperty(ref _distanceShape, value); } }
		public int MaxDistance { get { return _maxDistance; } set { ChangeProperty(ref _maxDistance, value); } }

		private DistanceShape _distanceShape = DistanceShape.Circle;
		private DistanceMapResult _resultType = DistanceMapResult.Outside;
		private int _maxDistance = 8192;
		private int _downSample = 1;
		private Material _material;
		private RenderTexture _rtAlphaUV;
		private RenderTexture[] _rtJumpFlood = new RenderTexture[2];
		private RenderTexture _rtDistance;
		private RenderTexture _sourceTexture;
		private RenderTexture _outputTexture;

		private bool _materialsDirty = true;
		private FilterBase _parentFilter = null;

		private DistanceMap() { }

		public DistanceMap(FilterBase parentFilter)
		{
			Debug.Assert(parentFilter != null);
			_parentFilter = parentFilter;

			_downSample = 1;
			if ((Filters.PerfHint & PerformanceHint.AllowDownsampling) != 0)
			{
				_downSample = 2;
			}
		}

		private void ChangeProperty<T>(ref T backing, T value) where T : struct
		{
			if (ObjectHelper.ChangeProperty(ref backing, value))
			{
				backing = value;
				_materialsDirty = true;
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

			_outputTexture = _sourceTexture;

			if (_sourceTexture != null)
			{
				if (_resultType == DistanceMapResult.Outside || _resultType == DistanceMapResult.Inside)
				{
					var a = GetTempJumpFloodTexture();
					var b = GetTempJumpFloodTexture();

					#if UNITY_EDITOR
					a.name = "JumpFloodA";
					b.name = "JumpFloodB";
					#endif

					ProcessPrime(_resultType == DistanceMapResult.Outside, a);
					var jfa = ProcessJumpFlood(a, a, b);
					ProcessResolveDistance(jfa);

					RenderTextureHelper.ReleaseTemporary(ref b);
					RenderTextureHelper.ReleaseTemporary(ref a);
				}
				else
				{
					// Outside
					var a = GetTempJumpFloodTexture();
					var b = GetTempJumpFloodTexture();
					#if UNITY_EDITOR
					a.name = "JumpFloodA";
					b.name = "JumpFloodB";
					#endif
					ProcessPrime(true, a);
					var jfaOut = ProcessJumpFlood(a, a, b);

					// Inside
					var c = GetTempJumpFloodTexture();
					#if UNITY_EDITOR
					c.name = "JumpFloodC";
					#endif
					var d = (jfaOut == a) ? b : a;
					ProcessPrime(false, c);
					var jfaIn = ProcessJumpFlood(c, c, d);

					ProcessResolveDistanceSDF(jfaOut, jfaIn);

					RenderTextureHelper.ReleaseTemporary(ref c);
					RenderTextureHelper.ReleaseTemporary(ref b);
					RenderTextureHelper.ReleaseTemporary(ref a);
				}

				_outputTexture = _rtDistance;
			}

			RenderTexture.active = prevRT;

			return _outputTexture;
		}

		private void ProcessPrime(bool isOutside, RenderTexture targetTexture)
		{
			// Convert alpha channel to UV map (also downsample if set)
			int pass = isOutside ? ShaderPass.AlphaToUV : ShaderPass.InvAlphaToUV;
			Graphics.Blit(_sourceTexture, targetTexture, _material, pass);
			targetTexture.IncrementUpdateCount();
		}

		private void ProcessResolveDistance(RenderTexture jfa)
		{
			// Resolve nearest-UV map to distance
			Graphics.Blit(jfa, _rtDistance, _material, ShaderPass.ResolveDistance);
			_rtDistance.IncrementUpdateCount();
		}

		private void ProcessResolveDistanceSDF(RenderTexture jfaOut, RenderTexture jfaIn)
		{
			// Resolve nearest-UV map to distance
			_material.SetTexture(ShaderProp.InsideTex, jfaIn);
			int pass = (_resultType == DistanceMapResult.SDF) ? ShaderPass.ResolveDistanceSDF : ShaderPass.ResolveDistanceInOutMax;
			Graphics.Blit(jfaOut, _rtDistance, _material, pass);
			_rtDistance.IncrementUpdateCount();
		}

		private RenderTexture ProcessJumpFlood(RenderTexture srcTexture, RenderTexture flipTexture, RenderTexture flopTexture)
		{
			RenderTexture result = srcTexture;

			int stepX = (Mathf.Min(_maxDistance, srcTexture.width)) / 2;
			int stepY = (Mathf.Min(_maxDistance, srcTexture.height)) / 2;

			float maxLength = Mathf.NextPowerOfTwo(Mathf.Min(_maxDistance, Mathf.Max(srcTexture.width, srcTexture.height)));
			int maxPassCount = Mathf.FloorToInt(Mathf.Log(maxLength, 2f));

			int pass = ShaderPass.JumpFlood;
			for (int i = 0; i <	maxPassCount; i++)
			{
				_material.SetVector(ShaderProp.StepSize, new Vector2(stepX, stepY));

				var dstTexture = (srcTexture == flipTexture) ? flopTexture : flipTexture;
				//Debug.Log("step " + i + "/" + maxPassCount + " steps: " + stepX + "," + stepY + " " + dstTexture.width + "x" + dstTexture.height);
				Graphics.Blit(srcTexture, dstTexture, _material, pass);
				dstTexture.IncrementUpdateCount();
				result = dstTexture;

				if (stepX == 1) { stepX = 0; pass = ShaderPass.JumpFloodSingleAxis; }
				if (stepY == 1) { stepY = 0; pass = ShaderPass.JumpFloodSingleAxis; }
				if (stepX > 1) stepX /= 2;
				if (stepY > 1) stepY /= 2;

				srcTexture = dstTexture;

				if (stepX == 0 && stepY == 0)
				{
					break;
				}
			}

			return result;
		}

		public RenderTexture GetOutputTexture()
		{
			return _outputTexture;
		}

		public void FreeResources()
		{
			FreeShaders();
			FreeTextures();
		}

		void SetupResources(RenderTexture sourceTexture)
		{
			// TODO: keep track of incrementCount so we can avoid rerunning filters is the source texture hasn't changed
			if (_sourceTexture != null)
			{
				bool sourceTextureSizeChanged = (sourceTexture == null) || ((_sourceTexture.width != sourceTexture.width) || (_sourceTexture.height != sourceTexture.height));
				if (sourceTextureSizeChanged)
				{
					FreeTextures();
					_materialsDirty = true;
				}
			}
			if (_sourceTexture == null && sourceTexture != null)
			{
				CreateTextures(sourceTexture);
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

		void UpdateMaterials()
		{
			_material.SetInt(ShaderProp.DownSample, _downSample);
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

		RenderTexture GetTempJumpFloodTexture()
		{
			int w = _sourceTexture.width / _downSample;
			int h = _sourceTexture.height / _downSample;

			var formatRG = RenderTextureFormat.RGHalf;
			if ((Filters.PerfHint & PerformanceHint.UseMorePrecision) != 0)
			{
				formatRG = RenderTextureFormat.RGFloat;
			}
			//TODO
			if (!SystemInfo.SupportsRenderTextureFormat(formatRG))
			{
				formatRG = RenderTextureFormat.R8;
			}
			
			RenderTextureReadWrite rw = RenderTextureReadWrite.Linear;

			var result = RenderTexture.GetTemporary(w, h, 0, formatRG, rw);
			// NOTE: Have to use point sampling because we sampling values that are UV coordinates, so we never want interpolation
			result.filterMode = FilterMode.Point;
			result.wrapMode = TextureWrapMode.Clamp;
			return result;
		}

		void CreateTextures(RenderTexture sourceTexture)
		{
			int w = sourceTexture.width / _downSample;
			int h = sourceTexture.height / _downSample;

			var formatR = RenderTextureFormat.RHalf;
			if ((Filters.PerfHint & PerformanceHint.UseMorePrecision) != 0)
			{
				formatR = RenderTextureFormat.RFloat;
			}
			//TODO
			if (!SystemInfo.SupportsRenderTextureFormat(formatR))
			{
				formatR = RenderTextureFormat.R8;
			}
			
			RenderTextureReadWrite rw = RenderTextureReadWrite.Linear;

			_rtDistance = RenderTexture.GetTemporary(w, h, 0, formatR, rw);
			//_rtDistance.filterMode = FilterMode.Point;

			#if UNITY_EDITOR
			_rtDistance.name = "DistanceMap";
			#endif
		}

		void FreeShaders()
		{
			ObjectHelper.Destroy(ref _material);
		}

		void FreeTextures()
		{
			RenderTextureHelper.ReleaseTemporary(ref _rtDistance);
			_sourceTexture = null;
		}
	}
}