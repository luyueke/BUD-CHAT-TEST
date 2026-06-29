using System.Collections.Generic;
using System.Linq;

namespace prop.editor
{
	public class EnumGenerated : BaseGenerated
	{
		private List<EnumListStruct> sortValueList = new List<EnumListStruct>();
		private string namespacename;
		private string enumname;
        public EnumGenerated(string path, string namespacename, string enumname, List<EnumListStruct> enumLists): base(path)
        {
			sortValueList.AddRange(enumLists);
			sortValueList.Sort((a,b) => a.Id - b.Id);
			this.namespacename = namespacename;
			this.enumname = enumname;
        }

		public override void Generate()
		{
			WriteHeader();

			WirteContinue($"namespace {namespacename}");
			WirteContinue("{");

			WirteContinue($"\tpublic enum {enumname}");
			WirteContinue("\t{");
			sortValueList.ForEach(x => 
			{
				WirteContinue($"\t\t{x.Name} = {x.Id},");
			});
			WirteContinue("\t}");

			WirteContinue("}");

			WriteToFile();
		}
	}
}