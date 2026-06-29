using System;
using System.Collections.Generic;
using System.IO;
using OfficeOpenXml;

namespace EasySpreadsheet
{
	public class EsWorkbook
	{
		private ExcelWorkbook workbook;
		private List<EsWorksheet> _sheets;

		public int sheetCount => workbook?.Worksheets.Count ?? 0;

		public List<EsWorksheet> sheets
		{
			get
			{
				if (_sheets == null)
				{
					try
					{
						_sheets = new List<EsWorksheet>();
						if (workbook != null)
							foreach (var sheet in workbook.Worksheets)
								sheets.Add(new EsWorksheet(sheet));
					}
					catch (Exception e)
					{
						EsLog.Error(e.ToString());
					}
				}

				return _sheets;
			}
		}

		private EsWorkbook()
		{
		}

		private EsWorkbook(ExcelWorkbook workbook)
		{
			this.workbook = workbook;
		}

		public EsWorksheet GetWorkSheet(int index)
		{
			int i = 0;
			foreach (var sheet in workbook.Worksheets)
			{
				if (i == index)
					return new EsWorksheet(sheet);
				i++;
			}

			return null;
		}

		public EsWorksheet AddWorksheet(string name)
		{
			var sheet = new EsWorksheet {name = name};
			sheets.Add(sheet);
			return sheet;
		}

		public void Dump()
		{
			try
			{
				foreach (var t in sheets)
					t.Dump();
			}
			catch (Exception e)
			{
				EsLog.Error(e.ToString());
			}
		}

		public static EsWorkbook Load(string path)
		{
			try
			{
				if (!File.Exists(path))
				{
					EsLog.Error("Cannot find file " + path);
					return null;
				}

				var file = new FileInfo(path);
				var ep = new ExcelPackage(file);
				var workbook = new EsWorkbook(ep.Workbook);
				return workbook;
			}
			catch (Exception e)
			{
				EsLog.Error($"Failed to load [{path}]: {e}");
			}

			return null;
		}

		public static EsWorkbook Create(string firstSheetName = "Sheet 1")
		{
			try
			{
				using (var ep = new ExcelPackage())
				{
					ep.Workbook.Worksheets.Add(firstSheetName);
					var workbook = new EsWorkbook(ep.Workbook);
					return workbook;
				}
			}
			catch (Exception e)
			{
				EsLog.Error(e.ToString());
				return null;
			}
		}

		public void SaveToFile(string path)
		{
			try
			{
				var output = new FileInfo(path);
				using (var ep = new ExcelPackage())
				{
					foreach (var s in sheets)
					{
						var sheet = ep.Workbook.Worksheets.Add(s.name);
						s.CopyTo(sheet);
					}

					ep.SaveAs(output);
				}
			}
			catch (Exception e)
			{
				EsLog.Error(e.ToString());
			}
		}
	}
}