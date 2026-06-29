using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using Basic;

[RequireComponent(typeof(MyInputField))]
public class MultiLineInputFieldSelection : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("选中高亮设置")]
    public Color selectionColor = new Color(0.2f, 0.4f, 0.8f, 0.4f);
    public Color caretColor = Color.white;
    public float caretWidth = 2f;
    public float blinkInterval = 0.5f;

    [Header("触摸设置")]
    public float longPressDuration = 0.5f;
    public float doubleClickInterval = 0.3f;

    private MyInputField inputField;
    private Text textComponent;
    private RectTransform textRectTransform;
    private Canvas canvas;

    // 选择和高亮相关
    private List<GameObject> selectionHighlights = new List<GameObject>();
    private GameObject caretObject;
    private Coroutine blinkCoroutine;
    private bool isCaretVisible = true;
    private bool isFocused = false;

    // 选择状态
    private int selectionStart = -1;
    private int selectionEnd = -1;
    private bool isSelecting = false;
    private bool hasSelection = false;
    private Vector2 selectionStartPos;
    private Vector2 selectionEndPos;

    // 触摸相关
    private Vector2 touchStartPosition;
    private float touchStartTime;

    [HideInInspector] public bool canHighlight = true; //直接拖拽是没有高亮的,只有长按再拖拽才有高亮
    private bool beginDrag;
    private float dragStartTime;
    private bool isLongPress = false;
    private Coroutine longPressCoroutine;
    private int clickCount = 0;
    private float lastClickTime = 0f;

    // 文本测量
    private TextGenerator textGenerator;
    private TextGenerationSettings textSettings;

    private Camera uiCamera; //需要设置uicamera

    void Awake()
    {
        inputField = GetComponent<MyInputField>();
        textComponent = inputField.textComponent;
        textRectTransform = textComponent.GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        uiCamera = GlobalCameraManager.Inst.UICamera;
        InitializeTextGenerator();
        SetupEvents();
    }

    void Start()
    {
        CreateCaret();
        HideSystemSelection();
    }

    void Update()
    {
        if (isFocused)
        {
            UpdateCaretPosition();
            UpdateSelectionHighlight();
            HandleKeyboardInput();
        }
    }

    /// <summary>
    /// 初始化文本生成器
    /// </summary>
    private void InitializeTextGenerator()
    {
        textGenerator = new TextGenerator();
        UpdateTextGenerationSettings();
    }

    /// <summary>
    /// 更新文本生成设置
    /// </summary>
    private void UpdateTextGenerationSettings()
    {
        textSettings = textComponent.GetGenerationSettings(textRectTransform.rect.size);
        textSettings.generateOutOfBounds = false;
    }

    /// <summary>
    /// 设置事件
    /// </summary>
    private void SetupEvents()
    {
        inputField.onValueChanged.AddListener(OnTextChanged);

        // 添加事件触发器处理焦点
        var eventTrigger = gameObject.GetComponent<EventTrigger>();
        if (eventTrigger == null)
            eventTrigger = gameObject.AddComponent<EventTrigger>();

        // 选择事件
        var selectEntry = new EventTrigger.Entry();
        selectEntry.eventID = EventTriggerType.Select;
        selectEntry.callback.AddListener((data) => OnInputFieldSelected());
        eventTrigger.triggers.Add(selectEntry);

        // 取消选择事件
        var deselectEntry = new EventTrigger.Entry();
        deselectEntry.eventID = EventTriggerType.Deselect;
        deselectEntry.callback.AddListener((data) => OnInputFieldDeselected());
        eventTrigger.triggers.Add(deselectEntry);
    }

    /// <summary>
    /// 创建光标
    /// </summary>
    private void CreateCaret()
    {
        caretObject = new GameObject("CustomCaret");
        caretObject.transform.SetParent(textComponent.transform);
        caretObject.transform.localScale = Vector3.one;
        caretObject.transform.localScale = Vector3.zero;

        Image caretImage = caretObject.AddComponent<Image>();
        caretImage.color = caretColor;
        caretImage.raycastTarget = false;

        RectTransform rectTransform = caretObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.zero;
        rectTransform.pivot = new Vector2(0.5f, 0f);

        caretObject.SetActive(false);
    }

    /// <summary>
    /// 隐藏系统自带的选中效果
    /// </summary>
    private void HideSystemSelection()
    {
#if UNITY_EDITOR
        // try
        // {
        //     var selectionColorField = typeof(InputField).GetField("m_SelectionColor",
        //         System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        //     if (selectionColorField != null)
        //     {
        //         selectionColorField.SetValue(inputField, new Color(0, 0, 0, 0));
        //     }
        // }
        // catch (System.Exception e)
        // {
        //     Debug.LogWarning("无法隐藏系统选择颜色: " + e.Message);
        // }
#endif
    }

    /// <summary>
    /// 指针按下事件
    /// </summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isFocused)
        {
            inputField.Select();
            inputField.ActivateInputField();
        }
        beginDrag = false;

        touchStartPosition = eventData.position;
        touchStartTime = Time.time;

        // 处理双击/三击
        HandleMultipleClick();

        // 设置选择起始位置
        selectionStartPos = touchStartPosition;
        selectionEndPos = touchStartPosition;

        if (clickCount == 1)
        {
            // 单击 - 设置光标位置
            selectionStart = GetCharacterIndexFromPosition(eventData.position);
            // Debug.LogError("111selectionStart: " + selectionStart);
            selectionEnd = selectionStart;
            isSelecting = true;
            // longPressCoroutine = StartCoroutine(LongPressDetection());
        }
        else if (clickCount == 2)
        {
            // 双击 - 选择单词
            SelectWordAtPosition(eventData.position);
        }
        else if (clickCount >= 3)
        {
            // 三击 - 选择整行
            SelectLineAtPosition(eventData.position);
        }

        UpdateSelection();
    }

    /// <summary>
    /// 处理多次点击
    /// </summary>
    private void HandleMultipleClick()
    {
        clickCount = 1;
        return;
        //暂时屏蔽多次点击
        float currentTime = Time.time;
        if (currentTime - lastClickTime < doubleClickInterval)
        {
            clickCount++;
        }
        else
        {
            clickCount = 1;
        }
        lastClickTime = currentTime;
    }

    /// <summary>
    /// 拖拽事件
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (!beginDrag)
        {
            beginDrag = true;
            dragStartTime = Time.time;
            if (dragStartTime - touchStartTime > MyInputFieldConfig.longPressThreshold)
            {
                canHighlight = true;
            }
            else
            {
                canHighlight = false;
            }
        }

        if (!isSelecting) return;

        selectionEndPos = eventData.position;
        selectionEnd = GetCharacterIndexFromPosition(eventData.position);
        UpdateSelection();
    }

    /// <summary>
    /// 指针抬起事件
    /// </summary>
    public void OnPointerUp(PointerEventData eventData)
    {
        if (longPressCoroutine != null)
        {
            StopCoroutine(longPressCoroutine);
            longPressCoroutine = null;
        }

        isSelecting = false;
        isLongPress = false;

        // 如果没有选择文本，设置光标位置
        if (!hasSelection && clickCount == 1)
        {
            SetCaretPosition(selectionStart);
        }
    }

    /// <summary>
    /// 长按检测
    /// </summary>
    private IEnumerator LongPressDetection()
    {
        yield return new WaitForSeconds(longPressDuration);
        isLongPress = true;

        // 长按时选择单词
        if (!hasSelection)
        {
            SelectWordAtPosition(touchStartPosition);
        }
    }

    /// <summary>
    /// 根据屏幕位置获取字符索引
    /// </summary>
    public int GetCharacterIndexFromPosition(Vector2 screenPos)
    {
        if (textComponent == null) return 0;

        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            textRectTransform, screenPos, uiCamera, out localPos);

        // 使用 cachedTextGenerator，它只包含可见的字符
        TextGenerator cachedTextGenerator = textComponent.cachedTextGenerator;
        if (cachedTextGenerator.lineCount == 0)
        {
            return inputField.text.Length;
        }

        // 首先确定点击位置在哪一行（使用与 MyInputField 相同的逻辑）
        int targetLine = GetUnclampedCharacterLineFromPosition(localPos, cachedTextGenerator);

        if (targetLine < 0)
        {
            return 0;
        }

        if (targetLine >= cachedTextGenerator.lineCount)
        {
            // 如果位置在可见区域下方，使用完整文本生成器来计算实际字符索引
            UpdateTextGenerationSettings();
            textGenerator.Populate(inputField.text, textSettings);

            if (textGenerator.lineCount == 0)
            {
                return inputField.text.Length;
            }

            // 检查是否拖拽到了文本区域的底部
            Rect textRect = textRectTransform.rect;
            if (localPos.y < textRect.yMin)
            {
                // 拖拽到可见区域下方，使用完整文本生成器重新计算
                // 将 localPos 转换为相对于完整文本的坐标
                // 注意：完整文本生成器的坐标系统可能与可见区域不同
                int fullTargetLine = GetUnclampedCharacterLineFromPosition(localPos, textGenerator);

                if (fullTargetLine >= textGenerator.lineCount)
                {
                    // 拖拽到文本最底部，返回文本末尾
                    return inputField.text.Length;
                }

                if (fullTargetLine < 0)
                {
                    return 0;
                }

                // 在完整文本生成器的目标行中查找字符
                UILineInfo fullLineInfo = textGenerator.lines[fullTargetLine];
                int fullLineStart = fullLineInfo.startCharIdx;
                int fullLineEnd = (fullTargetLine < textGenerator.lineCount - 1)
                    ? textGenerator.lines[fullTargetLine + 1].startCharIdx
                    : textGenerator.characterCount;

                // 如果拖拽位置在文本区域底部，直接返回该行的末尾
                if (localPos.y < textRect.yMin + textRect.height * 0.1f)
                {
                    // 拖拽到很底部，返回该行的末尾或文本末尾
                    return Mathf.Min(fullLineEnd, inputField.text.Length);
                }

                for (int i = fullLineStart; i < fullLineEnd && i < textGenerator.characterCount; i++)
                {
                    UICharInfo charInfo = textGenerator.characters[i];
                    float charPosX = charInfo.cursorPos.x / textComponent.pixelsPerUnit;
                    float charWidth = charInfo.charWidth / textComponent.pixelsPerUnit;

                    float distanceToLeft = localPos.x - charPosX;
                    float distanceToRight = (charPosX + charWidth) - localPos.x;

                    if (distanceToLeft < distanceToRight)
                    {
                        return i;
                    }
                }

                return Mathf.Min(fullLineEnd, inputField.text.Length);
            }
            else
            {
                // 如果不在可见区域下方，但 targetLine 超出范围，返回可见区域的末尾
                int drawStartValue = GetDrawStart();
                return drawStartValue + cachedTextGenerator.characterCountVisible;
            }
        }

        // 在目标行中查找最接近的字符
        UILineInfo targetLineInfo = cachedTextGenerator.lines[targetLine];
        int lineStart = targetLineInfo.startCharIdx;
        int lineEnd = GetLineEndPosition(cachedTextGenerator, targetLine);

        float localPosX = localPos.x * textComponent.pixelsPerUnit; // 转换为像素坐标
        int drawStart = GetDrawStart();

        for (int i = lineStart; i < lineEnd && i < cachedTextGenerator.characterCountVisible; i++)
        {
            UICharInfo charInfo = cachedTextGenerator.characters[i];
            Vector2 charPos = charInfo.cursorPos / textComponent.pixelsPerUnit;
            float charWidth = charInfo.charWidth / textComponent.pixelsPerUnit;

            float distanceToLeft = localPos.x - charPos.x;
            float distanceToRight = (charPos.x + charWidth) - localPos.x;

            if (distanceToLeft < distanceToRight)
            {
                // 返回实际文本中的索引（需要加上 drawStart）
                return drawStart + i;
            }
        }

        // 如果位置在行尾之后，返回行尾字符索引
        // 需要加上 m_DrawStart 来得到实际文本中的索引
        return drawStart + lineEnd;
    }

    /// <summary>
    /// 获取未限制的行索引（与 MyInputField 的实现一致）
    /// </summary>
    private int GetUnclampedCharacterLineFromPosition(Vector2 pos, TextGenerator generator)
    {
        if (!inputField.multiLine)
        {
            return 0;
        }

        float posY = pos.y * textComponent.pixelsPerUnit;
        float prevBottomY = 0f;

        for (int i = 0; i < generator.lineCount; i++)
        {
            UILineInfo lineInfo = generator.lines[i];
            float lineTopY = lineInfo.topY;
            float lineBottomY = lineInfo.topY - lineInfo.height;

            if (posY > lineTopY)
            {
                float lineHeight = lineTopY - prevBottomY;
                if (posY > lineTopY - 0.5f * lineHeight)
                {
                    return i - 1;
                }
                return i;
            }

            if (posY > lineBottomY)
            {
                return i;
            }

            prevBottomY = lineBottomY;
        }

        return generator.lineCount;
    }

    /// <summary>
    /// 获取行结束位置
    /// </summary>
    private int GetLineEndPosition(TextGenerator gen, int line)
    {
        line = Mathf.Max(line, 0);
        if (line + 1 < gen.lines.Count)
        {
            return gen.lines[line + 1].startCharIdx - 1;
        }
        return gen.characterCountVisible;
    }

    /// <summary>
    /// 获取 DrawStart（通过反射访问 MyInputField 的 m_DrawStart）
    /// </summary>
    private int GetDrawStart()
    {
        try
        {
            var drawStartField = typeof(MyInputField).GetField("m_DrawStart",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (drawStartField != null)
            {
                return (int)drawStartField.GetValue(inputField);
            }
        }
        catch (System.Exception)
        {
            // 如果无法访问，返回 0
        }
        return 0;
    }

    /// <summary>
    /// 获取 DrawEnd（通过反射访问 MyInputField 的 m_DrawEnd）
    /// </summary>
    private int GetDrawEnd()
    {
        try
        {
            var drawEndField = typeof(MyInputField).GetField("m_DrawEnd",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (drawEndField != null)
            {
                return (int)drawEndField.GetValue(inputField);
            }
        }
        catch (System.Exception)
        {
            // 如果无法访问，返回文本长度
        }
        return inputField.text.Length;
    }

    /// <summary>
    /// 选择单词
    /// </summary>
    private void SelectWordAtPosition(Vector2 screenPos)
    {
        int clickPos = GetCharacterIndexFromPosition(screenPos);
        SelectWord(clickPos);
    }

    /// <summary>
    /// 选择整行
    /// </summary>
    private void SelectLineAtPosition(Vector2 screenPos)
    {
        int clickPos = GetCharacterIndexFromPosition(screenPos);
        SelectLine(clickPos);
    }

    /// <summary>
    /// 选择单词
    /// </summary>
    private void SelectWord(int position)
    {
        if (string.IsNullOrEmpty(inputField.text))
        {
            selectionStart = 0;
            // Debug.LogError("222selectionStart: " + selectionStart);

            selectionEnd = 0;
            return;
        }

        string text = inputField.text;

        // 找到单词起始位置
        selectionStart = position;
        // Debug.LogError("333selectionStart: " + selectionStart);
        while (selectionStart > 0 && !IsWordSeparator(text[selectionStart - 1]))
        {
            selectionStart--;
        }
        // Debug.LogError("444selectionStart: " + selectionStart);

        // 找到单词结束位置
        selectionEnd = position;
        while (selectionEnd < text.Length && !IsWordSeparator(text[selectionEnd]))
        {
            selectionEnd++;
        }

        hasSelection = true;
        // Debug.LogError("111hasSelection=" + hasSelection);
        UpdateSelection();
    }

    /// <summary>
    /// 选择整行
    /// </summary>
    private void SelectLine(int position)
    {
        if (string.IsNullOrEmpty(inputField.text))
        {
            selectionStart = 0;
            // Debug.LogError("555selectionStart: " + selectionStart);

            selectionEnd = 0;
            return;
        }

        UpdateTextGenerationSettings();
        textGenerator.Populate(inputField.text, textSettings);

        // 找到所在行
        for (int i = 0; i < textGenerator.lineCount; i++)
        {
            UILineInfo lineInfo = textGenerator.lines[i];
            int lineEndCharIdx = (i < textGenerator.lineCount - 1) ? textGenerator.lines[i + 1].startCharIdx : textGenerator.characterCount;
            if (position >= lineInfo.startCharIdx && position < lineEndCharIdx)
            {
                selectionStart = lineInfo.startCharIdx;
                // Debug.LogError("666selectionStart: " + selectionStart);

                selectionEnd = lineEndCharIdx;
                break;
            }
        }

        hasSelection = true;
        // Debug.LogError("222hasSelection=" + hasSelection);
        UpdateSelection();
    }

    /// <summary>
    /// 判断是否是单词分隔符
    /// </summary>
    private bool IsWordSeparator(char c)
    {
        return char.IsWhiteSpace(c) || char.IsPunctuation(c) || c == '\n' || c == '\r';
    }

    /// <summary>
    /// 更新选中高亮
    /// </summary>
    private void UpdateSelectionHighlight()
    {
        // 清除之前的高亮
        ClearSelectionHighlights();
        // Debug.LogError("UpdateSelectionHighlight: " + hasSelection + " " + selectionStart + " " + selectionEnd);

        if (!hasSelection || selectionStart == selectionEnd || !canHighlight)
        {
            return;
        }
        int start = Mathf.Min(selectionStart, selectionEnd);
        int end = Mathf.Max(selectionStart, selectionEnd);

        // 使用 cachedTextGenerator，它只包含可见的字符
        TextGenerator cachedTextGenerator = textComponent.cachedTextGenerator;
        if (cachedTextGenerator.lineCount == 0)
        {
            return;
        }


        int drawStart = GetDrawStart();
        int drawEnd = GetDrawEnd();
        // Debug.LogError("selectionStart: " + selectionStart + " selectionEnd: " + selectionEnd + " drawStart: " + drawStart + " drawEnd: " + drawEnd);

        // 计算可见区域内的选中范围（相对于可见区域的索引）
        int visibleStart = Mathf.Max(0, start - drawStart);
        int visibleEnd = Mathf.Max(0, end - drawStart);

        // 限制在可见字符范围内
        visibleEnd = Mathf.Min(visibleEnd, cachedTextGenerator.characterCountVisible);

        if (visibleStart >= visibleEnd)
        {
            return;
        }

        // 为每个选中的行创建高亮（只处理可见区域内的行）
        for (int i = 0; i < cachedTextGenerator.lineCount; i++)
        {
            UILineInfo lineInfo = cachedTextGenerator.lines[i];
            int lineStart = lineInfo.startCharIdx;
            int lineEnd = GetLineEndPosition(cachedTextGenerator, i);

            // 检查这一行是否有被选中的部分（在可见区域内）
            if (visibleEnd > lineStart && visibleStart < lineEnd)
            {
                int highlightStart = Mathf.Max(visibleStart, lineStart);
                int highlightEnd = Mathf.Min(visibleEnd, lineEnd);

                if (highlightStart < highlightEnd)
                {
                    CreateLineHighlight(highlightStart, highlightEnd, lineInfo, cachedTextGenerator);
                }
            }
        }
    }

    /// <summary>
    /// 创建行高亮
    /// </summary>
    private void CreateLineHighlight(int start, int end, UILineInfo lineInfo, TextGenerator generator)
    {
        GameObject highlight = new GameObject("SelectionHighlight");
        highlight.transform.SetParent(textComponent.transform);
        highlight.transform.localScale = Vector3.one;

        Image highlightImage = highlight.AddComponent<Image>();
        highlightImage.color = selectionColor;
        highlightImage.raycastTarget = false;

        RectTransform rectTransform = highlight.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.zero;
        rectTransform.pivot = Vector2.zero;

        // 确保索引有效
        if (start >= generator.characterCountVisible || end > generator.characterCountVisible)
        {
            Destroy(highlight);
            return;
        }

        // 计算高亮位置和大小（TextGenerator 返回的是像素坐标，需要转换为 Unity 单位）
        UICharInfo startChar = generator.characters[start];
        UICharInfo endChar = generator.characters[Mathf.Min(end - 1, generator.characterCountVisible - 1)];

        // 转换为 Unity 单位坐标
        float pixelsPerUnit = textComponent.pixelsPerUnit;
        float startX = startChar.cursorPos.x / pixelsPerUnit;
        float endX = (endChar.cursorPos.x + endChar.charWidth) / pixelsPerUnit;

        // 计算 Y 坐标：lineInfo.topY 是行的顶部（像素坐标），需要转换为 Unity 单位
        // 高亮应该从行的顶部向下延伸行的高度
        float lineTopY = lineInfo.topY / pixelsPerUnit;
        float lineHeight = lineInfo.height / pixelsPerUnit;
        float y = lineTopY - lineHeight; // 行的底部位置

        rectTransform.anchoredPosition = new Vector2(startX, y);
        rectTransform.sizeDelta = new Vector2(endX - startX, lineHeight);

        var localPosition = rectTransform.localPosition;
        localPosition.z = 0;
        rectTransform.localPosition = localPosition;

        selectionHighlights.Add(highlight);
    }

    /// <summary>
    /// 清除所有高亮
    /// </summary>
    private void ClearSelectionHighlights()
    {
        foreach (GameObject highlight in selectionHighlights)
        {
            if (highlight != null)
                Destroy(highlight);
        }
        selectionHighlights.Clear();
    }

    /// <summary>
    /// 更新光标位置
    /// </summary>
    private void UpdateCaretPosition()
    {
        if (caretObject == null || !isFocused) return;

        // int caretPos = hasSelection ? Mathf.Max(selectionStart, selectionEnd) : inputField.caretPosition;
        // Vector2 caretPosition = GetCaretWorldPosition(caretPos);
        int caretPosition = inputField.caretPosition;
        Text textComponent = inputField.textComponent;
        RectTransform caretRect = caretObject.GetComponent<RectTransform>();
        // caretRect.anchoredPosition = caretPosition;
        caretRect.sizeDelta = new Vector2(caretWidth, textComponent.fontSize);

        caretObject.SetActive(!hasSelection && isCaretVisible);



        string text = inputField.text;

        if (textComponent == null || caretObject == null)
        {
            return;
        }

        // 确保光标位置在有效范围内
        if (caretPosition > text.Length)
        {
            caretPosition = text.Length;
        }

        // 使用缓存的文本生成器来获取字符和行信息
        TextGenerator textGen = textComponent.cachedTextGenerator;
        if (textGen == null)
        {
            // 如果缓存未生成，强制生成一次
            textComponent.cachedTextGenerator.Invalidate();
            textGen = textComponent.cachedTextGenerator;
        }

        // 确保文本生成器已生成
        if (textGen.characterCount == 0 && !string.IsNullOrEmpty(text))
        {
            TextGenerationSettings settings = textComponent.GetGenerationSettings(textComponent.rectTransform.rect.size);
            textGen.Populate(text, settings);
        }

        // 计算光标位置（TextGenerator 处理的是整个文本，所以直接使用 caretPosition）
        int displayCaretPos = Mathf.Clamp(caretPosition, 0, textGen.characterCount);

        // 获取光标所在字符的信息
        float caretX = 0;
        float caretY = 0;

        // 如果文本为空，光标位置在起始位置
        if (textGen.characterCount == 0)
        {
            caretX = 0;
            caretY = 0;
        }
        else if (displayCaretPos < textGen.characters.Count)
        {
            UICharInfo charInfo = textGen.characters[displayCaretPos];
            caretX = charInfo.cursorPos.x / textComponent.pixelsPerUnit;
        }
        else if (textGen.characterCount > 0)
        {
            // 如果光标在文本末尾，使用最后一个字符的位置
            UICharInfo lastChar = textGen.characters[textGen.characterCount - 1];
            caretX = (lastChar.cursorPos.x + lastChar.charWidth) / textComponent.pixelsPerUnit;
        }

        // 找到光标所在的行
        int caretLine = 0;
        if (textGen.lineCount > 0)
        {
            // 精确的行查找：检查光标位置是否在行的字符范围内
            for (int i = 0; i < textGen.lineCount; i++)
            {
                UILineInfo lineInfo = textGen.lines[i];
                int lineEndCharIdx = (i < textGen.lineCount - 1) ? textGen.lines[i + 1].startCharIdx : textGen.characterCount;

                if (displayCaretPos >= lineInfo.startCharIdx && displayCaretPos < lineEndCharIdx)
                {
                    caretLine = i;
                    break;
                }
            }

            // 计算Y坐标（行高）
            if (caretLine < textGen.lines.Count)
            {
                UILineInfo lineInfo = textGen.lines[caretLine];
                // 行的顶部Y坐标（相对于文本组件的顶部）
                caretY = lineInfo.height / textComponent.pixelsPerUnit;
            }
        }

        // 设置光标位置（相对于文本组件的锚点）
        Vector2 caretPos = textComponent.rectTransform.anchoredPosition;

        // 考虑文本对齐方式
        TextAnchor anchor = textComponent.alignment;
        Rect rect = textComponent.rectTransform.rect;

        // 根据对齐方式调整X坐标
        if (anchor == TextAnchor.UpperLeft || anchor == TextAnchor.MiddleLeft || anchor == TextAnchor.LowerLeft)
        {
            caretPos.x += caretX;
        }
        else if (anchor == TextAnchor.UpperCenter || anchor == TextAnchor.MiddleCenter || anchor == TextAnchor.LowerCenter)
        {
            caretPos.x += caretX - rect.width * 0.5f;
        }
        else if (anchor == TextAnchor.UpperRight || anchor == TextAnchor.MiddleRight || anchor == TextAnchor.LowerRight)
        {
            caretPos.x += caretX - rect.width;
        }
        else
        {
            // 默认左对齐
            caretPos.x += caretX;
        }

        // 根据对齐方式调整Y坐标
        if (anchor == TextAnchor.UpperLeft || anchor == TextAnchor.UpperCenter || anchor == TextAnchor.UpperRight)
        {
            caretPos.y -= caretY;
        }
        else if (anchor == TextAnchor.MiddleLeft || anchor == TextAnchor.MiddleCenter || anchor == TextAnchor.MiddleRight)
        {
            caretPos.y -= caretY - textComponent.transform.parent.GetComponent<RectTransform>().rect.height * 0.5f;
        }
        else if (anchor == TextAnchor.LowerLeft || anchor == TextAnchor.LowerCenter || anchor == TextAnchor.LowerRight)
        {
            caretPos.y += textComponent.transform.parent.GetComponent<RectTransform>().rect.height; ;
            caretPos.y -= caretY;
            caretPos.y -= caretY * (caretLine);
            // caretPos.y -= (caretLine) * caretY;
        }
        else
        {
            // 默认上对齐
            caretPos.y -= caretY;
        }

        caretRect.anchoredPosition = caretPos;
    }

    /// <summary>
    /// 获取光标世界位置
    /// </summary>
    private Vector2 GetCaretWorldPosition(int position)
    {
        if (textComponent == null || string.IsNullOrEmpty(inputField.text) || position == 0)
            return Vector2.zero;

        UpdateTextGenerationSettings();
        textGenerator.Populate(inputField.text, textSettings);

        position = Mathf.Clamp(position, 0, textGenerator.characterCount - 1);
        UICharInfo charInfo = textGenerator.characters[position];

        return new Vector2(charInfo.cursorPos.x, charInfo.cursorPos.y);
    }

    /// <summary>
    /// 处理键盘输入
    /// </summary>
    private void HandleKeyboardInput()
    {
        // 这里可以添加键盘选择逻辑
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            // Shift+方向键选择文本
            if (!hasSelection)
            {
                selectionStart = inputField.caretPosition;
                // Debug.LogError("777selectionStart: " + selectionStart);

                hasSelection = true;
                //  Debug.LogError("333hasSelection=" + hasSelection);
            }
            selectionEnd = inputField.caretPosition;
            UpdateSelection();
        }
        else if (hasSelection && (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow) ||
                 Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow)))
        {
            // 方向键清除选择
            ClearSelection();
        }
    }

    /// <summary>
    /// 设置光标位置
    /// </summary>
    public void SetCaretPosition(int position)
    {
        position = Mathf.Clamp(position, 0, inputField.text.Length);
        selectionStart = position;
        // Debug.LogError("888selectionStart: " + selectionStart);

        selectionEnd = position;
        hasSelection = false;

#if UNITY_EDITOR
        try
        {
            var caretPosField = typeof(InputField).GetField("m_CaretPosition",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (caretPosField != null)
            {
                caretPosField.SetValue(inputField, position);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("无法设置光标位置: " + e.Message);
        }
#endif

        UpdateCaretPosition();
    }

    /// <summary>
    /// 设置选择范围
    /// </summary>
    public void SetSelection(int start, int end)
    {
        selectionStart = Mathf.Clamp(start, 0, inputField.text.Length);
        // Debug.LogError("999selectionStart: " + selectionStart);

        selectionEnd = Mathf.Clamp(end, 0, inputField.text.Length);
        hasSelection = selectionStart != selectionEnd;
        // Debug.LogError("444hasSelection=" + hasSelection);
        UpdateSelection();
    }

    /// <summary>
    /// 更新选择
    /// </summary>
    private void UpdateSelection()
    {
        hasSelection = selectionStart != selectionEnd;
        // Debug.LogError("555hasSelection=" + hasSelection + " selectionStart=" + selectionStart + " selectionEnd=" + selectionEnd);
        if (!hasSelection)
        {
            ClearSelectionHighlights();
            if (isFocused)
            {
                caretObject.SetActive(true);
            }
        }
        else
        {
            caretObject.SetActive(false);
            UpdateSelectionHighlight();
        }
    }

    /// <summary>
    /// 清除选择
    /// </summary>
    public void ClearSelection()
    {
        selectionStart = -1;
        selectionEnd = -1;
        hasSelection = false;
        // Debug.LogError("666hasSelection=" + hasSelection);
        ClearSelectionHighlights();

        if (isFocused)
        {
            caretObject.SetActive(true);
        }
    }

    /// <summary>
    /// 获取选中文本
    /// </summary>
    public string GetSelectedText()
    {
        if (!hasSelection || selectionStart == selectionEnd)
            return string.Empty;

        int start = Mathf.Min(selectionStart, selectionEnd);
        int end = Mathf.Max(selectionStart, selectionEnd);

        return inputField.text.Substring(start, end - start);
    }

    /// <summary>
    /// 文本改变回调
    /// </summary>
    private void OnTextChanged(string text)
    {
        if (hasSelection)
        {
            // 文本改变时清除选择
            ClearSelection();
        }
        UpdateCaretPosition();
    }

    /// <summary>
    /// 输入框获得焦点
    /// </summary>
    private void OnInputFieldSelected()
    {
        isFocused = true;
        StartCaretBlink();

        if (!hasSelection)
        {
            SetCaretPosition(inputField.text.Length);
        }
    }

    /// <summary>
    /// 输入框失去焦点
    /// </summary>
    private void OnInputFieldDeselected()
    {
        isFocused = false;
        caretObject.SetActive(false);
        ClearSelectionHighlights();
        StopCaretBlink();
        ClearSelection();
        clickCount = 0;
    }

    /// <summary>
    /// 开始光标闪烁
    /// </summary>
    private void StartCaretBlink()
    {
        StopCaretBlink();
        blinkCoroutine = StartCoroutine(CaretBlink());
    }

    /// <summary>
    /// 停止光标闪烁
    /// </summary>
    private void StopCaretBlink()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }
        if (caretObject != null && isFocused && !hasSelection)
        {
            caretObject.SetActive(true);
            isCaretVisible = true;
        }
    }

    /// <summary>
    /// 光标闪烁协程
    /// </summary>
    private IEnumerator CaretBlink()
    {
        while (isFocused && !hasSelection)
        {
            isCaretVisible = !isCaretVisible;
            caretObject.SetActive(isCaretVisible);
            yield return new WaitForSeconds(blinkInterval);
        }
    }

    /// <summary>
    /// 设置选择颜色
    /// </summary>
    public void SetSelectionColor(Color color)
    {
        selectionColor = color;
        // 更新现有高亮的颜色
        foreach (GameObject highlight in selectionHighlights)
        {
            Image image = highlight.GetComponent<Image>();
            if (image != null)
            {
                image.color = selectionColor;
            }
        }
    }

    void OnDestroy()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
        }
        if (longPressCoroutine != null)
        {
            StopCoroutine(longPressCoroutine);
        }
        ClearSelectionHighlights();
    }
}