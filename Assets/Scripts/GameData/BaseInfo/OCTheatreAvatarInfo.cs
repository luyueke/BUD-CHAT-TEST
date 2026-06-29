using System.Collections.Generic;
using GameData.Base;

namespace GameData.BaseInfo
{
    public class OCTheatreAvatarInfo : UgcBaseInfo
    {
        public PaymentInfo paymentInfo; //支付信息
        public int isBan;
        public int isDelete;
        public int gender; //性别
        public string backgroundDes; //背景描述
        public List<string> personalities; //性格
        public List<string> importantPersons; //重要人物
        public List<OCTAvatarExpression> expressions; //立绘表情
        public List<OTCAvatarClothes> avatarClothes; //衣服
        public string barColor; //横条背景颜色
        public string mainColor; //主色调
        public string textColor; //文字颜色
        public string bgColor; //背景颜色
        public string bgUrl;//玩家自己上传的演员卡背景
    }

    public class OCTAvatarExpression{
        public string expressionName;
        public string expressionURL;
        public int expressionType;//常态 表情 自定义 
        public int mType; //表情类型，区分是立绘表情还是头像表情
    }

    public class OTCAvatarClothes{
        public int clothesIndex; //衣服Index
        public string clothesName; //衣服名字
        public string clothesURL; //衣服url
        public string clothesJson; //衣服json

        public int isDef;//是否默认 1是默认
    }
}