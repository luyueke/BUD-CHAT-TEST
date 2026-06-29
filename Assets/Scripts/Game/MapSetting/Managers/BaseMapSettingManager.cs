

using Game.Base;

// namespace Game.MapSetting
// {
	/// <summary>
	/// 编辑器内，设置类基类，如天气系统、后处理、天空盒等
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public abstract class BaseMapSettingManager<T> : GameInstance<T>, IMapSetting, IAutoInit where T : BaseInstance, new()
	{
		public virtual void OnCreateByData()
		{
			
		}

		public virtual void Init()
		{
			
		}
	}
// }