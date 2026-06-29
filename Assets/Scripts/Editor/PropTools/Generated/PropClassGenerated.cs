/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-07-20 18:28:03
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-08-24 11:40:29
 * @ Description: 三件套生成器
 */

using System.Collections.Generic;
using System.Linq;
using common.editor;
using Game.Generated;
using GameData.Config;

namespace prop.editor
{
	public class TemplateGenerated : BaseGenerated
	{
		protected PropGeneratedContext context;
		protected string tmpContent;
		public TemplateGenerated(PropGeneratedContext context): base("")
        {
			this.context = context;
        }

		public TemplateGenerated Generate(string path, string tmpContent)
		{
			this.path = path;
			this.tmpContent = tmpContent;
			Generate();
			return this;
		}

		public override void Generate()
		{
			WirteContinue(tmpContent);
			Replace("${Name}", context.Name);

			WriteToFile();
		}
	}

	public class ManagerRegisterFileGenerated : TemplateGenerated
	{
		Dictionary<string, string> Dict;
		public ManagerRegisterFileGenerated(PropGeneratedContext context): base(context)
        {
			Dict = new Dictionary<string, string>();
			var managerRegister = new EntityManagerRegister();
			managerRegister.Dict.Keys.ToList().ForEach(x=>
			{
				Dict.Add(x.ToString(), managerRegister.Dict[x].Name);
			});
			if (!string.IsNullOrWhiteSpace(context.NodeModelTypeName))
				Dict.Add(context.NodeModelTypeName, $"{context.Name}Manager");

			this.path = Const.PropManagerRegisterFile;
			this.tmpContent = GeneratedTemplate.ManagerRegisterTmp;
        }

		public override void Generate()
		{
			WriteHeader();
			WirteContinue(tmpContent);
			string contentStr = "\n";
			this.Dict.Keys.ToList().ForEach(x=>{
				contentStr += $"\t\t\tDict.Add(NodeModelType.{x}, typeof({this.Dict[x]}));\n";
			});
			Replace("${Content}", contentStr);

			WriteToFile();
		}
	}

	public class ComponentRegisterFileGenerated : TemplateGenerated
	{
		Dictionary<string, string> Dict;
		public ComponentRegisterFileGenerated(PropGeneratedContext context): base(context)
        {
			Dict = new Dictionary<string, string>();
			var componentRegister = new EntityComponentRegister();
			componentRegister.Dict.Keys.ToList().ForEach(x=>
			{
				Dict.Add(x.ToString(), componentRegister.Dict[x].Name);
			});
			if (!string.IsNullOrWhiteSpace(context.NodeComponentIdName))
				Dict.Add(context.NodeComponentIdName, $"{context.Name}Component");

			this.path = Const.PropComponentRegisterFile;
			this.tmpContent = GeneratedTemplate.ComponentRegisterTmp;
        }

		public override void Generate()
		{
			WriteHeader();
			WirteContinue(tmpContent);
			string contentStr = "\n";
			this.Dict.Keys.ToList().ForEach(x=>{
				contentStr += $"\t\t\tDict.Add(NodeComponentId.{x}, typeof({this.Dict[x]}));\n";
			});
			Replace("${Content}", contentStr);

			WriteToFile();
		}
	}

	public class PropGeneratedContext
	{
		public string Name;
		public string NodeComponentIdName;
		public string NodeModelTypeName;
	}

	public class PropClassGenerated
	{
		private PropGeneratedContext context;
		public PropClassGenerated(PropGeneratedContext context)
		{
			this.context = context;
		}

		public void Generate()
		{
			// 三件套的类
			var templeteGen = new TemplateGenerated(context)
				.Generate($"{Const.PropLogicBehaviourPath}/{context.Name}Behaviour.cs", GeneratedTemplate.BehaviourTmp);
				
			if (context.NodeComponentIdName != NodeComponentId.None.ToString())
			{
				templeteGen.Generate($"{Const.PropLogicComponentPath}/{context.Name}Component.cs", GeneratedTemplate.ComponentTmp);
				templeteGen.Generate($"{Const.PropLogicManagerPath}/{context.Name}Manager.cs", GeneratedTemplate.ManagerTmp);
			} else {
				templeteGen.Generate($"{Const.PropLogicManagerPath}/{context.Name}Manager.cs", GeneratedTemplate.ManagerNoCompTmp);
			}

			// Register
			new ManagerRegisterFileGenerated(context).Generate();
			if (context.NodeComponentIdName != NodeComponentId.None.ToString())
			{
				new ComponentRegisterFileGenerated(context).Generate();
			}
		}
	}
}