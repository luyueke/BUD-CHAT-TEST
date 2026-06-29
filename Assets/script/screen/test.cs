using System.Collections;
using System.Collections.Generic;
using System.Runtime.Versioning;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class test : MonoBehaviour


{
    public Button button;
    // Start is called before the first frame update
    void Start()
    {
        string savePath = Path.Combine(Application.persistentDataPath, "1.0.4_xxx.apk");
        Debug.LogError(savePath);
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        button.onClick.AddListener(OnButtonClick);
    }

    void OnButtonClick()
    {
        var asset = Resources.Load("AICompanionChatPanel");
        var go = GameObject.Instantiate(asset, gameObject.transform);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
