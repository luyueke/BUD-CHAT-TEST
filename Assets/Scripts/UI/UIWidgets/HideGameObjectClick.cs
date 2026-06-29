using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using System;

/// <summary>
/// 如果点击的不是 go ,则隐藏 go,
/// 不影响其它界面的事件响应
/// </summary>
public class HideGameObjectClick : MonoBehaviour
{
    public GameObject go;
    public Action<GameObject> cb;

    public List<GameObject> exCludeGos;

    private GameObject currentRaycastGameObject = null;
    private List<RaycastResult> raycastResults = new List<RaycastResult>();
    private PointerEventData pointData;

    private void Update()
    {
        if (go == null)
        {
            return;
        }

        if (go.activeInHierarchy)
        {
            if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
            {
                if (EventSystem.current != null)
                {
                    var isTouch = false;
                    if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
                    {
                        isTouch = Input.touchCount > 0 && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
                    }
                    else
                    {
                        isTouch = EventSystem.current.IsPointerOverGameObject();
                    }

                    if (isTouch)
                    {
                        if (pointData == null)
                        {
                            pointData = new PointerEventData(EventSystem.current);
                        }

                        pointData.position = Input.mousePosition;
                        raycastResults.Clear();
                        EventSystem.current.RaycastAll(pointData, raycastResults);
                        currentRaycastGameObject = null;
                        if (raycastResults.Count > 0)
                        {
                            currentRaycastGameObject = raycastResults[0].gameObject;
                        }

                        if (currentRaycastGameObject == go.gameObject || currentRaycastGameObject.transform.IsChildOf(go.transform) || (exCludeGos != null && exCludeGos.Contains(currentRaycastGameObject)))
                        {
                        }
                        else
                        {
                            go.SetActive(false);
                            cb?.Invoke(go);
                        }
                    }
                    else
                    {
                        go.SetActive(false);
                        cb?.Invoke(go);
                    }
                }
            }
        }
    }
}
