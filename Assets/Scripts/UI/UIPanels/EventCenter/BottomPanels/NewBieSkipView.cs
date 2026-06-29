using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NewBieSkipView : MonoBehaviour
{
    public GameObject skipItem;
    public Transform content;
    public void SetData(List<string> items , Transform target , Vec3  DelayPosition = null)
    {
        if (DelayPosition != null)
        {
            transform.position = target.transform.position + DelayPosition;
        }
        else
        {
            transform.position = target.transform.position;
        }
        
        foreach (Transform child in content) Destroy(child.gameObject);
        foreach (var reward in items)
        {
            var item = reward.Split(",");
            // 克隆预制体
            GameObject cloneItem = GameObject.Instantiate(skipItem, content);
            Button clickBtn = cloneItem.transform.Find("Button").GetComponent<Button>();
            clickBtn.onClick.RemoveAllListeners();
            
            clickBtn.onClick.AddListener(() =>
            {
                NewbieTaskSkipManager.Inst.HandleSkip(item[0]);
            });
            cloneItem.transform.Find("Title").GetComponent<Text>().text = item[1];
            // 激活物体
            cloneItem.SetActive(true);
        }

    }
}
