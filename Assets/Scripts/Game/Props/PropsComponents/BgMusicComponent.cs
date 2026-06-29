
using Game.Base;
using Game.ECS;
using Google.Protobuf.WellKnownTypes;
using Pb.Map;
using UnityEngine;

namespace Game.Props.PropsComponents
{
	public class BgMusicComponent : BaseComponent, IComponentSerializer
	{

		/// <summary>
		/// 背景音乐ID
		/// </summary>
		public string bgId = "10000";

		/// <summary>
		/// 白噪音ID
		/// </summary>
		public string noiseId = "20000";


		/// <summary>
		/// 用户上传的音乐地址
		/// </summary>
		public string ugcMusicUrl = "";

        public int loudness = 0;



		public void Read(PComponentData componentData)
		{
			if (componentData.CmpData != null &&  componentData.CmpData.Is(PBgMusicData.Descriptor))
			{
				if (componentData.CmpData.TryUnpack<PBgMusicData>(out var pbBodyData))
				{
					bgId = pbBodyData.BgId;
					noiseId = pbBodyData.NoiseId;
					ugcMusicUrl = pbBodyData.UgcMusicUrl;
                    loudness = pbBodyData.Loudness;
				}
			}
		}

		public PComponentData Write()
		{
			var componentData = new PComponentData();
			var pBgComponentData = new PBgMusicData()
			{
				BgId = bgId,
				NoiseId = noiseId,
				UgcMusicUrl = ugcMusicUrl,
                Loudness = (int)loudness
			};
			componentData.CmpData = Any.Pack(pBgComponentData);
			return componentData;
		}

		public override BaseComponent Clone()
		{
			var component = new BgMusicComponent()
			{
				bgId = bgId,
				noiseId = noiseId,
				ugcMusicUrl = ugcMusicUrl,
                loudness = loudness
			};
			return component;
		}
	}
}

