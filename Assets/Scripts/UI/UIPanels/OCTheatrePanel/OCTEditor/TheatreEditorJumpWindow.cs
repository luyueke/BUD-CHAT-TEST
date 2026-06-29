using System;
using Pb.Theatre;
using UnityEngine;
using UnityEngine.UI;

public class TheatreEditorJumpWindow : MonoBehaviour
{
    [SerializeField] private Text title;
    [SerializeField] private Button closeBtn;
    [SerializeField] private Button confirmBtn;
    [SerializeField] private RectTransform jumpWindowContent;

    private Action onJump;

    public void Show(POCTheatreSection targetSection, Action onJumpAction)
    {
        onJump = onJumpAction;

        if (title != null)
            title.text = TheatreEditorJumpSectionInput.GetSectionFirstText(targetSection);

        closeBtn?.onClick.RemoveAllListeners();
        closeBtn?.onClick.AddListener(Hide);

        confirmBtn?.onClick.RemoveAllListeners();
        confirmBtn?.onClick.AddListener(() =>
        {
            onJump?.Invoke();
            Hide();
        });

        PositionContent();
        gameObject.SetActive(true);
    }

    private void PositionContent()
    {
        if (jumpWindowContent == null) return;

        bool showBelow = Input.mousePosition.y > Screen.height * 0.5f;
        var parentRT = jumpWindowContent.parent as RectTransform;
        if (parentRT == null) return;

        var canvas = GetComponentInParent<Canvas>();
        Camera uiCam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? canvas.worldCamera : null;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRT, Input.mousePosition, uiCam, out Vector2 localPoint)) return;

        float halfH = jumpWindowContent.rect.height * 0.5f;
        float yPos = showBelow ? localPoint.y - halfH - 40f : localPoint.y + halfH + 40f;
        jumpWindowContent.anchoredPosition = new Vector2(jumpWindowContent.anchoredPosition.x, yPos);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
}
