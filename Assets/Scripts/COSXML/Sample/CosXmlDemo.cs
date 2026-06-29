using System;
using UnityEngine;

namespace Game.COSXML.Sample
{
    public class CosXmlDemo : MonoBehaviour
    {

        private void Start()
        {

            CosXmlUploadManager.UploadFile("HotUpdate/UIWindow.json", "Assets/Resources/Configs/UIWindow.json", (remotePath, err) =>
            {
                Debug.Log("remotePath:" + remotePath + " err:" + err);
            });
        }

    }

}
