/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-08-02 17:15:01
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-08-10 18:14:06
 * @ Description: 动画通用属性的Component
 */


using DG.Tweening;
using Game.Base;
using Game.ECS;
using Google.Protobuf.WellKnownTypes;
using Pb.Map;
using UnityEngine;

namespace Game.Props.PropsComponents
{
	public static class RPAnimConfig
	{
		public static Ease[] UpEases = { Ease.Unset, Ease.InOutCubic, Ease.OutCubic, Ease.OutCubic };
		public static Ease[] DownEases = { Ease.Unset, Ease.InOutCubic, Ease.InCubic, Ease.InCubic };
		public static float[] UpdownDurations = { 0, 1.2f, 0.2f, 0.1f }; // 上下弹跳速度
		public static float[] RotSpeeds = { 0, 20, 10, 6.7f }; // 旋转速度(秒/圈)
	}

	public class RPAnimComponent : BaseComponent, IComponentSerializer
	{
		// 旋转速度
		public int RSpeed = 0;
		// 上下弹跳速度
		public int USpeed = 0;
		/// 0 X轴， 1 Y轴， 2 Z轴
		public int RAxis = 1;
		// 游戏开始时是否自动开始
		public bool AutoPlay = true;

		// 临时变量
		public Vector3 TmpOriginPosition = Vector3.zero;
		public Vector3 TmpOriginRotation = Vector3.zero;
		public bool TmpAnimIsRun = false; // 是否在运行的记录
		public Transform TmpAnimNode = null; // 是否在运行的动画节点

		public void Read(PComponentData componentData)
		{
			if (componentData.CmpData.TryUnpack<PRPAnimComponentData>(out var pbBodyData))
            {
				RSpeed = pbBodyData.RSpeed;
				USpeed = pbBodyData.USpeed;
				RAxis = pbBodyData.RAxis;
				AutoPlay = pbBodyData.AutoPlay;
            }
		}

		public PComponentData Write()
		{
			var pbBodyData = new PRPAnimComponentData();
			pbBodyData.RSpeed = RSpeed;
			pbBodyData.USpeed = USpeed;
			pbBodyData.RAxis = RAxis;
			pbBodyData.AutoPlay = AutoPlay;
            
			var componentData = new PComponentData();
            componentData.CmpData = Any.Pack(pbBodyData);
            return componentData;
		}

		public override BaseComponent Clone()
		{
			var component = new RPAnimComponent()
			{
				RSpeed = RSpeed,
				USpeed = USpeed,
				RAxis = RAxis,
				AutoPlay = AutoPlay,
			};
			return component;
		}
	}
}
        
