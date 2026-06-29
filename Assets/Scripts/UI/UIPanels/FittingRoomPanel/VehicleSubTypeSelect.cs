using System;
using GameData.PgcData;
using UnityEngine;
using UnityEngine.UI;

public class VehicleSubTypeSelect : MonoBehaviour{
    [SerializeField] private Button rootButton;
    [SerializeField] private Text rootText;
    [SerializeField] private Image rootArrow;
    [SerializeField] private Button option1;
    [SerializeField] private Text option1Text;
    [SerializeField] private Button option2;
    [SerializeField] private Text option2Text;

    private bool isOpen;

    public Action<bool> OnSelectedAction;

    private void Awake()
    {
        rootButton.onClick.AddListener(OnClick);
        option1.onClick.AddListener(() => OnSelected(false));

        Enum.TryParse(option2.name, out VehicleSubType result1);
        option2.onClick.AddListener(() => OnSelected(true));

        rootText.text = option2Text.text;
    }

    private void OnSelected(bool isSingle)
    {
        OnSelectedAction?.Invoke(isSingle);
        rootText.text = isSingle ? option2Text.text : option1Text.text;
        OnClick();
    }

    private void OnClick()
    {
        isOpen = !isOpen;
        if (isOpen)
        {
            option1.gameObject.SetActive(true);
            option2.gameObject.SetActive(true);
            rootArrow.transform.localEulerAngles = Vector3.zero;
        }
        else
        {
            option1.gameObject.SetActive(false);
            option2.gameObject.SetActive(false);
            rootArrow.transform.localEulerAngles = new Vector3(0, 0, -90);
        }
    }

    public void DefaultOn(bool isSingle)
    {
        OnSelectedAction?.Invoke(isSingle);
        rootText.text = isSingle ? option2Text.text : option1Text.text;
    }
}