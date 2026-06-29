using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine.EventSystems;

public class AdvancedInputFieldNavigator : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,IPointerMoveHandler
{
    public MyInputField inputField;

    public int currentLineNumber = 0;

    // 滑动检测相关变量
    private bool isDragging = false;
    private Vector2 dragStartPosition;
    private int dragStartLineNumber;
    private int lastJumpedLineNumber = -1;
    private float lineJumpThreshold = 30f; // 滑动多少像素触发行跳转

    void Awake()
    {
        inputField = GetComponent<MyInputField>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.LogError("OnPointerDown11");

        isDragging = true;
        dragStartPosition = eventData.position;
        dragStartLineNumber = GetCurrentLineNumber();
        lastJumpedLineNumber = dragStartLineNumber;
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (!isDragging || inputField == null || string.IsNullOrEmpty(inputField.text))
            return;

        // 计算垂直滑动距离
        float verticalDelta = eventData.position.y - dragStartPosition.y;
        
        int totalLines = GetTotalLineCount();
        
        if (totalLines <= 1)
            return; // 只有一行，不需要跳转

        // 根据滑动距离计算应该跳转的行数
        // 每滑动 lineJumpThreshold 像素就跳转一行
        int lineOffset = Mathf.RoundToInt(verticalDelta / lineJumpThreshold);
        
        // 计算目标行号（基于起始行号）
        // 向上滑动（Y值增加）-> 跳到下一行（行号增加）
        // 向下滑动（Y值减少）-> 跳到上一行（行号减少）
        int targetLine = dragStartLineNumber + lineOffset;
        
        // 限制在有效范围内
        targetLine = Mathf.Clamp(targetLine, 0, totalLines - 1);
        
        // 如果目标行与上次跳转的行不同，执行跳转
        if (targetLine != lastJumpedLineNumber)
        {
            lastJumpedLineNumber = targetLine;
            GoToLine(targetLine);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
        lastJumpedLineNumber = -1;
    }

  
    // 公开的方法供UI按钮调用
    public void GoToLine(int lineNumber)
    {
        Debug.LogError("GoToLine: " + lineNumber);
        StartCoroutine(GoToLineCoroutine(lineNumber));
    }

    public void GoToLineByInput(string lineNumberStr)
    {
        if (int.TryParse(lineNumberStr, out int lineNumber))
        {
            GoToLine(lineNumber - 1); // 转换为0-based索引
        }
    }

    public void FindAndGoToText(string searchText)
    {
        if (string.IsNullOrEmpty(searchText)) return;

        string[] lines = inputField.text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains(searchText))
            {
                GoToLine(i);
                break;
            }
        }
    }

    public void GoToBottom()
    {
        if (!string.IsNullOrEmpty(inputField.text))
        {
            inputField.caretPosition = inputField.text.Length;
            inputField.ForceLabelUpdate();
        }
    }

    public void GoToTop()
    {
        inputField.caretPosition = 0;
        inputField.ForceLabelUpdate();
    }

    private IEnumerator GoToLineCoroutine(int lineNumber)
    {
        yield return null;

        if (string.IsNullOrEmpty(inputField.text))
            yield break;

        string[] lines = inputField.text.Split('\n');
        lineNumber = Mathf.Clamp(lineNumber, 0, lines.Length - 1);

        // 计算字符索引
        int charIndex = CalculateCharIndexForLine(lineNumber);

        // 使用光标位置触发滚动
        bool wasFocused = inputField.isFocused;

        if (!wasFocused)
        {
            inputField.ActivateInputField();
            yield return null;
        }

        inputField.caretPosition = charIndex;
        inputField.selectionAnchorPosition = charIndex;
        inputField.selectionFocusPosition = charIndex;

        inputField.ForceLabelUpdate();

        if (!wasFocused)
        {
            yield return null;
            inputField.DeactivateInputField();
        }
    }

    private int CalculateCharIndexForLine(int lineNumber)
    {
        string[] lines = inputField.text.Split('\n');
        int charIndex = 0;

        for (int i = 0; i < lineNumber && i < lines.Length; i++)
        {
            charIndex += lines[i].Length + 1; // +1 for newline
        }

        return charIndex;
    }

    // 获取当前行号
    public int GetCurrentLineNumber()
    {
        if (string.IsNullOrEmpty(inputField.text))
            return 0;

        string textUpToCaret = inputField.text.Substring(0, inputField.caretPosition);
        string[] lines = textUpToCaret.Split('\n');
        return lines.Length - 1; // 0-based
    }

    // 获取总行数
    public int GetTotalLineCount()
    {
        if (string.IsNullOrEmpty(inputField.text))
            return 0;

        return inputField.text.Split('\n').Length;
    }

    [Button("测试跳转行")]
    void Test()
    {
        GoToLine(currentLineNumber);
    }

    [Button("测试跳转top")]
    void TestGoToTop()
    {
        GoToTop();
    }

    [Button("测试跳转bottom")]
    void TestGoToBottom()
    {
        GoToBottom();
    }
}