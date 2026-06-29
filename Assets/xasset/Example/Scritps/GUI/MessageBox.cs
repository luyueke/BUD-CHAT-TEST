using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace xasset.example
{
    public class MessageBox : MonoBehaviour
    {
        public Button RetryBtn;
        public Button QuitBtn;
        public Text Txt_Title;
        public Text LeftText;
        public Text RightText;
        public const string Filename = "Assets/xasset/Example/Prefabs/MessageBox.prefab";
        public Action onExitGame;
        private static readonly Queue<MessageBox> Unused = new Queue<MessageBox>();


        private readonly Request _request = new Request();

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            RetryBtn.onClick.AddListener(OnClickYes);
            QuitBtn.onClick.AddListener(OnClickNo);
#if PACKAGE_TYPE_US
            if (LeftText != null)
            {
                LeftText.text = "Retry";
            }

            if (RightText != null)
            {
                RightText.text = "Quit";
            }
#endif
        }

        public void OnClickYes()
        {
            _request.SetResult(Request.Result.Success);
            Complete();
        }

        private void Complete()
        {
            gameObject.SetActive(false);
            Unused.Enqueue(this);
        }

        public void OnClickNo()
        {
            _request.Cancel();
            Complete();
            onExitGame?.Invoke();
        }

        private Request SendRequest()
        {
            gameObject.SetActive(true);
            _request.Reset();
            _request.SendRequest();
            return _request;
        }
        

        public static Request Show(MessageBox box, string title)
        {
            if (Unused.Count > 0)
            {
                var item = Unused.Dequeue();
                return item.SendRequest();
            }

            var canvas = GameObject.Find("Canvas");
            var go = Instantiate(box,canvas.transform);
            go.name = box.name;
            var messageBox = go.GetComponent<MessageBox>();
            messageBox.onExitGame = box.onExitGame;
            messageBox.Txt_Title.text = title;
            return messageBox.SendRequest();
        }
    }
}