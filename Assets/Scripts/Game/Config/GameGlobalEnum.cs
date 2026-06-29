namespace Game.Config
{
    public class GameGlobalEnum
    {
        // 道具操作流程枚举
        public enum NodeOpReason
        {
            CreatePrepare,
            CreateSuccess,
            CreateFail_MaxNum, // 添加失败 (最大上限)
            RemovePrepare,
            RemoveSuccess,
            RemoveFail_MinNum, // 移除失败（最小下限）
            Unknown, // 未知原因
        }

        // 道具UI的分栏类型
        public enum BannerType
        {
            GameSetting = 0, //通关设置
            Environment = 1, //环境
            Sound = 2, //声音
            
            BasicModel = 3, // 基础模型
            BasicProp = 4, // 基础道具
            LogicProp = 5, // 逻辑道具
            

		}
        
        public enum LockHideType
        {
            Lock,
            Hide,
            Show
        }
        
        
        public enum WeatherType
        {
            None = 0,
            Rain,
            Snow
        }
        
        public enum PropControlType
        {
            Visible, //显隐控制
            Movement, // 移动控制
            Sound, // 声音播放控制
            Anim, // 旋转移动控制
            Firework,//烟花道具控制
        }
        
        public enum TrapBoxTrans
        {
            MapSpawn = 0,//地图出生点
            CustomSpawn = 1,//自定义出生点
            NoTrans = 2, //原地
            CheckPoint = 3 //存档点
        }

    }
}