using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using dnlib;
using UnityEditor;
using UnityEngine;

namespace EasySpreadsheet
{
	/// <summary>
	///     Spreadsheet Converter
	/// </summary>
	public partial class EsConverter
	{
		public void GenerateData(string excelFilesDir, string output)
		{
			try
			{
				excelFilesDir = Path.GetFullPath(excelFilesDir);
				output = Path.GetFullPath(output);
				
				EsLog.Info("Generate data for files in " + excelFilesDir);

				if (!Directory.Exists(excelFilesDir))
				{
					EditorUtility.DisplayDialog("EasySpreadsheet", $"Path does not exist: \n{excelFilesDir}", "OK");
					return;
				}

				if (!Directory.Exists(output))
				{
					Directory.CreateDirectory(output);
					EsCaches.Clear();
				}

				EsCaches.Reload();
				
				PrasePredefinedTypes(excelFilesDir);

				var excelFiles = Directory.GetFiles(excelFilesDir);
				var count = 0;
				bool hasError = false;
				bool generateUIEnum = false;
				for (var i = 0; i < excelFiles.Length; ++i)
				{
					var excelPath = excelFiles[i];
					if (!IsSpreadsheetFile(excelPath)) continue;

					if (!EsCaches.IsDataChanged(excelPath, out var excelMd5))
					{
						//EsLog.Log($"没有变化asset跳过文件{filePath}");
						continue;
					}

					UpdateProgressBar(i, excelFiles.Length, Path.GetFileName(excelPath) + " to asset");

					if (ToScriptableObject(excelPath, output))
						EsCaches.UpdateData(excelPath, excelMd5);
					else
						hasError = true;
					count++;

					//BUD TOOL
					if (!generateUIEnum
					&& excelPath.Contains("UIPanel") || excelPath.Contains("UIWindow"))
					{
						generateUIEnum = true;
					}
				}

				if (count > 0 && !hasError)
					EsLog.Info("Generate data successfully.");
				if (count == 0)
					EsLog.Info("No excel files changed.");
				
				EsCaches.Save();

				//BUD TOOL
				if (generateUIEnum)
				{
					AssetDatabase.ImportAsset("Assets/Arts/Config/Asset/UIPanel.asset");
					EsLog.Info("重新导入UI数据.");
				}
			}
			catch (Exception e)
			{
				EsLog.Error(e.ToString());
			}
			finally
			{
				ClearProgressBar();
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
			}
		}
		
		private bool ToScriptableObject(string excelPath, string outputPath)
		{
			try
			{
				var book = EsWorkbook.Load(excelPath);
				if (book == null)
					return false;
				int sheetCount = book.sheetCount;
				bool hasError = false;
				for (int i = 0; i < sheetCount; ++i)
				{
					var sheet = book.GetWorkSheet(i);
					if (sheet == null)
						continue;
					sheet.LoadAllCells();
					
					if (!IsValidDataTableSheet(sheet))
						continue;

					var sheetData = CreateDataTableSheetContext(sheet);
					if (!ToScriptableObject(excelPath, sheet.name, outputPath, sheetData))
						hasError = true;
				}

				return !hasError;
			}
			catch (Exception e)
			{
				EsLog.Error(e.ToString());
			}

			return false;
		}

		private bool ToScriptableObject(string excelPath, string sheetName, string outputPath, DataTableSheetContext sheetContext)
		{
			try
			{
				string fileName = Path.GetFileName(excelPath);
				EsLog.Info($"Gen data for [{sheetName}] in [{fileName}]...");
				
				
				var sheetClassName = EsSettings.Instance.GetSheetClassName(fileName, sheetName);
				string uniSheetName = UniTypeName(sheetName);
				string uniTableClass = uniSheetName + EsSettings.Instance.sheetClassPostfix;
				var dataTable = ScriptableObject.CreateInstance(uniTableClass) as EsRowDataTable;
				if (dataTable == null)
				{
					EsLog.Info($"ScriptableObject.CreateInstance({sheetClassName}): Error for [{sheetName}] in [{fileName}]");
					return false;
				}
				dataTable.SpreadsheetFileName = fileName;
				dataTable.SpreadsheetSheetName = sheetName;

				var dataTableType = dataTable.GetType();
				var dataListField = dataTableType.GetField("_dataList", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
				var dataList = dataListField.GetValue(dataTable);
				var dataListAdd = dataListField.FieldType.GetMethod("Add", BindingFlags.Instance | BindingFlags.Public);

				var rowDataFullTypeName = EsSettings.Instance.GetRowDataClassName(fileName, uniSheetName, true);
				var rowDataType = Type.GetType(rowDataFullTypeName);
				if (rowDataType == null)
				{
					var assemblies = AppDomain.CurrentDomain.GetAssemblies();
					foreach (var assembly in assemblies)
					{
						rowDataType = assembly.GetType(rowDataFullTypeName);
						if (rowDataType != null)
							break;
					}
				}
				if (rowDataType == null)
				{
					EsLog.Error(rowDataFullTypeName + " not exist !");
					return false;
				}

				var parseMethod = rowDataType.GetMethod("Parse", BindingFlags.Instance | BindingFlags.NonPublic);
				if (parseMethod == null)
				{
					EsLog.Error(rowDataFullTypeName + " parse method not exist !");
					return false;
				}

				var keySet = new HashSet<object>();
				for (var row = sheetContext.DataStartRow; row < sheetContext.RowCount; ++row)
				{
					string firstCell = sheetContext.Get(row, 0).Trim();
					if (string.IsNullOrEmpty(firstCell))// skip empty rows
						continue;
					
					for (var col = 0; col < sheetContext.ColumnCount; ++col)
						sheetContext.Set(row, col, sheetContext.Get(row, col).Replace("\\n", "\n"));

					var rowData = Activator.CreateInstance(rowDataType) as EsRowData;
					if (rowData == null)
						continue;
					
					parseMethod.Invoke(rowData, new object[]{sheetContext.Table[row], 0});
					
					var key = rowData.GetKeyFieldValue();
					if (key == null)
					{
						EsLog.Error("The value of key is null in sheet " + sheetName);
						continue;
					}
					
					if (key is int i && i == 0)
						continue;
					
					if (key is string s && string.IsNullOrEmpty(s))
						continue;
					
					if (!keySet.Contains(key))
					{
						dataListAdd.Invoke(dataList, new object[]{rowData});
						keySet.Add(key);
					}
					else
						EsLog.Error($"Duplicated Key [{key}] at row [{row + 1}] in sheet [{sheetName}], file [{fileName}]! Please fix it.");
				}

				var keyField = EsUtility.GetRowDataKeyField(rowDataType);
				if (keyField != null)
					dataTable.KeyFieldName = keyField.Name;
				
				var assetFilePath = Path.Combine(outputPath, EsSettings.Instance.GetAssetFileName(fileName, sheetName));
				assetFilePath = assetFilePath.Substring(assetFilePath.IndexOf("Assets", StringComparison.Ordinal));
				if (File.Exists(assetFilePath))
				{
					//FileUtil.DeleteFileOrDirectory(assetFilePath);
					AssetDatabase.DeleteAsset(assetFilePath);
					//AssetDatabase.Refresh();
				}
				AssetDatabase.CreateAsset(dataTable, assetFilePath);
				return true;
			}
			catch (Exception ex)
			{
				EsLog.Error(ex.ToString());
			}

			return false;
		}

		public object CreateList(Type type)
		{
			Type listType = typeof(List<>);
			//指定泛型的具体类型
			Type newType = listType.MakeGenericType(new Type[] { type });
			//创建一个list返回
			return Activator.CreateInstance(newType, new object[] { });
		}
		
		
		

	}
}