using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;

namespace EasySpreadsheet
{
	/// <summary>
	///     Spreadsheet Converter
	/// </summary>
	public partial class EsConverter
	{
		public void GenerateScripts(string excelFilesDir, string output,string editOutput)
		{
			try
			{
				excelFilesDir = Path.GetFullPath(excelFilesDir);
				output = Path.GetFullPath(output);

				EsLog.Info("Generate scripts for files in " + excelFilesDir);

				if (!Directory.Exists(excelFilesDir))
				{
					EditorUtility.DisplayDialog("EasySpreadsheet", $"Path does not exist: \n{excelFilesDir}", "OK");
					return;
				}

				if (Directory.Exists(output))
					Directory.Delete(output, true);
				Directory.CreateDirectory(output);


				if (Directory.Exists(editOutput))
					Directory.Delete(editOutput, true);
				Directory.CreateDirectory(editOutput);

				var tmpPath = Environment.CurrentDirectory + "/EasySpreadsheetTmp/";
				if (Directory.Exists(tmpPath))
					Directory.Delete(tmpPath, true);
				Directory.CreateDirectory(tmpPath);

				EsCaches.Reload();
				sheetContexts.Clear();

				var watch = new Stopwatch();
				int changedSpreadsheetCount = 0;

				ToCSharpPredefinedClasses(excelFilesDir, tmpPath);

				var excelFiles = Directory.GetFiles(excelFilesDir);
				for (var i = 0; i < excelFiles.Length; ++i)
				{
					watch.Restart();

					var excelFilePath = excelFiles[i];

					if (!IsSpreadsheetFile(excelFilePath))
						continue;

					if (i + 1 < excelFiles.Length)
						UpdateProgressBar(i + 1, excelFiles.Length, Path.GetFileName(excelFilePath) + " to scripts");
					else
						ClearProgressBar();

					changedSpreadsheetCount++;

					string fileName = Path.GetFileName(excelFilePath);

					var sheetContexts = ToCSharpClasses(excelFilePath);
					foreach (var sheetContext in sheetContexts.Values)
					{
						var rowFile = EsSettings.Instance.GetRowClassFileName(fileName, sheetContext.sheetName);
						bool shouldWrite = true;
						if (File.Exists(Path.Combine(output, rowFile)))
							shouldWrite = File.ReadAllText(Path.Combine(output, rowFile)) != sheetContext.rowTxt;
						if (shouldWrite)
							File.WriteAllText(Path.Combine(output, rowFile), sheetContext.rowTxt, Encoding.UTF8);

						var tableFile = EsSettings.Instance.GetTableClassFileName(fileName, sheetContext.sheetName);
						shouldWrite = true;
						if (File.Exists(Path.Combine(output, tableFile)))
							shouldWrite = File.ReadAllText(Path.Combine(output, tableFile)) != sheetContext.tableTxt;
						if (shouldWrite)
							File.WriteAllText(Path.Combine(output, tableFile), sheetContext.tableTxt, Encoding.UTF8);


						var tableEditorFile = EsSettings.Instance.GetTableEditorClassFileName(fileName, sheetContext.sheetName);
						shouldWrite = true;
						if (File.Exists(Path.Combine(editOutput, tableEditorFile)))
							shouldWrite = File.ReadAllText(Path.Combine(editOutput, tableEditorFile)) != sheetContext.editorTxt;
						if (shouldWrite)
							File.WriteAllText(Path.Combine(editOutput, tableEditorFile), sheetContext.editorTxt, Encoding.UTF8);
					}

					//EsCaches.UpdateScript(excelFilePath, excelMd5);

					EsLog.Info($"Gen scripts for [{fileName}]. {watch.ElapsedMilliseconds}ms");
				}

				File.WriteAllText(Path.Combine(tmpPath, EsConstant.DataTablesName + ".cs"), ToDataTables(), Encoding.UTF8);

				EsStringBuilderCache.Reset();
				EsCaches.Save();

				var files = Directory.GetFiles(tmpPath);
				foreach (var s in files)
				{
					File.Copy(s, Path.Combine(output, Path.GetFileName(s)), true);
				}

				if (changedSpreadsheetCount == 0)
					EsLog.Info("No excel files changed.");
				else
					EsLog.Info("Generate scripts successfully.");

				if (Directory.Exists(tmpPath))
					Directory.Delete(tmpPath, true);
			}
			catch (Exception e)
			{
				EsLog.Error(e.ToString());
				//EditorPrefs.SetBool(csChangedKey, false);
				EsCaches.Clear();
			}

			finally
			{
				ClearProgressBar();
				AssetDatabase.Refresh();
			}
		}

		private Dictionary<string, DataTableSheetContext> ToCSharpClasses(string excelPath)
		{
			var book = EsWorkbook.Load(excelPath);
			if (book == null)
				return sheetContexts;

			string fileName = Path.GetFileName(excelPath);
			int sheetCount = book.sheetCount;
			for (int i = 0; i < sheetCount; ++i)
			{
				var sheet = book.GetWorkSheet(i);
				if (sheet == null)
					continue;
				sheet.LoadCells(EsConstant.HeaderRowRange);

				if (!IsValidDataTableSheet(sheet))
				{
					//EsLog.Log(string.Format("Skipped sheet {0} in file {1}.", sheet.name, fileName));
					continue;
				}

				var sheetContext = CreateDataTableSheetContext(sheet);

				ToCSharpRowClass(sheetContext, fileName);
				ToCSharpTableClass(sheetContext, fileName);
				ToEditorCSharpTableClass(sheetContext, fileName);
				sheetContexts.Add(sheetContext.tableClassName, sheetContext);
			}

			return sheetContexts;
		}

		private void ToCSharpTableClass(DataTableSheetContext sheetContext, string fileName)
		{
			sheetContext.tableClassName = string.Empty;
			try
			{
				sheetContext.ParseFields();

				if (sheetContext.keyField == null)
					EsLog.Error("Cannot find Key column in sheet " + sheetContext.sheetName);

				var rowClass = EsSettings.Instance.GetRowDataClassName(fileName, sheetContext.sheetName);
				sheetContext.tableClassName = EsSettings.Instance.GetSheetClassName(fileName, sheetContext.sheetName);

				var csFile = new StringBuilder(128*100);
				csFile.Append("//------------------------------------------------------------------------------\n");
				csFile.Append("// <auto-generated>\n");
				csFile.Append("//     This code was generated by EasySpreadsheet.\n");
				csFile.Append("//     Runtime Version: " + EsConstant.Version + "\n");
				csFile.Append("//\n");
				csFile.Append("//     Changes to this file may cause incorrect behavior and will be lost if\n");
				csFile.Append("//     the code is regenerated.\n");
				csFile.Append("// </auto-generated>\n");
				csFile.Append("//------------------------------------------------------------------------------");
				csFile.Append("\nusing System;\nusing System.Collections.Generic;\nusing UnityEngine;\nusing EasySpreadsheet;\n\n");
				csFile.Append(string.Format("namespace {0}\n", EsSettings.Instance.GetNameSpace(fileName)));
				csFile.Append("{\n");


				// EsRowDataTable class
				csFile.Append("\tpublic sealed partial class " + sheetContext.tableClassName + " : EsRowDataTable\n");
				csFile.Append("\t{");
				csFile.AppendFormat("\n\t\t[SerializeField]\n\t\tprivate List<{0}> _dataList = new List<{0}>();\n", rowClass);
				csFile.Append($"\n\t\tprivate Dictionary<{sheetContext.keyField.KeyFieldType}, {rowClass}> _dataMap;");

				csFile.Append($"\n\t\tpublic Dictionary<{sheetContext.keyField.KeyFieldType}, {rowClass}> DataMap => _dataMap;");
				csFile.Append($"\n\t\tpublic List<{rowClass}> DataList => _dataList;");
				csFile.Append("\n");
				csFile.Append($"\n\t\tpublic {rowClass} Get({sheetContext.keyField.KeyFieldType} key) => _dataMap.TryGetValue(key, out var v) ? v : null;");
				csFile.Append($"\n\t\tpublic {rowClass} this[{sheetContext.keyField.KeyFieldType} key] => _dataMap.TryGetValue(key, out var v) ? v : null;");

				csFile.Append("\n\n\t\tprotected override void OnDeserialized()\n\t\t{");
				csFile.Append($"\n\t\t\t_dataMap = new Dictionary<{sheetContext.keyField.KeyFieldType}, {rowClass}>(_dataList.Count);");
				csFile.Append("\n\t\t\tforeach (var data in _dataList)");
				csFile.Append("\n\t\t\t{");
				csFile.Append($"\n\t\t\t\t_dataMap.Add(data.{sheetContext.keyField.KeyFieldName}, data);");
				csFile.Append("\n\t\t\t\t(data as ISerializationCallbackReceiver).OnAfterDeserialize();");
				csFile.Append("\n\t\t\t}");
				csFile.Append("\n\t\t\tPostInit();");
				csFile.Append("\n\t\t}");

				csFile.Append("\n\n\t\tpartial void PostInit();\n");

				csFile.Append("\t}\n");

				csFile.Append("}\n");

				// Inspector
				// var inspectorClassName = EsSettings.Instance.GetSheetInspectorClassName(fileName, sheetContext.sheetName);
				// csFile.Append("\n#if UNITY_EDITOR");
				// csFile.Append(string.Format("\nnamespace {0}", EsSettings.Instance.GetNameSpace(fileName)));
				// csFile.Append("\n{");
				// csFile.Append(string.Format("\n\t[UnityEditor.CustomEditor(typeof({0}))]",
				// 	EsSettings.Instance.GetSheetClassName(fileName, sheetContext.sheetName) /*sheetName, EsSettings.Current.SheetDataPostfix*/));
				// csFile.Append("\n\tpublic class " + inspectorClassName + " : EsAssetInspector");
				// csFile.Append("\n\t{");
				// csFile.Append("\n\t}");
				// csFile.Append("\n}");
				// csFile.Append("\n#endif");

				sheetContext.tableTxt = csFile.ToString();
			}
			catch (Exception ex)
			{
				EsLog.Error(ex.ToString());
				sheetContext.tableTxt = "";
			}
		}

		private void ToEditorCSharpTableClass(DataTableSheetContext sheetContext, string fileName)
		{
			try
			{
				// Inspector
				var csFile = new StringBuilder(128 * 100);
				var inspectorClassName =
					EsSettings.Instance.GetSheetInspectorClassName(fileName, sheetContext.sheetName);
				csFile.Append("\n#if UNITY_EDITOR");
				csFile.Append("\nusing System;\nusing System.Collections.Generic;\nusing UnityEngine;\nusing EasySpreadsheet;\n\n");
				csFile.Append(string.Format("\nnamespace {0}", EsSettings.Instance.GetNameSpace(fileName)));
				csFile.Append("\n{");
				csFile.Append(string.Format("\n\t[UnityEditor.CustomEditor(typeof({0}))]",
					EsSettings.Instance.GetSheetClassName(fileName,
						sheetContext.sheetName) /*sheetName, EsSettings.Current.SheetDataPostfix*/));
				csFile.Append("\n\tpublic class " + inspectorClassName + " : EsAssetInspector");
				csFile.Append("\n\t{");
				csFile.Append("\n\t}");
				csFile.Append("\n}");
				csFile.Append("\n#endif");
				sheetContext.editorTxt = csFile.ToString();
			}
			catch (Exception ex)
			{
				EsLog.Error(ex.ToString());
				sheetContext.editorTxt = "";
			}
		}

		private void ToCSharpRowClass(DataTableSheetContext sheetContext, string fileName)
		{
			try
			{
				sheetContext.ParseFields();

				if (sheetContext.keyField == null)
					EsLog.Error("Cannot find Key column in sheet " + sheetContext.sheetName);

				bool visualScripting = EsSettings.Instance.visualScripting;
				var rowClass = EsSettings.Instance.GetRowDataClassName(fileName, sheetContext.sheetName);

				sheetContext.rowClassName = rowClass;

				var csFile = new StringBuilder(256*100);
				csFile.Append("//------------------------------------------------------------------------------\n");
				csFile.Append("// <auto-generated>\n");
				csFile.Append("//     This code was generated by EasySpreadsheet.\n");
				csFile.Append("//     Runtime Version: " + EsConstant.Version + "\n");
				csFile.Append("//\n");
				csFile.Append("//     Changes to this file may cause incorrect behavior and will be lost if\n");
				csFile.Append("//     the code is regenerated.\n");
				csFile.Append("// </auto-generated>\n");
				csFile.Append("//------------------------------------------------------------------------------");
				csFile.Append("\nusing System;");
				csFile.Append("\nusing System.Collections.Generic;");
				if (visualScripting)
					csFile.Append("\nusing Unity.VisualScripting;");
				csFile.Append("\nusing UnityEngine;");
				csFile.Append("\nusing EasySpreadsheet;");
				csFile.Append(string.Format("\n\nnamespace {0}\n", EsSettings.Instance.GetNameSpace(fileName)));
				csFile.Append("{\n");
				csFile.Append("\t[Serializable]");
				if (visualScripting)
					csFile.Append("\n\t[Inspectable]");
				csFile.Append("\n\tpublic sealed partial class " + rowClass + " : EsRowData\n");
				csFile.Append("\t{\n");

				for (var col = 0; col < sheetContext.fields.Length; col++)
				{
					var columnField = sheetContext.fields[col];
					if (columnField == null)
						continue;
					csFile.Append(columnField.GetDeclarationLines());
				}

				csFile.AppendFormat("\n\t\tpublic {0}()\n", rowClass);
				csFile.Append("\t\t{\n");
				csFile.Append("\t\t}\n");
				csFile.Append("\n#if UNITY_EDITOR\n");
				csFile.Append("\t\tprivate void Parse(List<string> cells, int column)\n");
				csFile.Append("\t\t{\n");

				for (var col = 0; col < sheetContext.fields.Length; col++)
				{
					var columnField = sheetContext.fields[col];
					if (columnField == null)
						continue;
					csFile.Append(columnField.GetParseLines());
				}

				csFile.Append("\t\t}\n#endif\n");

				csFile.Append("\t\tprotected override void OnDeserialized()\n");
				csFile.Append("\t\t{\n");
				for (var col = 0; col < sheetContext.fields.Length; col++)
				{
					var columnField = sheetContext.fields[col];
					if (columnField == null)
						continue;
					csFile.Append(columnField.GetAfterSerializedLines());
				}

				csFile.Append("\n\t\t\tPostInit();\n");
				csFile.Append("\t\t}\n");

				csFile.Append("\n\t\tpartial void PostInit();");

				csFile.Append("\n\t}");

				csFile.Append("\n\n}");

				sheetContext.rowTxt = csFile.ToString();
			}
			catch (Exception ex)
			{
				EsLog.Error(ex.ToString());
				sheetContext.rowTxt = "";
			}
		}

		private string ToDataTables()
		{
			try
			{
				const string className = EsConstant.DataTablesName;

				List<string> tableClassNames = sheetContexts.Keys.ToList();
				tableClassNames.Sort();

				var settings = EsSettings.Instance;
				var csFile = new StringBuilder(10240);
				csFile.Append("//------------------------------------------------------------------------------\n");
				csFile.Append("// <auto-generated>\n");
				csFile.Append("//     This code was generated by EasySpreadsheet.\n");
				csFile.Append("//     Runtime Version: " + EsConstant.Version + "\n");
				csFile.Append("//\n");
				csFile.Append("//     Changes to this file may cause incorrect behavior and will be lost if\n");
				csFile.Append("//     the code is regenerated.\n");
				csFile.Append("// </auto-generated>\n");
				csFile.Append("//------------------------------------------------------------------------------");

				csFile.Append("\nusing System;");
				csFile.Append("\nusing System.Collections.Generic;");
				csFile.Append("\nusing System.Collections;");
				csFile.Append("\nusing EasySpreadsheet;\n\n");
				csFile.Append($"namespace {settings.nameSpace}\n");
				csFile.Append("{\n");
				csFile.Append($"\tpublic partial class {className}\n");
				csFile.Append("\t{");

				csFile.Append($"\n\t\tpublic static {className} Instance => s_instance;\n");

				csFile.Append($"\n\t\tprivate Dictionary<string, {nameof(EsRowDataTable)}> _cfgs;");
				csFile.Append($"\n\t\tpublic Dictionary<string, {nameof(EsRowDataTable)}> Cfgs => _cfgs;");

				foreach (var tableClass in tableClassNames)
				{
					string fileName = settings.ToAssetFileName(tableClass);
					string uniFileName = UniTypeName(fileName);
					if (fileName.Equals(uniFileName))
					{
						csFile.Append($"\n\t\tpublic {tableClass} {tableClass} {{get; private set;}}");
					}
					else
					{
						string uniTableClass = uniFileName + settings.sheetClassPostfix;
						csFile.Append($"\n\t\tpublic {uniTableClass} {tableClass} {{get; private set;}}");
					}
				}

				csFile.Append($"\n\n\t\tprivate static {className} s_instance;");

				csFile.Append($"\n\n\t\tpublic {className}()\n\t\t{{");
				csFile.Append("\n\t\t\ts_instance = this;");
				csFile.Append("\n\t\t}");

				csFile.Append("\n\n\t\tpublic void Load(IEsDataLoader loader)");
				csFile.Append("\n\t\t{");
				csFile.Append($"\n\t\t\t_cfgs = new Dictionary<string, {nameof(EsRowDataTable)}>();");
				foreach (var tableClass in tableClassNames)
				{
					string fileName = settings.ToAssetFileName(tableClass);
					string uniFileName = UniTypeName(fileName);
					if (fileName.Equals(uniFileName))
					{
						csFile.Append($"\n\t\t\t{tableClass} = loader.Load(\"{fileName}\") as {tableClass};");
						csFile.Append($"\n\t\t\t_cfgs.Add(\"{tableClass}\", {tableClass});");
					}
					else
					{
						string uniTableClass = uniFileName + settings.sheetClassPostfix;
						csFile.Append($"\n\t\t\t{tableClass} = loader.Load(\"{fileName}\") as {uniTableClass};");
						csFile.Append($"\n\t\t\t_cfgs.Add(\"{tableClass}\", {tableClass});");
					}

				}

				csFile.Append("\n\t\t\tPostInit();");
				csFile.Append("\n\t\t}");

				csFile.Append("\n");
				csFile.Append("\n\t\tpublic IEnumerator LoadAsync(IEsDataLoader loader)");
				csFile.Append("\n\t\t{");
				csFile.Append($"\n\t\t\t_cfgs = new Dictionary<string, {nameof(EsRowDataTable)}>();");
				int count = 0;
				foreach (var tableClass in tableClassNames)
				{
					if (count == 0)
					{
                        //csFile.Append("\n\t\t\tUnityEngine.Profiling.Profiler.BeginSample(\"LoadTable\");");
                    }
					string fileName = settings.ToAssetFileName(tableClass);
					string uniFileName = UniTypeName(fileName);
					if (fileName.Equals(uniFileName))
					{
						csFile.Append($"\n\t\t\t{tableClass} = loader.Load(\"{fileName}\") as {tableClass};");
						csFile.Append($"\n\t\t\t_cfgs.Add(\"{tableClass}\", {tableClass});");
						//csFile.Append("\n\t\t\tyield return null;");
					}
					else
					{
						string uniTableClass = uniFileName + settings.sheetClassPostfix;
						csFile.Append($"\n\t\t\t{tableClass} = loader.Load(\"{fileName}\") as {uniTableClass};");
						csFile.Append($"\n\t\t\t_cfgs.Add(\"{tableClass}\", {tableClass});");
						//csFile.Append("\n\t\t\tyield return null;");
					}
					count++;
					if (count >= 5) 
					{
                        //csFile.Append("\n\t\t\tUnityEngine.Profiling.Profiler.EndSample();");
                        csFile.Append("\n\t\t\tyield return null;");
						count = 0;
                    }
                }

				csFile.Append("\n\t\t\tPostInit();");
				csFile.Append("\n\t\t}");

				csFile.Append("\n\n\t\tpartial void PostInit();");

				csFile.Append("\n");

				foreach (var tableClass in tableClassNames)
				{
					string fileName = settings.ToAssetFileName(tableClass);
					string uniFileName = UniTypeName(fileName);
					if (fileName.Equals(uniFileName))
					{
						var context = sheetContexts[tableClass];
						string rowClass = context.rowClassName;
						csFile.Append($"\n\n\t\tpublic static {rowClass} Get{rowClass}({context.keyField.KeyFieldType} id)\n\t\t{{");
						csFile.Append($"\n\t\t\treturn s_instance.{context.tableClassName}.Get(id);");
						csFile.Append("\n\t\t}");

						csFile.Append($"\n\n\t\tpublic static List<{rowClass}> Get{rowClass}List()\n\t\t{{");
						csFile.Append($"\n\t\t\treturn s_instance.{context.tableClassName}.DataList;");
						csFile.Append("\n\t\t}");
					}
					else
					{
						var context = sheetContexts[tableClass];
						string rowClass = context.rowClassName;
						csFile.Append($"\n\n\t\tpublic static {uniFileName} Get{rowClass}({context.keyField.KeyFieldType} id)\n\t\t{{");
						csFile.Append($"\n\t\t\treturn s_instance.{context.tableClassName}.Get(id);");
						csFile.Append("\n\t\t}");

						csFile.Append($"\n\n\t\tpublic static List<{uniFileName}> Get{rowClass}List()\n\t\t{{");
						csFile.Append($"\n\t\t\treturn s_instance.{context.tableClassName}.DataList;");
						csFile.Append("\n\t\t}");
					}
				}

				csFile.Append("\n\n\t}\n");
				csFile.Append("}\n");

				return csFile.ToString();
			}
			catch (Exception ex)
			{
				EsLog.Error(ex.ToString());
			}

			return "";
		}

		private string UniTypeName(string fileName)
		{
			var dir = EsSettings.Instance.sameTypeDic;
			if (dir == null)
			{
				return fileName;
			}
			for (var i = 0; i < dir.Count; i++)
			{
				var sameTypes = dir[i].SameTypes;
				if (sameTypes != null && sameTypes.Contains(fileName))
				{
					return dir[i].UniType;
				}
			}

			return fileName;
		}

		private void PrasePredefinedTypes(string excelFilesDir)
		{
			try
			{
				string filePath = Path.Combine(excelFilesDir, EsConstant.PredefinedTypesFile);
				if (!File.Exists(filePath))
				{
					EsLog.Error($"Cannot find file {EsConstant.PredefinedTypesFile} in {excelFilesDir}");
					return;
				}

				var book = EsWorkbook.Load(filePath);
				if (book == null)
					return;

				var sheet = book.GetWorkSheet(0);
				if (sheet == null)
					return;
				sheet.LoadCells(sheet.RowCount);

				int fullNameCol = 0;
				int sepCol = 0;
				int commentCol = 0;
				int fieldNameCol = 0;
				int fieldTypeCol = 0;
				int fieldCommentCol = 0;

				for (var c = 0; c < sheet.ColumnCount; c++)
				{
					var str = sheet.GetCellValue(0, c);
					string fieldStr = sheet.GetCellValue(1, c);
					if (str == "full_name")
						fullNameCol = c;
					else if (str == "separator")
						sepCol = c;
					else if (str == "comment")
						commentCol = c;

					if (fieldStr == "name")
						fieldNameCol = c;
					else if (fieldStr == "type")
						fieldTypeCol = c;
					else if (fieldStr == "comment")
						fieldCommentCol = c;
				}

				PredefinedTypes.Clear();

				for (var row = 3; row < sheet.RowCount;)
				{
					string fullName = sheet.GetCellValue(row, fullNameCol);
					if (string.IsNullOrEmpty(fullName))
					{
						row++;
						continue;
					}

					string sep = sheet.GetCellValue(row, sepCol).Trim();
					if (string.IsNullOrEmpty(sep)) sep = "-";// default separator

					var type = new PredefinedType();
					PredefinedTypes.Add(fullName, type);
					type.fullName = fullName;
					type.isUnityType = fullName.Contains("UnityEngine.");
					type.sep = sep;
					type.comment = sheet.GetCellValue(row, commentCol).Trim();
					type.startRow = row;
					type.fields = new List<PredefinedField>(4);
					while ((row == type.startRow || string.IsNullOrEmpty(sheet.GetCellValue(row, fullNameCol)))
							&& !string.IsNullOrEmpty(sheet.GetCellValue(row, fieldNameCol)))
					{
						var field = new PredefinedField();
						field.name = sheet.GetCellValue(row, fieldNameCol);
						field.type = sheet.GetCellValue(row, fieldTypeCol);
						field.comment = sheet.GetCellValue(row, fieldCommentCol);
						type.fields.Add(field);

						row++;
					}
					type.endRow = row - 1;

					if (row == type.startRow) row++;
				}

			}
			catch (Exception ex)
			{
				EsLog.Error(ex.ToString());
			}
		}

		private void ToCSharpPredefinedClasses(string excelFilesDir, string output)
		{
			try
			{
				PrasePredefinedTypes(excelFilesDir);

				var settings = EsSettings.Instance;
				var csFile = new StringBuilder(10240);
				csFile.Append("//------------------------------------------------------------------------------\n");
				csFile.Append("// <auto-generated>\n");
				csFile.Append("//     This code was generated by EasySpreadsheet.\n");
				csFile.Append("//     Runtime Version: " + EsConstant.Version + "\n");
				csFile.Append("//\n");
				csFile.Append("//     Changes to this file may cause incorrect behavior and will be lost if\n");
				csFile.Append("//     the code is regenerated.\n");
				csFile.Append("// </auto-generated>\n");
				csFile.Append("//------------------------------------------------------------------------------");

				csFile.Append("\nusing System;");
				csFile.Append("\nusing System.Collections.Generic;");
				csFile.Append("\nusing System.Collections;");
				csFile.Append("\nusing EasySpreadsheet;\n\n");
				csFile.Append($"\nnamespace {settings.nameSpace}");
				csFile.Append("\n{");

				csFile.Append("\n#if UNITY_EDITOR\n\tpublic static class UnityTypesParse");
				csFile.Append("\n\t{");
				foreach (var pair in PredefinedTypes)
				{
					var type = pair.Value;
					if (!type.isUnityType) continue;
					csFile.Append($"\n\t\tpublic static void Parse(ref {type.fullName} v, string str)");
					csFile.Append("\n\t\t{");
					csFile.Append($"\n\t\t\tstring[] parts = str.Split('{type.sep}');");
					for (int i = 0; i < type.fields.Count; ++i)
					{
						var field = type.fields[i];
						csFile.Append($"\n\t\t\tif (parts.Length > {i}) {{ EsParser.TryParse(parts[{i}], out {field.type} _{field.name}_); v.{field.name} = _{field.name}_; }}");
					}
					csFile.Append("\n\t\t}\n");
				}
				csFile.Append("\n\t}\n#endif\n");

				foreach (var pair in PredefinedTypes)
				{
					var type = pair.Value;
					if (type.isUnityType) continue;
					if (!string.IsNullOrEmpty(type.comment.Trim()))
						csFile.Append($"\n\t/*{type.comment}*/");
					csFile.Append("\n\t[Serializable]");
					csFile.Append($"\n\tpublic partial class {type.fullName}");
					csFile.Append("\n\t{");
					foreach (var field in type.fields)
					{
						if (!string.IsNullOrEmpty(field.comment.Trim()))
							csFile.Append($"\n\t\t/* {field.comment}*/");
						csFile.Append($"\n\t\tpublic {field.type} {field.name};");
					}

					csFile.Append("\n\n#if UNITY_EDITOR");
					csFile.Append("\n\t\tpublic void Parse(string str)");
					csFile.Append("\n\t\t{");
					csFile.Append($"\n\t\t\tstring[] parts = str.Split('{type.sep}');");
					for (int i = 0; i < type.fields.Count; ++i)
					{
						var field = type.fields[i];

                        var isUnityType =
                            PredefinedTypes.Values.FirstOrDefault(tmp => tmp.isUnityType && tmp.fullName == field.type) != null;
                        if (isUnityType) {
                            csFile.Append($"\n\t\t\tif (parts.Length > {i}) {{\n\t\t\t\t{field.name} = new {field.type}();\n\t\t\t\tUnityTypesParse.Parse(ref {field.name}, parts[{i}]);\n\t\t\t}}");
                        } else {
                            csFile.Append($"\n\t\t\tif (parts.Length > {i}) EsParser.TryParse(parts[{i}], out {field.name});");
                        }
					}
					csFile.Append("\n\t\t}");
					csFile.Append("\n#endif");

					csFile.Append("\n\t}\n");
				}

				csFile.Append("\n\n}");

				File.WriteAllText(Path.Combine(output, "_PredefinedTypes.cs"), csFile.ToString());

			}
			catch (Exception ex)
			{
				EsLog.Error(ex.ToString());
			}
		}

	}
}
