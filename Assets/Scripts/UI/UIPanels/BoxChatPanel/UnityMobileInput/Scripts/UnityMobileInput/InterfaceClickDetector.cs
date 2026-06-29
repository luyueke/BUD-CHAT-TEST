using Message;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InterfaceClickDetector : MonoBehaviour
{
    GraphicRaycaster raycaster;

    void Awake()
    {
        raycaster = GameObject.Find("UIRoot/Canvas").GetComponent<GraphicRaycaster>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (IsClickOnClickableUI())
            {
                Debug.Log("点击到了自己");
            }
            else
            {
                Debug.Log("点击在空区域！");
                MessageHelper.Broadcast(MessageName.ClickEmpty);
            }
        }
    }

    bool IsClickOnClickableUI()
    {
        PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
        pointerEventData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();

        raycaster.Raycast(pointerEventData, results);

        foreach (var result in results)
        {
            //Debug.LogError(result.gameObject.name);
            if (result.gameObject == this.gameObject)
            {
                return true;
            }
        }

        return false;
    }
}