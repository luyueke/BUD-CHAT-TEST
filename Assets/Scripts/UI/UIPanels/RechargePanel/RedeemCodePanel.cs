using System.Collections.Generic;
using Basic.Utils;
using Com.TheFallenGames.OSA.Util.IO;
using Game.Store;
using GameData.PgcData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine.UI;

public class RedeemCodePanel : BasePanel<RedeemCodePanel>
{
    public CButton BackBtn;
    public CButton SaveBtn;
    public Image SaveBtnBg;

    public CText idInputTxt;
    public CButton idInputBtn;

    private string idInput = "";

    public override void OnCreate()
    {
        SaveBtn.onClick.AddListener(OnConfirmClick);
        BackBtn.onClick.AddListener(OnBackBtnClick);

        idInputBtn.onClick.AddListener(() => { OnInputIdClick(); });
    }

    private void OnConfirmClick()
    {
        if (string.IsNullOrEmpty(idInput))
        {
            TipPanel.ShowToast("请输入5位兑换码");
            return;
        }

        RedeemClaimReq redeemClaimReq = new RedeemClaimReq();
        redeemClaimReq.redeemCode = idInput;
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.redeemClaim,
            HttpMethod.GET,
            JsonConvert.SerializeObject(redeemClaimReq),
            onReceive: msg =>
            {
                if (string.IsNullOrEmpty(msg))
                {
                    LoggerUtils.LogError("响应数据为空");
                    return;
                }

                RedeemClaimResponse redeemClaimResponse = JsonConvert.DeserializeObject<RedeemClaimResponse>(msg);

                if (redeemClaimResponse == null)
                {
                    LoggerUtils.LogError("响应数据解析失败");
                    return;
                }

                #region 特殊奖励
                // 检查是否为特殊奖励（头像框、主页皮肤或聊天气泡）
                if (IsSpecialRedeem(redeemClaimResponse))
                {   
                    // 创建特殊奖励数据列表
                    var spTaskRewardDatas = new List<CommonRewardItemData>();
                    // 创建单个奖励数据对象
                    var rewardData = new CommonRewardItemData();
                    // 打开通用奖励展示面板
                    var spRewardPanel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
                    // 特殊奖励数量固定为1
                    rewardData.RewardAmount = 1;
                    // 处理头像框奖励
                    if (redeemClaimResponse.avatarFrame != 0)
                    {
                        // 获取头像框数据（包含名称和图片）
                        var headCycleData = UserUIWidgetManager.Inst.GetHeadCycleData(redeemClaimResponse.avatarFrame, this.gameObject);
                        // 设置奖励名称
                        rewardData.rewardName = headCycleData.Name;
                        // 设置奖励图标
                        rewardData.IconSp = headCycleData.Sp_PreviewCycle;
                    }

                    // 处理主页皮肤奖励
                    if (redeemClaimResponse.homepageSkin != 0)
                    {
                        // 获取主页皮肤数据（包含图片）
                        rewardData.IconSp = ProfileThemeManager.Inst.LoadThemeIcon(redeemClaimResponse.homepageSkin, gameObject);
                    }

                    // 处理聊天气泡奖励
                    if (redeemClaimResponse.chatBubbles != 0)
                    {
                        // 获取聊天气泡数据（包含图片）
                        rewardData.IconSp = UserUIWidgetManager.Inst.GetChoosePanelBubbleBg(redeemClaimResponse.chatBubbles, this.gameObject);
                    }

                    // 处理昵称框奖励
                    if (redeemClaimResponse.nicknameFrame != 0)
                    {
                        // 获取数据（包含图片）
                        rewardData.IconSp = UserUIWidgetManager.Inst.GetNicknameBg(redeemClaimResponse.nicknameFrame, this.gameObject);
                    }

                    // 添加奖励数据到列表
                    spTaskRewardDatas.Add(rewardData);
                    // 显示奖励列表
                    spRewardPanel.ShowRewards(spTaskRewardDatas);
                    // 关闭当前面板
                    CloseSelf();
                    //刷新UserInfo
                    AccountDataManager.Inst.RefreshUserInfo();
                    return;
                }
                #endregion

                #region ugc奖励
                if (IsUgcRedeem(redeemClaimResponse))
                {   
                    try
                    {
                        // 验证UGC数据完整性
                        if (redeemClaimResponse.ugcInfo == null)
                        {
                            LoggerUtils.LogError("UGC信息为空");
                            TipPanel.ShowToast("UGC商品数据不完整");
                            return;
                        }

                        if (string.IsNullOrEmpty(redeemClaimResponse.ugcInfo.ugcName))
                        {
                            LoggerUtils.LogError("UGC名称为空");
                            TipPanel.ShowToast("UGC商品名称不完整");
                            return;
                        }

                        // 创建特殊奖励数据列表
                        var spTaskRewardDatas = new List<CommonRewardItemData>();
                        // 创建单个奖励数据对象
                        var rewardData = new CommonRewardItemData();
                        
                        // 打开通用奖励展示面板
                        var spRewardPanel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
                        if (spRewardPanel == null)
                        {
                            return;
                        }

                        //设置数量
                        rewardData.RewardAmount = 1;

                        //设置类型 
                        rewardData.rewardType = redeemClaimResponse.rewardType;

                        // 设置奖励名称
                        rewardData.rewardName = redeemClaimResponse.ugcInfo.ugcName;

                        // 加载ugc封面图片
                        rewardData.UgcCover = redeemClaimResponse.ugcInfo.ugcCover;

                        // 添加奖励数据到列表
                        spTaskRewardDatas.Add(rewardData);
                        
                        // 显示奖励列表
                        spRewardPanel.ShowRewards(spTaskRewardDatas);
                        
                        // 关闭当前面板
                        CloseSelf();
                    }
                    catch (System.Exception e)
                    {
                        LoggerUtils.LogError($"处理UGC奖励时发生错误: {e}");
                    }
                    return;
                }
                #endregion

                // 从服务器响应中获取普通奖励列表和背包数据
                List<RedeemClaimRewardsItem> redeemClaimRewardsItems = redeemClaimResponse.rewards;
                var pairList = redeemClaimResponse.backPackData?.pairList;
                // 如果两种奖励都为空，则直接返回
                if ((redeemClaimRewardsItems == null || redeemClaimRewardsItems.Count <= 0) && (pairList == null || pairList.Count <= 0))
                {
                    return;
                }
                // 创建用于显示的奖励数据列表
                var taskRewardDatas = new List<CommonRewardItemData>();

                // 打开通用奖励展示面板
                var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
                
                // 处理普通奖励（如货币、道具等）
                if (redeemClaimRewardsItems != null)
                {
                    foreach (var redeemClaimRewardsItem in redeemClaimRewardsItems)
                    {
                        // 获取奖励类型和数量
                        int rewardType = redeemClaimRewardsItem.rewardType;
                        int rewardCount = redeemClaimRewardsItem.count;
                        // 添加奖励显示数据
                        taskRewardDatas.Add(new CommonRewardItemData()
                        {
                            rewardType = rewardType,
                            RewardAmount = rewardCount,
                            // 获取奖励名称
                            rewardName = PgcUtils.GetRewardName((BUDRewardType)rewardType),
                            // 加载奖励图标
                            IconSp = PgcUtils.LoadCurrencyIcon((int)GameUtils.ConvertRewardType(rewardType), gameObject)
                        });
                    }
                }

                // 处理背包物品奖励（如装扮、宠物等）
                if (pairList != null)
                {
                    // 获取角色装扮和宠物装扮的数据处理器
                    var avatarDataHandler = AssetsDataManager.GetData<AvatarBUDSceneHandler>();
                    var petAvatarDataHandler = AssetsDataManager.GetData<PetAvatarBUDSceneHandler>();
                    foreach (var pairData in pairList)
                    {
                        if (pairData.list != null)
                        {
                            foreach (var opertionData in pairData.list)
                            {
                                // 尝试获取角色装扮数据
                                var assetData = avatarDataHandler.GetAssetsData(opertionData.id);
                                // 如果不是角色装扮，则尝试获取宠物装扮数据
                                if (assetData == null)
                                {
                                    assetData = petAvatarDataHandler.GetAssetsData(opertionData.id);
                                }
                                // 添加装扮奖励显示数据
                                taskRewardDatas.Add(new CommonRewardItemData()
                                {
                                    rewardType = pairData.dataType,
                                    // 加载装扮图标
                                    IconSp = PgcUtils.GetIconSpriteByPgcId(opertionData.id, panel.gameObject),
                                    rewardName = assetData?.Name,
                                    RewardAmount = 1,
                                });
                            }
                        }
                    }
                    // 广播消息更新背包数据
                    Message.MessageHelper.Broadcast(Message.MessageName.AvaterDatabaseCheck);
                }
                // 显示所有奖励
                panel.ShowRewards(taskRewardDatas);
                // 刷新账户余额信息
                AccountDataManager.Inst.BalanceInfo.Refresh();
                // 关闭兑换码面板
                CloseSelf();
            }, onFail: arg0 =>
            {
                // 处理请求失败的情况
                if (string.IsNullOrEmpty(arg0))
                {
                    return;
                }

                // 解析错误响应
                HttpResponseRawData httpResponseRawData = JsonConvert.DeserializeObject<HttpResponseRawData>(arg0);
                if (httpResponseRawData == null)
                {
                    return;
                }

                // 显示错误提示
                string rmsg = httpResponseRawData.rmsg;
                TipPanel.ShowToast(rmsg);
            });
    }

    private bool IsSpecialRedeem(RedeemClaimResponse redeemClaimResponse)
    {
        if (redeemClaimResponse.avatarFrame != 0 || redeemClaimResponse.homepageSkin != 0 || redeemClaimResponse.chatBubbles != 0 || redeemClaimResponse.nicknameFrame != 0)
        {
            return true;
        }
        return false;
    }
    //判断是否是ugc奖励
    private bool IsUgcRedeem(RedeemClaimResponse redeemClaimResponse)
    {
        
        if (redeemClaimResponse.rewardType == (int)BUDRewardType.RewardUgcResource)
        {
            return true;
        }

        return false;
    }

    private void OnBackBtnClick()
    {
        CloseSelf();
    }

    private void CloseSelf()
    {
        UIManager.Inst.ClosePanel(this);
    }

    private void OnInputIdClick()
    {
        KeyBoardInfo keyBoardInfo = new KeyBoardInfo
        {
            type = 0,
            placeHolder = $"{LocalizationManager.Inst.GetLocalizedText("请输入5位兑换码")}...",
            inputMode = 2,
            maxLength = 5,
            inputFlag = 0,
            textSecurity = 1,
            lengthTips = LocalizationManager.Inst.GetLocalizedText("字数超出限制"),
            defaultText = "",
            returnKeyType = (int)ReturnType.Send
        };
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, KeyboardReturn);
        MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(keyBoardInfo));
    }

    private void KeyboardReturn(string str)
    {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
        if (string.IsNullOrEmpty(str))
        {
            return;
        }

        if (str.Length < 5)
        {
            TipPanel.ShowToast("请输入5位兑换码");
            return;
        }

        idInput = str;
        idInputTxt.text = str;

        var spriteatlasPath = "Assets/Loadable/UI/UIPanel/CommonSprite/CommonSprite.spriteatlas";
        SaveBtnBg.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, "Yellow_Btn_3", gameObject);
    }
}
