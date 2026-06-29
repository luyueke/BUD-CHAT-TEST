using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Sirenix.OdinInspector;

public class InputFieldTextScroller : MonoBehaviour
{
    public MyInputField inputField;
    private RectTransform textRectTransform;
    private Vector2 originalTextPosition;

    public string focusText = "";
    
    void Start()
    {
        if (inputField.textComponent != null)
        {
            textRectTransform = inputField.textComponent.GetComponent<RectTransform>();
            originalTextPosition = textRectTransform.anchoredPosition;
            
            inputField.onValueChanged.AddListener(OnValueChanged);
        }
    }
    
    // 定位到特定文本
    public void FocusOnText(string searchText)
    {
        if (string.IsNullOrEmpty(searchText) || inputField.textComponent == null)
            return;
            
        int foundIndex = inputField.text.IndexOf(searchText);
        if (foundIndex >= 0)
        {
            FocusOnCharIndex(foundIndex);
        }
    }
    
    // 定位到字符索引
    public void FocusOnCharIndex(int charIndex)
    {
        StartCoroutine(FocusOnCharIndexCoroutine(charIndex));
    }
    
    private IEnumerator FocusOnCharIndexCoroutine(int charIndex)
    {
        yield return null;
        
        charIndex = Mathf.Clamp(charIndex, 0, inputField.text.Length);
        
        TextGenerator textGenerator = inputField.textComponent.cachedTextGenerator;
        textGenerator.Invalidate();
        
        // 计算字符所在位置
        UICharInfo charInfo = textGenerator.characters[charIndex];
        float charPositionY = charInfo.cursorPos.y;
        
        // 调整文本位置使该字符可见
        RectTransform viewportRect = inputField.textComponent.rectTransform;
        float viewportHeight = viewportRect.rect.height;
        
        float targetY = -charPositionY + viewportHeight * 0.5f;
        
        // 限制在有效范围内
        float contentHeight = textGenerator.GetPreferredHeight(
            inputField.text, 
            inputField.textComponent.GetGenerationSettings(viewportRect.rect.size)
        );
        
        targetY = Mathf.Clamp(targetY, -Mathf.Max(0, contentHeight - viewportHeight), 0);
        
        textRectTransform.anchoredPosition = new Vector2(
            originalTextPosition.x,
            targetY
        );
    }
    
    private void OnValueChanged(string newValue)
    {
        // 文本变化时自动调整位置（可选）
        // AutoAdjustPosition();
    }
    
    // 自动调整位置确保光标可见
    private void AutoAdjustPosition()
    {
        if (inputField.isFocused)
        {
            StartCoroutine(AdjustForCaretCoroutine());
        }
    }
    
    private IEnumerator AdjustForCaretCoroutine()
    {
        yield return null;
        
        int caretPosition = inputField.caretPosition;
        FocusOnCharIndex(caretPosition);
    }


    [Button("测试")]
    void Test()
    {
        FocusOnText(focusText);
    }
}