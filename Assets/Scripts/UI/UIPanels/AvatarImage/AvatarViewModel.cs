using System;
using Game.Avatar;
using GameData.PgcData;
using Network;
using Network.Http;
using Newtonsoft.Json;

public class AvatarViewModel: IDisposable
{
    public CharacterData InputData;
    private bool IsNewUser = false;

    public AvatarMenuType DefaultMenuType { get; private set; }
    public AvatarSubType? DefaultPart { get; private set; }

    /// <summary>
    /// 带参数的构造函数
    /// </summary>
    /// <param name="inputData">当前人物形象数据</param>
    /// <param name="isNewUser">是否为新手用户, UI不一致</param>
    public AvatarViewModel(CharacterData tempData, bool isNewUser = false, AvatarMenuType menuType = AvatarMenuType.Hair, AvatarSubType? defaultPart = null)
    {
        InputData = tempData;
        IsNewUser = isNewUser;
        DefaultMenuType = menuType;
        DefaultPart = defaultPart;
    }

    /// <summary>
    /// 形象编辑完成
    /// </summary>
    public Action<CharacterData> ImageEditFinshAction;

    public void EditFinsh(CharacterData data)
    {
        ImageEditFinshAction?.Invoke(data);
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public bool isNewUser()
    {
        return IsNewUser;
    }

    public void SetOcInfo(string ocCover, string avatarJson, Action<bool> resultHandler = null)
    {
        AvatarOcInfo info = new AvatarOcInfo()
        {
            ocCover = ocCover,
            avatarJson = avatarJson,
        };
        
        AvatarOcSetReq req = new AvatarOcSetReq
        {
            setType = 0,
            ocInfo = info
        };

        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.setOc,
            HttpMethod.POST, 
            JsonConvert.SerializeObject(req),
            onReceive: arg0 =>
            {
                resultHandler?.Invoke(true);
            }, onFail: arg0 =>
            {
                resultHandler?.Invoke(false);
            });
    }
    
    public void DeleteOcInfo(AvatarOcInfo ocInfo,  Action<bool> resultHandler = null)
    {
        AvatarOcSetReq req = new AvatarOcSetReq
        {
            setType = 1,
            ocInfo = ocInfo
        };

        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.setOc,
            HttpMethod.POST, 
            JsonConvert.SerializeObject(req),
            onReceive: arg0 =>
            {
                resultHandler?.Invoke(true);
            }, onFail: arg0 =>
            {
                LoggerUtils.LogError("删除设子失败 : " + arg0);
                resultHandler?.Invoke(false);
            });
    }
}
