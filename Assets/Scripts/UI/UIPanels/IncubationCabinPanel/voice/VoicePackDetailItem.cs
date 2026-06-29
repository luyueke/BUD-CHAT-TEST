using UnityEngine;
using UnityEngine.UI;
using System;
public class VoicePackDetailItem : MonoBehaviour
{
    public Text textName;
    public Button playBtn;
    public Button deleteBtn;
    public GameObject loadingGo;

    void Awake()
    {
        playBtn.onClick.AddListener(OnPlayBtnClick);
        deleteBtn.onClick.AddListener(OnDeleteBtnClick);
    }

    public void Init()
    {
        loadingGo.SetActive(false);
    }

    void OnPlayBtnClick()
    {
       
    }
    void OnDeleteBtnClick()
    {
       
    }
}
