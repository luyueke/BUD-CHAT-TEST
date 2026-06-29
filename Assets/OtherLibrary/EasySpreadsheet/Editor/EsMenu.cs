using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Es;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace EasySpreadsheet
{
	public static class EsMenu
	{
		private const string MenuPath = "EasySpreadsheet/";
		
		[MenuItem(MenuPath + "Generate Scripts")]
		public static void GenScripts()
		{
			if (CheckSpreadsheetFilesDir(out string dir))
			{
				var cnv = new EsConverter();
				cnv.GenerateScripts(dir, EsSettings.Instance.generatedScriptPath,EsSettings.Instance.generatedEditorScriptPath);
			}
			else
			{
				EsLog.Error("CheckSpreadsheetFilesDir");
			}
		}
		
		[MenuItem(MenuPath + "Generate Data")]
		public static void GenData()
		{
			if (EditorApplication.isCompiling)
				return;
			
			if (CheckSpreadsheetFilesDir(out string dir))
			{
				var cnv = new EsConverter();
				cnv.GenerateData(dir, EsSettings.Instance.generatedAssetPath);
			}
			else
			{
				EsLog.Error("CheckSpreadsheetFilesDir");
			}
			// GenerateCombineData(); //已废弃，后续走OP拉取配置表
			ForceInsertAvatarZeroData();//AvatarCommon表强制前面写入PgcID=0的数据，业务需要
		}

		private static string combineAsset = "AvatarCommonData";
		private static void GenerateCombineData()
		{
			Debug.LogError("GenerateCombineData");
			var generatePath = EsSettings.Instance.generatedAssetPath;
		
			List<AvatarCommonData> avatarDatas = new List<AvatarCommonData>();
			
			var excelFiles = Directory.GetFiles(generatePath,"*.asset");
			List<string> tempNames = new List<string>();
			bool isAddZero = false;
			for (var i = 0; i < excelFiles.Length; i++)
			{
				if (excelFiles[i].Contains("AvatarData"))
				{
					var assetData = AssetDatabase.LoadAssetAtPath<EsRowDataTable>(excelFiles[i]);
					JsonSerializerSettings settings = new JsonSerializerSettings();
					settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
					
					string avatarJson = JsonConvert.SerializeObject(assetData, settings);
					JObject dataListJObject = JsonConvert.DeserializeObject<JObject>(avatarJson);
					var dataList = dataListJObject["DataList"].ToArray();
					for (var j = 0; j< dataList.Length; j++)
					{
						var dataJObject = dataList[j] as JObject;
						var temp = dataJObject.Properties().ToList();
						temp.ForEach(x =>
						{
							if(!tempNames.Contains(x.Name))
							{
								tempNames.Add(x.Name);
							}
						});
						AvatarCommonData cData = new AvatarCommonData();
						var fields = cData.GetType().GetProperties();
						for (var k = 0; k < fields.Length; k++)
						{
							var valField = fields[k];
							if (dataJObject.ContainsKey(valField.Name))
							{
								var temp1 = cData.GetType().GetField("_" + valField.Name, BindingFlags.NonPublic | BindingFlags.Instance);
								var type = valField.PropertyType;
								if (type == typeof(string))
								{
									var tempToken = dataJObject[fields[k].Name];
									var val = tempToken.ToObject<string>();
									temp1.SetValue(cData,val);
								}
								if (type == typeof(int))
								{
									var tempToken = dataJObject[fields[k].Name];
									var val = tempToken.ToObject<int>();
									temp1.SetValue(cData,val);
								}
								
								if (type == typeof(bool))
								{
									var tempToken = dataJObject[fields[k].Name];
									var val = tempToken.ToObject<bool>();
									temp1.SetValue(cData,val);
								}

								if (type == typeof(List<string>))
								{
									var tempToken = dataJObject[fields[k].Name];
									var val = tempToken.ToObject<List<string>>();
									temp1.SetValue(cData,val);
								}

								if (type == typeof(Vector3))
								{
									var tempToken = dataJObject[fields[k].Name];
									var val = tempToken.ToObject<Vector3>();
									temp1.SetValue(cData,val);
								}
								
								if (type == typeof(List<Vector3>))
								{
									var tempToken = dataJObject[fields[k].Name];
									var val = tempToken.ToObject<List<Vector3>>();
									temp1.SetValue(cData,val);
								}
							}
						}

						if (cData.PgcId.Equals("0"))
						{
							if (!isAddZero)
							{
								isAddZero = true;
								avatarDatas.Add(cData);
							}
						}
						else
						{
							avatarDatas.Add(cData);
						}
					}
				}
			}

			AvatarCommonData tempCommonData = new AvatarCommonData();
			var tempFields = tempCommonData.GetType().GetProperties().Select(x=>x.Name);
			tempNames.ForEach(x =>
			{
				if (!tempFields.Contains(x) && !x.Equals("Name"))
				{
					Debug.LogError($"AvatarCommonData.xlsx No {x} field exists");
				}
			});
			string assetFilePath = generatePath +"/"+ combineAsset + ".asset";
			var asseObj = AssetDatabase.LoadAssetAtPath<Es.AvatarCommonDataTable>(assetFilePath);
			var commonAssetData =  ScriptableObject.CreateInstance<AvatarCommonDataTable>();
			commonAssetData.DataList.AddRange(avatarDatas);
			commonAssetData.SpreadsheetFileName = asseObj.SpreadsheetFileName;
			commonAssetData.SpreadsheetSheetName = asseObj.SpreadsheetSheetName;
			commonAssetData.KeyFieldName = asseObj.KeyFieldName;
			if (File.Exists(assetFilePath))
			{
				AssetDatabase.DeleteAsset(assetFilePath);
			}
			AssetDatabase.CreateAsset(commonAssetData, assetFilePath);
			AssetDatabase.Refresh();
		}

		public static void ForceInsertAvatarZeroData()
		{
			var generatePath = EsSettings.Instance.generatedAssetPath;
			string assetFilePath = generatePath +"/"+ combineAsset + ".asset";
			var asseObj = AssetDatabase.LoadAssetAtPath<Es.AvatarCommonDataTable>(assetFilePath);
			var commonAssetData =  ScriptableObject.CreateInstance<AvatarCommonDataTable>();
			if (asseObj != null && asseObj.DataList[0] != null && asseObj.DataList[0].PgcId == "0")
			{
				return;
			}
			
			//写入0
			var zeroData = new AvatarCommonData();
			var fields = zeroData.GetType().GetProperties();
			var pgcIdField = zeroData.GetType().GetField("_PgcId", BindingFlags.NonPublic | BindingFlags.Instance);
			var subTypeField = zeroData.GetType().GetField("_SubType", BindingFlags.NonPublic | BindingFlags.Instance);
			pgcIdField.SetValue(zeroData, "0");
			subTypeField.SetValue(zeroData, 0);
			asseObj.DataList.Insert(0, zeroData);
			
			commonAssetData.DataList.AddRange(asseObj.DataList);
			commonAssetData.SpreadsheetFileName = asseObj.SpreadsheetFileName;
			commonAssetData.SpreadsheetSheetName = asseObj.SpreadsheetSheetName;
			commonAssetData.KeyFieldName = asseObj.KeyFieldName;
			if (File.Exists(assetFilePath))
			{
				AssetDatabase.DeleteAsset(assetFilePath);
			}
			AssetDatabase.CreateAsset(commonAssetData, assetFilePath);
			AssetDatabase.Refresh();
			Debug.Log("AvatarCommonData写入0数据成功");
		}
		
		
		[MenuItem(MenuPath + "Clear Cache")]
		public static void Clear()
		{
			EsCaches.Clear();
			DeleteGeneratedAssets();
			AssetDatabase.Refresh();
		}

		private static bool CheckSpreadsheetFilesDir(out string dir)
		{
			string excelPath = Path.GetFullPath(EsSettings.Instance.excelFilesPath);
			if (Directory.Exists(excelPath))
			{
				dir = excelPath;
				return true;
			}
			
			dir = string.Empty;

			return false;
		}
		
		/*[MenuItem(@"Tools/EasySpreadsheet/Import Xlsx Files")]
		public static void ImportFolder()
		{
			var historySpreadsheetPath = EditorPrefs.GetString(EsConverter.excelPathKey);
			if (string.IsNullOrEmpty(historySpreadsheetPath) || !Directory.Exists(historySpreadsheetPath))
			{
				var fallbackDir = Environment.CurrentDirectory + "/Assets/EasySpreadsheet/Example/SpreadsheetFiles";
				historySpreadsheetPath = Directory.Exists(fallbackDir) ? fallbackDir : Environment.CurrentDirectory;
			}

			var excelPath = EditorUtility.OpenFolderPanel("Select the folder of excel files", historySpreadsheetPath, "");
			if (string.IsNullOrEmpty(excelPath))
				return;

			EditorPrefs.SetString(EsConverter.excelPathKey, excelPath);
			EsConverter.GenerateScripts(excelPath, Environment.CurrentDirectory + "/" + EsSettings.Instance.generatedScriptPath);
		}

		/*[DidReloadScripts]
		private static void OnScriptsReloaded()
		{
			if (!EditorPrefs.GetBool(EsConverter.csChangedKey, false)) return;
			EditorPrefs.SetBool(EsConverter.csChangedKey, false);
			var historySpreadsheetPath = EditorPrefs.GetString(EsConverter.excelPathKey);
			if (string.IsNullOrEmpty(historySpreadsheetPath)) return;
			EsLog.Log("Scripts are reloaded, start generating assets...");
			EsConverter.GenerateScriptableObjects(historySpreadsheetPath, Environment.CurrentDirectory + "/" + EsSettings.Instance.generatedAssetPath);
		}*/

		private static void DeleteGeneratedAssets()
		{
			string assetPath = EsSettings.Instance.generatedAssetPath;
			if (Directory.Exists(assetPath))
				Directory.Delete(assetPath, true);

			string asMeta = Path.GetFullPath(assetPath) + ".meta";
			if (File.Exists(asMeta))
				File.Delete(asMeta);
		}

		[MenuItem(MenuPath + "Settings")]
		public static void OpenSettingsWindow() => SettingsService.OpenProjectSettings("Project/Easy Spreadsheet");

		[MenuItem(MenuPath + "About")]
		private static void OpenAboutWindow()
		{
			var window = EditorWindow.GetWindowWithRect<EsAboutWindow>(new Rect(0, 0, 480, 320), true, "About EasySpreadsheet", true);
			window.Show();
		}
	}
}