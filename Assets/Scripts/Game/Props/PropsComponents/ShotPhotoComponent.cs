
using Game.Base;
using Game.ECS;
using Google.Protobuf.WellKnownTypes;
using Pb.Map;

namespace Game.Props.PropsComponents
{
    public enum ShotPhotoSaveType
    {
        CheckInPhoto = 0,
        SystemPhoto = 1,
    }
	
	public class ShotPhotoComponent : BaseComponent, IComponentSerializer
	{
		public string PhotoUrl = "";
		public ShotPhotoSaveType PhotoType = ShotPhotoSaveType.SystemPhoto;

		public void Read(PComponentData componentData)
		{
			if (componentData.CmpData.TryUnpack<PShotPhotoComponentData>(out var pbBodyData))
            {
				PhotoType = (ShotPhotoSaveType)pbBodyData.PhotoType;
				PhotoUrl = pbBodyData.PhotoUrl;
            }
		}

		public PComponentData Write()
		{
			var pbBodyData = new PShotPhotoComponentData();
			pbBodyData.PhotoUrl = PhotoUrl;
			pbBodyData.PhotoType = (int)PhotoType;
            
			var componentData = new PComponentData();
            componentData.CmpData = Any.Pack(pbBodyData);
            return componentData;
		}

		public override BaseComponent Clone()
		{
			var component = new ShotPhotoComponent()
			{
				PhotoUrl = PhotoUrl,
				PhotoType = PhotoType,
			};
			return component;
		}
	}
}
        
