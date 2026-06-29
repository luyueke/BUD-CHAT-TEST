using GameData.Base;

namespace UI {
    public abstract class UGCBaseEditData
    {
        public abstract UgcBaseInfo GetInfo();

        public CurrencyType currencyType = CurrencyType.PinkCoin;
    }
}
