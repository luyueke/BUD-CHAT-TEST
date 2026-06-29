using Com.TheFallenGames.OSA.Util.IO;
using System.Collections;
using System.Collections.Generic;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class SwitchMS : MonoBehaviour
{
    [SerializeField] private RemoteImageBehaviour RemoteImage;
    [SerializeField] private Image IconImage;
    [SerializeField] private Button SelectedButton;
    [SerializeField] private Sprite DefaultSprite;

    private string MSId;

    public void Awake()
    {
        SelectedButton.onClick.AddListener(OnSwitchButton);
    }

    public void OnSwitchButton()
    {
        UIManager.Inst.OpenPanel(PanelId.MusicScoreBagSubPanel, MSId);
    }

    public void SetTarget(string id, string url)
    {
        MSId = id;
        RemoteImage.gameObject.SetActive(false);
        IconImage.gameObject.SetActive(false);
        if (string.IsNullOrEmpty(id))
        {
            IconImage.gameObject.SetActive(true);
            IconImage.sprite = DefaultSprite;
            return;
        }

        if (string.IsNullOrEmpty(url))
        {
            IconImage.gameObject.SetActive(true);
            IconImage.sprite = DefaultSprite;
        }
        else
        {
            RemoteImage.gameObject.SetActive(true);
            RemoteImage.Load(url);
        }
    }
}
