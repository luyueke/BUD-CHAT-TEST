//--------------------------------------------------------------------------//
// Copyright 2023-2024 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;

namespace ChocDino.UIFX
{
	class TextureUtils
	{
		internal static bool WriteToPNG(RenderTexture sourceTexture, Texture2D rwTexture, string outputPath)
		{
			// Assumptions
			Debug.Assert(sourceTexture != null);
			Debug.Assert(rwTexture != null);
			Debug.Assert(rwTexture.width == sourceTexture.width && rwTexture.height == sourceTexture.height);

			// Read pixels from GPU to CPU
			RenderTexture prevTexture = RenderTexture.active;
			RenderTexture.active = sourceTexture;
			rwTexture.ReadPixels(new Rect(0, 0, sourceTexture.width, sourceTexture.height), 0, 0, recalculateMipMaps:false);
			rwTexture.Apply(updateMipmaps:false, makeNoLongerReadable:false);
			RenderTexture.active = prevTexture;

			// Write PNG
			byte[] data = ImageConversion.EncodeToPNG(rwTexture);
			System.IO.File.WriteAllBytes(outputPath, data);

			return true;
		}
	}
}