using Es;
using GameData.BaseInfo;
using GameData.UGCData;
using Pb.Map;
/**
* @ Author: Jun Zhou
* @ Create Time: 2023-09-07 18:18:19
* @ Modified by: Jun Zhou
* @ Modified time: 2023-09-07 18:21:25
* @ Description: 选择材质列表的材质数据
*/
namespace GameData
{
    public enum MatGroupTypeEnum
    {
        Wood = 0, // 木质
        Stone = 1, // 石头
        Grass = 2, // 草
        Metal = 3, // 金属
        Pattern = 4, //图案
        Other = 5, //其他
        Anime = 6, // 动漫
        Store = 99, // 社区材质
    }

    public class MaterialUnionID
	{
		public string UGCId { get; private set; } = "";
		public int MatId { get; private set; } = 158;
		public bool IsUGC { get; private set; } = false;
        
        public bool IsTransparent {
            get; private set;
        }

        public bool IsEmission {
            get; private set;
        }

        public MaterialUnionID(){}

		public MaterialUnionID(string id)
		{
			UGCId = id;
			IsUGC = true;
			MatId = 0;
		}

        public MaterialUnionID(int id, string uId) {
            if (string.IsNullOrEmpty(uId)) {
                UGCId = "";
                IsUGC = false;
                MatId = id;
                var matConfig = DataTables.GetMatDataConfig(id);
                if (matConfig != null) {
                    IsTransparent = matConfig.IsTransparent;
                    IsEmission = matConfig.IsEmission;
                }
            } else {
                UGCId = uId;
                IsUGC = true;
                MatId = 0;
            }
        }

        public MaterialUnionID(int id)
		{
			UGCId = "";
			IsUGC = false;
			MatId = id;
            var matConfig = DataTables.GetMatDataConfig(id);
            if (matConfig != null) {
                IsTransparent = matConfig.IsTransparent;
                IsEmission = matConfig.IsEmission;
            }
        }

		public MaterialUnionID ParsePB(PMaterialComponentData data)
		{
			IsUGC = !string.IsNullOrEmpty(data.UMat);
            
			if (IsUGC)
			{
				UGCId = data.UMat;
			} else {
				MatId = data.MatId;
                var matConfig = DataTables.GetMatDataConfig(MatId);
                if (matConfig != null) {
                    IsTransparent = matConfig.IsTransparent;
                    IsEmission = matConfig.IsEmission;
                }
			}
            return this;
		}

        public MaterialUnionID Clone()
		{
			if (IsUGC)
				return new MaterialUnionID(UGCId);

			return new MaterialUnionID(MatId);
		}

        public static bool operator ==(MaterialUnionID left, MaterialUnionID right)
        {
            if (left is null)
            {
                return right is null;
            }

            return left.Equals(right);
        }

        public static bool operator !=(MaterialUnionID left, MaterialUnionID right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            var equObj = obj as MaterialUnionID;
            if (IsUGC != equObj.IsUGC) return false;

            return IsUGC ? UGCId == equObj.UGCId : MatId == equObj.MatId;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
	        return IsUGC ? UGCId : MatId.ToString();
        }
	}

    public class GameMatUIData
    {
        public MaterialUnionID Id;
        public MatGroupTypeEnum MatGroupType;
        public bool IsSelected;
        public bool IsStoreGoStyle;
        public string Url;
        public int UgcStyle;
        public GameMatUIData()
        {

        }

        public GameMatUIData(MatDataConfig matDataConfig)
        {
            Id = new MaterialUnionID(matDataConfig.Id);
            MatGroupType = (MatGroupTypeEnum)matDataConfig.MatGroupType;
            Url = matDataConfig.IconName;
        }

        public GameMatUIData(ResInfo<MaterialInfo> resInfo)
        {
            Id = new MaterialUnionID(resInfo.ugcInfo.id);
            MatGroupType = MatGroupTypeEnum.Store;
            Url = resInfo.ugcInfo.cover;
            UgcStyle = resInfo.ugcInfo.ugcStyle;
        }
    }
}
