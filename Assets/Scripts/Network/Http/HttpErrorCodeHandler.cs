using System;
using System.Collections.Generic;
using UIAgent;
using Newtonsoft.Json.Linq;

namespace Network.Http
{
    public class HttpErrorCodeHandler
    {
        private List<HttpErrorCodeBasic> _errorCodeBasics;

        public HttpErrorCodeHandler()
        {
            _errorCodeBasics = new List<HttpErrorCodeBasic>
            {
                new HttpErrorCodeTip()
                {
                    ErrorCodeRange = new Tuple<int, int>(0, 1000),
                },
                new HttpErrorCodeTip()
                {
                    ErrorCodeRange = new Tuple<int, int>(10001, 99999),
                    ForceTip = "服务器发生了错误，请重试",
                },
                new HttpErrorCodeForceExit()
                {
                    ErrorCodeRange = new Tuple<int, int>(-200000, -200000),
                }
            };
        }

        public bool HandleErrorCodeResult(HttpResponseRawData responseRawData)
        {
            var result = responseRawData.result;
            if (result is 0 or -1)
            {
                return false;
            }

            foreach (var eCodeBasic in _errorCodeBasics)
            {
                var min = eCodeBasic.ErrorCodeRange.Item1;
                var max = eCodeBasic.ErrorCodeRange.Item2;

                if (result >= min && result <= max)
                {
                    if (eCodeBasic is HttpErrorCodeTip tipHandler)
                    {
                        var showTip = string.IsNullOrEmpty(tipHandler.ForceTip) ? responseRawData.rmsg : tipHandler.ForceTip;
                        if (!string.IsNullOrEmpty(showTip))
                        {
                            var data = (JObject)responseRawData.data;
                            if (data != null && data["isPopup"] != null && data["isPopup"].Value<int>() == 1)
                            {
                                UIAgentManager.Inst.OpenPanel(PanelId.CommonSingleConfirmPanel_Style2, new CommonSingleConfirmPanel_Style2Data()
                                {
                                    CanClose = true,
                                    ConfirmString = "确定",
                                    ContextString = showTip,
                                    TopTitleString = "提示",
                                });
                            }
                            else
                            {
                                UIAgentManager.Inst.ShowToast(showTip);
                            }
                        }
                    }
                    else
                    {
                        eCodeBasic.DoError(responseRawData.rmsg);
                    }
                    return true;
                }
            }

            return false;
        }
    }

    #region HttpErrorCodeTip实现

    internal class HttpErrorCodeForceExit : HttpErrorCodeBasic
    {
        public override void DoError(string errorMsg)
        {
            //todo: -200000错误码时退回登陆页
        }
    }

    internal class HttpErrorCodeTip : HttpErrorCodeBasic
    {
        public string ForceTip;

        public override void DoError(string errorMsg)
        {
            var showTip = string.IsNullOrEmpty(ForceTip) ? errorMsg : ForceTip;
            if (!string.IsNullOrEmpty(showTip))
            {
                UIAgentManager.Inst.ShowToast(showTip);
            }
        }
    }

    internal abstract class HttpErrorCodeBasic
    {
        public Tuple<int, int> ErrorCodeRange;

        public abstract void DoError(string errorMsg);
    }

    #endregion
}
