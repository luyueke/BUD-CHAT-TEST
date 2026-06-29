
public class VipDataManager : GlobalInstance<VipDataManager>
{
    
    public bool isVip = false;
    public bool isSVip = false;
    
    public void UpdateVipStatus()
    {
        IAPDataManager.Inst.GetSubscribeStatus((b, subscribeStatusResponse) =>
        {
            if (subscribeStatusResponse == null)
            {
                return;
            }

            isVip = false;
            isSVip = false;
            var vipType = subscribeStatusResponse.vipType;
            switch (vipType)
            {
                case (int)SubscribeVipType.MonthCard:
                    isVip = true;
                    break;
                case (int)SubscribeVipType.YearCard:
                    isVip = true;
                    isSVip = true;
                    break;
            }
        });
    }

    public void SetVipStatus(int vipType)
    {
        switch (vipType)
        {
            case (int)SubscribeVipType.MonthCard:
                isVip = true;
                break;
            case (int)SubscribeVipType.YearCard:
                isVip = true;
                isSVip = true;
                break;
        }
    }
}
