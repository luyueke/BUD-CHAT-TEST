/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-08-04 12:03:46
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-08-11 17:07:56
 * @ Description: 移动属性的Component
 */


using Game.Base;
using Game.ECS;
using System.Collections.Generic;
using Google.Protobuf.WellKnownTypes;
using Pb.Map;
using UnityEngine;
using System.Linq;
using Google.Protobuf.Collections;
using HLOD;

namespace Game.Props.PropsComponents
{
	public static class MovmentConfig
	{
		public static float[] MoveSpeed = { 1, 4, 8 }; // 移动速度
		public static float[] TurnAroundTime = { 0.4f, 0.6f, 1.2f }; // 转向速度
	}

	public class MovementComponent : BaseComponent, IComponentSerializer, IMovedComponent
	{
		public bool TurnAround = true;
		public bool AutoPlay = true; // 自动开始
		public int SpeedLv = 0; // 0 慢 1 中 2快
		public List<Vector3> PathPoints = new List<Vector3>(); // 世界坐标

		// 临时变量,不会进行序列化
		// 缓存当前路点的实例化对象。
		public List<Transform> PathPointTFCache = new List<Transform>();
		public Vector3 TmpOriginPosition = Vector3.zero;
		public Vector3 TmpOriginRotation = Vector3.zero;
		public bool TmpAnimIsRun = false; // 是否在运行的记录
		public Transform TmpAnimNode = null; // 是否在运行的动画节点

        public bool isMigrated;


		public void Read(PComponentData componentData)
		{
			if (componentData.CmpData.TryUnpack<PMovementComponentData>(out var pbBodyData))
            {
				TurnAround = pbBodyData.TurnAround;
				AutoPlay = pbBodyData.AutoPlay;
				SpeedLv = pbBodyData.SpeedLv;
				PathPoints.AddRange(pbBodyData.PathPoints.Select(x=>x.ToVector3()));
                isMigrated = pbBodyData.IsMigrated;
            }
		}

		public PComponentData Write()
		{
			var pbBodyData = new PMovementComponentData();
			pbBodyData.TurnAround = TurnAround;
			pbBodyData.AutoPlay = AutoPlay;
			pbBodyData.SpeedLv = SpeedLv;
			RepeatedField<PVector3> pathPoint = new RepeatedField<PVector3>();
			pbBodyData.PathPoints.AddRange(PathPoints.Select(x=>x.ToPB()));
            pbBodyData.IsMigrated = isMigrated;

			var componentData = new PComponentData();
            componentData.CmpData = Any.Pack(pbBodyData);
            return componentData;
		}

		public override BaseComponent Clone()
		{
			var component = new MovementComponent()
			{
				TurnAround = TurnAround,
				SpeedLv = SpeedLv,
				AutoPlay = AutoPlay,
				PathPoints = new List<Vector3>(PathPoints)
			};
			return component;
		}

        public bool IsMoved() {
            return PathPoints is { Count: > 0 };
        }
    }
}

