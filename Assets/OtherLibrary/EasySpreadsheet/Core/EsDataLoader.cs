using System;
using System.IO;
using UnityEngine;

namespace EasySpreadsheet
{
	public interface IEsDataLoader
	{
		EsRowDataTable Load(string sheetClassName);
	}
	
	/// <summary>
	/// Load generated data from Resources.
	/// </summary>
	public class EsDataLoaderResources : IEsDataLoader
	{
		private string relativePath = string.Empty;
		
		public EsRowDataTable Load(string sheetClassName)
		{
			if (string.IsNullOrEmpty(relativePath))
			{
				string rawPath = EsSettings.Instance.generatedAssetPath;
				relativePath = rawPath.Substring(rawPath.IndexOf("Resources", StringComparison.Ordinal) + "Resources".Length);
			}
			
			var headName = sheetClassName;
			var filePath = Path.Combine(relativePath, headName);
			var table = Resources.Load(filePath) as EsRowDataTable;
			
#if UNITY_EDITOR
			if (table == null)
			{
				EsLog.Error("Can not load file " + filePath + ".asset");
			}
#endif
			
			return table;
		}
	}
	
	/// <summary>
	/// Load generated data from AssetBundle.
	/// </summary>
	public class EsDataLoaderAssetBundle : IEsDataLoader
	{
		public EsRowDataTable Load(string sheetClassName)
		{
			// Your AssetBundle file path
			var bundlePath = Application.persistentDataPath + "/***Your AssetBundle File Name***";
			// Your AssetBundle file path
			var assetPath = "Assets/***/" + sheetClassName + ".asset";
			var bundle = AssetBundle.LoadFromFile(bundlePath);
			var collection =  bundle.LoadAsset(assetPath) as EsRowDataTable;
			return collection;
		}
	}
}