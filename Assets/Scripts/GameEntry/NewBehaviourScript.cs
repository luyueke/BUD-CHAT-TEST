using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using xasset;

public class NewBehaviourScript : MonoBehaviour
{
    public Button test1Btn;
    public Button testBtn;
    public Button test2Btn;
    public Text textText;
    // Start is called before the first frame update
    void Start()
    {

        // AkBankManager.LoadInitBank();
        // AkSoundEngine.AddBasePath(Application.persistentDataPath + "/Bundles/wwise/");
        // AkBankManager.LoadBank("Bud_Bgm_SoundBank", false, false);
        // // Debug.LogError($"Unity=== init = {init}  bank = {bank}");
        //
        //
        // testBtn.onClick.AddListener(() =>
        // {
        //     string musicEventName = "Bgm_Amusement_Park";
        //     string bgmEvent = "Play_Bgm_Loop";
        //     string bgmSwitch = "Bgm_Group";
        //     Debug.LogError("Unity==11111");
        //     AkSoundEngine.SetSwitch(bgmSwitch, musicEventName, this.gameObject);
        //     AkSoundEngine.PostEvent(bgmEvent, this.gameObject, (uint) AkCallbackType.AK_EndOfEvent, null,
        //         musicEventName);
        // });
        // test1Btn.onClick.AddListener(() =>
        // {
        //     Debug.LogError("Unity===22222222===");
        //     string musicEventName = "Bgm_Valley_Of_Awakening";
        //     string bgmEvent = "Play_Bgm_Loop";
        //     string bgmSwitch = "Bgm_Group";
        //     AkSoundEngine.SetSwitch(bgmSwitch, musicEventName, this.gameObject);
        //     AkSoundEngine.PostEvent(bgmEvent, this.gameObject, (uint) AkCallbackType.AK_EndOfEvent, null,
        //         musicEventName);
        // });
        // test2Btn.onClick.AddListener(() =>
        // {
        //     textText.text = "player Bgm_Final_Race";
        //     string musicEventName = "Bgm_Final_Race";
        //     string bgmEvent = "Play_Bgm_Loop";
        //     string bgmSwitch = "Bgm_Group";
        //     AkSoundEngine.SetSwitch(bgmSwitch, musicEventName, this.gameObject);
        //     AkSoundEngine.PostEvent(bgmEvent, this.gameObject, (uint) AkCallbackType.AK_EndOfEvent, null,
        //         musicEventName);
        // });
    }

    public static string GetFullPath(string BasePath, string RelativePath)
    {
        if (string.IsNullOrEmpty(BasePath))
            return "";

        var wrongSeparatorChar = System.IO.Path.DirectorySeparatorChar == '/' ? '\\' : '/';

        if (string.IsNullOrEmpty(RelativePath))
            return BasePath.Replace(wrongSeparatorChar, System.IO.Path.DirectorySeparatorChar);

        if (System.IO.Path.GetPathRoot(RelativePath) != "")
            return RelativePath.Replace(wrongSeparatorChar, System.IO.Path.DirectorySeparatorChar);
        Debug.LogError($"System.IO.Path.Combine(BasePath, RelativePath)======{System.IO.Path.Combine(BasePath, RelativePath)}");
        return System.IO.Path.GetFullPath(System.IO.Path.Combine(BasePath, RelativePath));
    }


    // Update is called once per frame

    void OnGUI()
    {
        if (GUI.Button(new Rect(100, 100, 300, 300), "222222222"))
        {

            Debug.LogError("3333333333");
        }
    }
}
