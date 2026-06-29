using System;
using System.Collections.Generic;
using OfficeOpenXml;
using UnityEngine;

namespace EasySpreadsheet
{
	public class EsWorksheet
	{
		public string name;

		private ExcelWorksheet sheet;
		
		private Dictionary<int, Dictionary<int, EsWorksheetCell>> cells;

		private int _columnCount;
		
		private int _rowCount;

		public int ColumnCount
		{
			get { return _columnCount; }
			set { _columnCount = value; }
		}

		public int RowCount
		{
			get { return _rowCount; }
			set { _rowCount = value; }
		}
		
		public Vector2 position;
		

		public EsWorksheet()
		{
			RowCount = 0;
			ColumnCount = 0;
		}

		public EsWorksheet(ExcelWorksheet sheet)
		{
			name = sheet.Name;
			this.sheet = sheet;
			if (sheet.Dimension != null)
			{
				RowCount = sheet.Dimension.Rows;
				ColumnCount = sheet.Dimension.Columns;
			}
			else//empty Sheet
			{
				RowCount = 0;
				ColumnCount = 0;
			}
			
			cells = new Dictionary<int, Dictionary<int, EsWorksheetCell>>(16);
		}
		
		public void CopyTo(ExcelWorksheet target)
		{
			if (target == null) return;
			for (var row = 0; row < RowCount; row++)
				for (var column = 0; column < ColumnCount; column++)
					target.Cells[row + 1, column + 1].Value = GetCellValue(row, column);
		}

		public void LoadCells(int maxRow)
		{
			maxRow = Math.Min(RowCount, maxRow);
			if (cells.Count < maxRow)
				cells = new Dictionary<int, Dictionary<int, EsWorksheetCell>>(maxRow);
			
			for (var row = 0; row < maxRow; row++)
			for (var column = 0; column < ColumnCount; column++)
			{
				var cellValue = sheet.Cells[row + 1, column + 1].Value;
				var value = cellValue == null ? "" : cellValue.ToString();
				SetCellValue(row, column, value);
			}
		}
		
		public void LoadAllCells()
		{
			LoadCells(RowCount);
		}

		/// <summary>
		/// Set cell's value
		/// </summary>
		/// <param name="row">Row of target cell, from 0 to RowCount</param>
		/// <param name="column">Column of target cell, from 0 to ColumnCount</param>
		/// <param name="value">Value of string to set</param>
		public EsWorksheetCell SetCellValue(int row, int column, string value)
		{
			if (row < 0 || column < 0)
				return null;

			if (RowCount <= row)
				RowCount = row + 1;
			if (ColumnCount <= column)
				ColumnCount = column + 1;

			Dictionary<int, EsWorksheetCell> targetRow;
			if (!cells.TryGetValue(row, out targetRow))
			{
				targetRow = new Dictionary<int, EsWorksheetCell>(ColumnCount);
				cells.Add(row, targetRow);
			}

			EsWorksheetCell targetCell;
			if (!targetRow.TryGetValue(column, out targetCell))
			{
				targetCell = new EsWorksheetCell(row, column, value);
				targetRow.Add(column, targetCell);
			}

			return targetCell;
		}

		/// <summary>
		/// Get value from cell
		/// </summary>
		/// <param name="row">Row of target cell, from 0 to RowCount</param>
		/// <param name="column">Column of target, from 0 to ColumnCount</param>
		public string GetCellValue(int row, int column)
		{
			if (row < 0 || column < 0)
				return null;
			var cell = GetCell(row, column);
			return cell != null ? cell.value : string.Empty;
		}

		public EsWorksheetCell GetCell(int row, int column)
		{
			Dictionary<int, EsWorksheetCell> targetRow;
			if (cells.TryGetValue(row, out targetRow))
			{
				EsWorksheetCell targetCell;
				if (targetRow.TryGetValue(column, out targetCell))
					return targetCell;
			}
			
			return null;
		}

		/*public void SetCellTypeByRow(int row, CellType type)
		{
			for (var column = 0; column < columnCount; column++)
			{
				var cell = GetCell(row, column);
				if (cell != null) cell.type = type;
			}
		}

		public void SetCellTypeByColumn(int column, CellType type, List<string> values = null)
		{
			for (var row = 1; row <= rowCount; row++)
			{
				var cell = GetCell(row, column);
				if (cell == null) continue;
				cell.type = type;
				if (values != null) cell.ValueSelected = values;
			}
		}*/

		public void Dump()
		{
			try
			{
				var msg = "";
				for (var row = 0; row < RowCount; row++)
				{
					for (var column = 0; column < ColumnCount; column++)
						msg += string.Format("{0} ", GetCellValue(row, column));
					msg += "\n";
				}
				Debug.Log(msg);
			}
			catch (Exception e)
			{
				EsLog.Error(e.ToString());
			}
			
		}
	}
}