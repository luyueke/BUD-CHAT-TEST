using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UI.Base;
using Com.TheFallenGames.OSA.Util.IO;

public class RemoteIconBgPanel : MonoBehaviour
{
    [SerializeField] private Image bgImg;
    public RemoteImageBehaviour icon;
    public string[] urls;
    private Vector3 start = new Vector3(-2048, 1024);
    private int rowNum = 10;
    private int lineNum = 8;
    private int xDistance = 250;
    private int yDistance = 40;
    private float offset = 200;
    private List<RemoteImageBehaviour> imgs = new List<RemoteImageBehaviour>();

    protected void Awake()
    {

    }

    public void HideSolidBg()
    {
        if (bgImg == null)
        {
            return;
        }
        bgImg.color = Color.clear;
    }
    
    public void InitCustomBgItem(string bgColor, List<string> bgRemoteSpriteIds)
    {
        if (!string.IsNullOrEmpty(bgColor))
        {
            bgImg.color = DataUtil.DeSerializeColorCheckHash(bgColor);
        }
        
        urls = bgRemoteSpriteIds.ToArray();
        if (urls.Length == 0)
        {
            return;
        }

        bgImg.transform.ClearChildren();
        transform.eulerAngles = new Vector3(0, 0, 15);

        int index = 0;
        float currentPositionX = 0;
        float currentPositionY = 256;
        for (int i = 0; i < lineNum * 2; i++)
        {
            for (int j = 0; j < rowNum; j++)
            {
                var img = Instantiate(icon, bgImg.transform);
                var obj = img.gameObject;
                obj.transform.localEulerAngles = Vector3.zero;
                obj.transform.localScale = Vector3.one;
                img.Load(urls[index], onCanceled: () =>
                {
                    img.RawImage.SetNativeSize();
                });
                currentPositionX += 256 / 2;
                obj.transform.localPosition = new Vector3(currentPositionX, -(currentPositionY + yDistance) * i);
                obj.transform.localPosition += start;
                currentPositionX += 256 / 2 + xDistance;
                index++;
                index %= urls.Length;
                imgs.Add(img);
            }

            if (i % 2 == 0)
            {
                currentPositionX = offset;
            }
            else
            {
                currentPositionX = 0;
            }
        }
    }
}