using System;
using System.Collections.Generic;
using UnityEditor;

namespace EasySpreadsheet
{
	/// <summary>
	///     Spreadsheet Converter
	/// </summary>
	public partial class EsConverter
	{
		public readonly EsColumnFieldParser FieldParser;
		public readonly Dictionary<string, PredefinedType> PredefinedTypes = new Dictionary<string, PredefinedType>(32);
		private readonly Dictionary<string, DataTableSheetContext> sheetContexts = new Dictionary<string, DataTableSheetContext>(64);

		public EsConverter()
		{
			FieldParser = new EsColumnFieldParser(this);
		}
		
		private DataTableSheetContext CreateDataTableSheetContext(EsWorksheet sheet)
		{
			var context = new DataTableSheetContext(this);
			context.sheetName = sheet.name;
			
			GetHeaderRows(sheet, out context.FieldNameRow, out context.FieldTypeRow, out context.DataStartRow);
			
			var validNameColumns = new List<int>();
			for (var column = EsConstant.FieldStartColumn; column < sheet.ColumnCount; column++)
			{
				var cellValue = sheet.GetCellValue(context.FieldNameRow, column);
				if (!string.IsNullOrEmpty(cellValue))
					validNameColumns.Add(column);
			}
			var validTypeColumns = new List<int>();
			for (var column = 0; column < validNameColumns.Count; column++)
			{
				var cellValue = sheet.GetCellValue(context.FieldTypeRow, validNameColumns[column]);
				if (FieldParser.IsSupportedType(cellValue))
					validTypeColumns.Add(validNameColumns[column]);
			}
			
			for (var i = 0; i < sheet.RowCount; i++)
			{
				var rowData = new List<string>();
				foreach (var c in validTypeColumns)
				{
					string cellValue = sheet.GetCellValue(i, c);
					rowData.Add(cellValue);
				}

				context.Table.Add(rowData);
			}

			context.RowCount = sheet.RowCount;
			context.ColumnCount = validTypeColumns.Count;

			return context;
		}
		
		private bool IsValidDataTableSheet(EsWorksheet sheet)
		{
			if (sheet == null || sheet.RowCount < 3 || sheet.ColumnCount < 2)
				return false;
			
			int validColumnCount = 0;
			
			if (!GetHeaderRows(sheet, out int nameRow, out int typeRow, out int dataRow))
				return false;
			
			for (int col = EsConstant.FieldStartColumn; col < sheet.ColumnCount; col++)
			{
				string varType = sheet.GetCellValue(typeRow, col).Trim();
				if (string.IsNullOrEmpty(varType) || varType.Equals("\r"))
					continue;
				if (FieldParser.IsSupportedType(varType))
				{
					string varName = sheet.GetCellValue(nameRow, col);
					if (!string.IsNullOrEmpty(varName))
						validColumnCount++;
				}
			}
			
			return validColumnCount > 0;
		}

		private bool IsSpreadsheetFile(string filePath)
		{
			return EsEditorUtility.IsSpreadsheetFileSupported(filePath);
		}
		
		private bool GetHeaderRows(EsWorksheet sheet, out int nameRow, out int typeRow, out int dataRow)
		{
			nameRow = typeRow = dataRow = -1;
			for (var i = 0; i < Math.Min(sheet.RowCount, EsConstant.HeaderRowRange); i++)
			{
				string cellValue = sheet.GetCellValue(i, 0).Trim().ToLower();

				if (cellValue.Equals(EsConstant.FieldNameFlag))
					nameRow = i;
				else if (cellValue.Equals(EsConstant.FieldTypeFlag))
					typeRow = i;
				else if (i > nameRow && i > typeRow && !cellValue.Equals(EsConstant.HeaderFlag))
					dataRow = i;
				
				if (nameRow >= 0 && typeRow >= 0 && dataRow >= 0)
					return true;
			}

			return false;
		}

		private bool isDisplayingProgress;
		
		private void UpdateProgressBar(int progress, int progressMax, string desc)
		{
			var title = "EasySpreadsheet importing...[" + progress + " / " + progressMax + "]";
			var value = progress / (float) progressMax;
			EditorUtility.DisplayProgressBar(title, desc, value);
			isDisplayingProgress = true;
		}

		private void ClearProgressBar()
		{
			if (!isDisplayingProgress) return;
			try
			{
				EditorUtility.ClearProgressBar();
			}
			catch (Exception)
			{
				// ignored
			}

			isDisplayingProgress = false;
		}

		public class DataTableSheetContext
		{
			private EsConverter _converter;
			private readonly List<List<string>> table = new List<List<string>>();
			public int ColumnCount;
			public int RowCount;

			public string sheetName;
			public string tableClassName;
			public string rowClassName;
			public EsColumnField[] fields;
			public EsColumnField keyField;

			public int FieldNameRow = -1;
			public int FieldTypeRow = -1;
			public int DataStartRow = -1;

			public string rowTxt;
			public string tableTxt;
			public string editorTxt;

			public List<List<string>> Table => table;

			
			public DataTableSheetContext(EsConverter conv)
			{
				_converter = conv;
			}

			public string Get(int row, int column)
			{
				return table[row][column];
			}

			public void Set(int row, int column, string value)
			{
				table[row][column] = value;
			}

			public void ParseFields()
			{
				if (fields != null) return;

				fields = new EsColumnField[ColumnCount];

				for (var col = 0; col < ColumnCount; col++)
				{
					var rawColumnName = Get(FieldNameRow, col);
					if (rawColumnName.Contains(EsConstant.HeaderFlag) || string.IsNullOrEmpty(rawColumnName.Trim()))
						continue;
					var rawColumnType = Get(FieldTypeRow, col);
					if (rawColumnType.Contains(EsConstant.HeaderFlag) || string.IsNullOrEmpty(rawColumnType.Trim()))
						continue;
					
					var field = _converter.FieldParser.Parse(col, rawColumnName, rawColumnType);
					if (field == null)
						continue;
					
					fields[col] = field;
					if (field.IsKeyField)
						keyField = field;
				}

				if (keyField == null && fields[0] != null)
				{
					keyField = fields[0];
					keyField.SetAsKeyField();
				}
			}
		}


		public class PredefinedType
		{
			public string fullName;
			public string sep;
			public string comment;
			public int startRow;
			public int endRow;
			public bool isUnityType;

			public List<PredefinedField> fields;
		}

		public class PredefinedField
		{
			public string name;
			public string type;
			public string comment;
		}
		
	}
}