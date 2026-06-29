using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;

public class H5ServiceItem : MonoBehaviour
{
	public Button termsOfService;
	public Button privacyPolicy;

	public void Start()
	{
		termsOfService.onClick.AddListener(OnClickTerms);
		privacyPolicy.onClick.AddListener(OnClickPrivacy);
	}

	private void OnClickTerms()
	{
		Open(BUDPrivacyPolicy.TermsOfServiceUrl_US);
	}

	private void OnClickPrivacy()
	{
		Open(BUDPrivacyPolicy.PrivacyPath_US);
	}

	private void Open(string path)
	{
		LoggerUtils.LogFormat($"$[WebView] open webView: {path}");
		var jb = new JObject
		{
			["url"] = path
		};
		MobileInterface.Instance.SendMessage(MobileInterfaceDefine.openWebview, JsonConvert.SerializeObject(jb));
	}
}
