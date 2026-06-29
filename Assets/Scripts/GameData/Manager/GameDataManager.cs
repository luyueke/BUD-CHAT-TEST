// @Author: YangJie
// @Description:
// @Date:  2023/07/20
// @Modify:

using GameData.GameSync;
using GameData.MapData;
using Message;

namespace GameData.Manager
{
    
    /// <summary>
    /// 游戏中全局数据
    /// </summary>
    public class GameDataManager : GlobalInstance<GameDataManager>
    {
        public MapGlobalData mapGlobalData = new MapGlobalData();
        public AccountUserInfo accountUserInfo = new AccountUserInfo();
        public GameOnlineData gameOnlineData = new GameOnlineData();
        public GlobalSettingData globalSettingData = new GlobalSettingData();
        public AppSettingData appSetting = new AppSettingData();
        private EditorSessionData _editorSessionData;
        public EditorSessionData editorSessionData
        { 
            get 
            {
                if (_editorSessionData == null)
                {
                    _editorSessionData = new EditorSessionData();
                }
                return _editorSessionData;
            }
            set { _editorSessionData = value; }
        }

        public GameDataManager()
        {
            MessageHelper.AddListener(MessageName.StartExitGame, OnStartExitGame);
        }


        public bool IsLowMobile()
        {
            return appSetting.quality < (int) ModelClassificationType.Middle;
        }

        public ModelClassificationType GetMobileQuality()
        {
            return (ModelClassificationType) appSetting.quality;
        }

        public override void Release()
        {
            base.Release();
            MessageHelper.RemoveListener(MessageName.StartExitGame, OnStartExitGame);
        }

        void OnStartExitGame()
        {
            editorSessionData = null;
        }
    }
}