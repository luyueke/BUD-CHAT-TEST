using System;
using System.Collections;
using System.Collections.Generic;
using Es;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UGCEditor
{
    public class MainUGCResCopyPanel : MonoBehaviour
    {
        public Button backBtn;
        public Button copyBtn;
        public Button doneBtn;
        public Transform mask;
        public RectTransform empty;
        private RawImage emptyImage;
        public InteractorCopyEdit interactor;
        public CopySelectPartPanel SelectPartPanel;
        private float _gridOffset;
        private float _gridCount;
        public Action<int[]> _OnCopy;
        public Action _OnBack;
        public Action<int> _OnPartSelect;
        public Action<int[], int> _OnDone;
        private Texture2D screenShot;
        public bool isCharacter;
        private void Awake()
        {
            backBtn.onClick.AddListener(Hide);
            copyBtn.onClick.AddListener(GetCopyInfo);
            doneBtn.onClick.AddListener(OnDoneClick);
        }

        public void Show(float gridOffset, int gridCount, List<RenderTexture> renderTextures, List<UgcPartData> ugcDatas)
        {
            
            gameObject.SetActive(true);
            _gridOffset = gridOffset;
            _gridCount = gridCount;
            SelectPartPanel.RenderTextures = renderTextures;
            SelectPartPanel.ugcDatas = ugcDatas;
            SelectPartPanel.OnPartSelect = SelectPart;
            SelectPartPanel._OnSelectReturn = OnSelectReturn;
            SelectPartPanel.isCharacter = isCharacter;
            empty.sizeDelta = new Vector2(_gridOffset * 4, _gridOffset * 4);
            empty.localPosition = Vector3.zero;
            empty.transform.eulerAngles = Vector3.zero;
            emptyImage = empty.GetComponent<RawImage>();
            interactor.transform.SetParent(mask.parent);
            interactor.transform.SetAsLastSibling();
            interactor.transform.localScale = Vector3.one;
            interactor.transform.localPosition = Vector3.one;
            interactor.transform.eulerAngles = Vector3.zero;
            interactor.ignoreCheck = false;
            interactor.Settup(empty, null, null, _gridOffset);
            interactor.ShowDetailTransform();
            interactor.SetConersShow(true);
            interactor.SetRotateShow(false);
            doneBtn.gameObject.SetActive(false);
            copyBtn.gameObject.SetActive(true);
            emptyImage.color = new Color(1, 1, 1, 0f);
        }

        public void Hide()
        {
            _OnBack?.Invoke();
            Destroy(screenShot);
            screenShot = null;
            gameObject.SetActive(false);

        }


        private void GetCopyInfo()
        {
            _OnCopy?.Invoke(GetPointIndexInfo());
            //防止截图截到四个点
            interactor.SetConersShow(false);
            StartCoroutine(Takeshot());
        }

        public IEnumerator Takeshot()
        {
            yield return new WaitForEndOfFrame();
            Canvas canvas = this.GetComponentInParent<Canvas>();
            ScreenSpaceConvert convert = new ScreenSpaceConvert(canvas);
            var screenShotRec = convert.FindCopyScreenRect(interactor.recTrans);
            screenShot = new Texture2D((int) screenShotRec.width, (int) screenShotRec.height, TextureFormat.RGB24,
                false);
            screenShot.ReadPixels(screenShotRec, 0, 0);
            screenShot.Apply();

            SelectPartPanel.Show();


        }

        private int[] GetPointIndexInfo()
        {
            Vector3[] lrConer = interactor.GetPointPos();
            Vector3 ltPos = mask.parent.InverseTransformPoint(lrConer[0]);
            Vector3 rdPos = mask.parent.InverseTransformPoint(lrConer[1]);
            int changeOffset = (int) (_gridCount * 0.5f);
            int xmin = GetPosInMap(ltPos.x) + changeOffset;
            int xmax = GetPosInMap(rdPos.x) + changeOffset - 1;
            int ymin = GetPosInMap(rdPos.y) + changeOffset;
            int ymax = GetPosInMap(ltPos.y) + changeOffset - 1;
            return new[] {xmin, xmax, ymin, ymax};
        }

        public int CheckIndexOut(int x)
        {
            if (x < 0)
            {
                return 0;
            }

            if (x >= _gridCount)
            {
                return (int) _gridCount - 1;
            }

            return x;
        }

        public void OnDoneClick()
        {
            _OnDone?.Invoke(GetPointIndexInfo(), (int) interactor.recTrans.eulerAngles.z);
            Hide();
        }

        private int GetPosInMap(float dis)
        {
            int x = (int) (dis / _gridOffset);
            if (dis % _gridOffset > _gridOffset * 0.8f || dis % _gridOffset < -_gridOffset * 0.8f)
            {
                if (dis > 0)
                {
                    x++;
                }
                else
                {
                    x--;
                }
            }

            return x;
        }

        //部位选择
        public void SelectPart(int id)
        {
            _OnPartSelect?.Invoke(id);
            SelectPartPanel.Hide();
            doneBtn.gameObject.SetActive(true);
            copyBtn.gameObject.SetActive(false);
            interactor.SetConersShow(false);
            interactor.SetRotateShow(true);
            if (screenShot != null)
            {
                emptyImage.texture = screenShot;
            }

            interactor.ignoreCheck = true;
            emptyImage.color = new Color(1, 1, 1, 0.5f);
        }

        public void OnSelectReturn()
        {
            interactor.SetConersShow(true);
        }
    }
}