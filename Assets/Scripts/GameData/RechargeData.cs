using System.Collections.Generic;


public class ExchangeReq
{
    public int fromCurrency;
    public int toCurrency;
    public int exchangeNum;
}

public class ExchangeResponse
{
    public int toBalance;//兑换后 目标余额
    public int fromBalance;//兑换后 余额
}
