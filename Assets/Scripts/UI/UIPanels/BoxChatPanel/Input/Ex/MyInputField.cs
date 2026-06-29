

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;


public class MyInputField : Selectable, IUpdateSelectedHandler, IEventSystemHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, ISubmitHandler, ICanvasElement, ILayoutElement
{
    public enum ContentType
    {
        Standard,
        Autocorrected,
        IntegerNumber,
        DecimalNumber,
        Alphanumeric,
        Name,
        EmailAddress,
        Password,
        Pin,
        Custom
    }

    public enum InputType
    {
        Standard,
        AutoCorrect,
        Password
    }

    public enum CharacterValidation
    {
        None,
        Integer,
        Decimal,
        Alphanumeric,
        Name,
        EmailAddress
    }

    public enum LineType
    {
        SingleLine,
        MultiLineSubmit,
        MultiLineNewline
    }

    public delegate char OnValidateInput(string text, int charIndex, char addedChar);

    [Serializable]
    public class SubmitEvent : UnityEvent<string>
    {
    }

    [Serializable]
    public class EndEditEvent : UnityEvent<string>
    {
    }

    [Serializable]
    public class OnChangeEvent : UnityEvent<string>
    {
    }

    protected enum EditState
    {
        Continue,
        Finish
    }

    protected TouchScreenKeyboard m_Keyboard;

    private static readonly char[] kSeparators = new char[6] { ' ', '.', ',', '\t', '\r', '\n' };

    private static bool s_IsQuestDeviceEvaluated = false;

    private static bool s_IsQuestDevice = false;

    [SerializeField]
    [FormerlySerializedAs("text")]
    protected Text m_TextComponent;

    [SerializeField]
    protected Graphic m_Placeholder;

    [SerializeField]
    private ContentType m_ContentType = ContentType.Standard;

    [FormerlySerializedAs("inputType")]
    [SerializeField]
    private InputType m_InputType = InputType.Standard;

    [FormerlySerializedAs("asteriskChar")]
    [SerializeField]
    private char m_AsteriskChar = '*';

    [FormerlySerializedAs("keyboardType")]
    [SerializeField]
    private TouchScreenKeyboardType m_KeyboardType = TouchScreenKeyboardType.Default;

    [SerializeField]
    private LineType m_LineType = LineType.SingleLine;

    [FormerlySerializedAs("hideMobileInput")]
    [SerializeField]
    private bool m_HideMobileInput = false;

    [FormerlySerializedAs("validation")]
    [SerializeField]
    private CharacterValidation m_CharacterValidation = CharacterValidation.None;

    [FormerlySerializedAs("characterLimit")]
    [SerializeField]
    private int m_CharacterLimit = 0;

    [FormerlySerializedAs("onSubmit")]
    [FormerlySerializedAs("m_OnSubmit")]
    [FormerlySerializedAs("m_EndEdit")]
    [FormerlySerializedAs("m_OnEndEdit")]
    [SerializeField]
    private SubmitEvent m_OnSubmit = new SubmitEvent();

    [SerializeField]
    private EndEditEvent m_OnDidEndEdit = new EndEditEvent();

    [FormerlySerializedAs("onValueChange")]
    [FormerlySerializedAs("m_OnValueChange")]
    [SerializeField]
    private OnChangeEvent m_OnValueChanged = new OnChangeEvent();

    [FormerlySerializedAs("onValidateInput")]
    [SerializeField]
    private OnValidateInput m_OnValidateInput;

    [FormerlySerializedAs("selectionColor")]
    [SerializeField]
    private Color m_CaretColor = new Color(10f / 51f, 10f / 51f, 10f / 51f, 1f);

    [SerializeField]
    private bool m_CustomCaretColor = false;

    [SerializeField]
    private Color m_SelectionColor = new Color(56f / 85f, 0.80784315f, 1f, 64f / 85f);

    [SerializeField]
    [Multiline]
    [FormerlySerializedAs("mValue")]
    protected string m_Text = string.Empty;

    [SerializeField]
    [Range(0f, 4f)]
    private float m_CaretBlinkRate = 0.85f;

    [SerializeField]
    [Range(1f, 5f)]
    private int m_CaretWidth = 1;

    [SerializeField]
    private bool m_ReadOnly = false;

    [SerializeField]
    private bool m_ShouldActivateOnSelect = true;

    protected int m_CaretPosition = 0;

    protected int m_CaretSelectPosition = 0;

    private RectTransform caretRectTrans = null;

    protected UIVertex[] m_CursorVerts = null;

    private TextGenerator m_InputTextCache;

    private CanvasRenderer m_CachedInputRenderer;

    private bool m_PreventFontCallback = false;

    [NonSerialized]
    protected Mesh m_Mesh;

    private bool m_AllowInput = false;

    private bool m_ShouldActivateNextUpdate = false;

    private bool m_UpdateDrag = false;

    private bool m_DragPositionOutOfBounds = false;

    private const float kHScrollSpeed = 0.05f;

    private const float kVScrollSpeed = 0.1f;

    protected bool m_CaretVisible;

    private Coroutine m_BlinkCoroutine = null;

    private float m_BlinkStartTime = 0f;

    protected int m_DrawStart = 0;

    protected int m_DrawEnd = 0;

    private Coroutine m_DragCoroutine = null;

    private string m_OriginalText = "";

    private bool m_WasCanceled = false;

    private bool m_HasDoneFocusTransition = false;

    private WaitForSecondsRealtime m_WaitForSecondsRealtime;

    private bool m_TouchKeyboardAllowsInPlaceEditing = false;

    private bool m_IsCompositionActive = false;

    private const string kEmailSpecialCharacters = "!#$%&'*+-/=?^_`{|}~";

    private const string kOculusQuestDeviceModel = "Oculus Quest";

    private Event m_ProcessingEvent = new Event();

    private const int k_MaxTextLength = 16382;

    private BaseInput input
    {
        get
        {
            if ((bool)EventSystem.current && (bool)EventSystem.current.currentInputModule)
            {
                return EventSystem.current.currentInputModule.input;
            }

            return null;
        }
    }

    private string compositionString => (input != null) ? input.compositionString : Input.compositionString;

    protected Mesh mesh
    {
        get
        {
            if (m_Mesh == null)
            {
                m_Mesh = new Mesh();
            }

            return m_Mesh;
        }
    }

    protected TextGenerator cachedInputTextGenerator
    {
        get
        {
            if (m_InputTextCache == null)
            {
                m_InputTextCache = new TextGenerator();
            }

            return m_InputTextCache;
        }
    }

    public bool shouldHideMobileInput
    {
        get
        {
            RuntimePlatform platform = Application.platform;
            RuntimePlatform runtimePlatform = platform;
            if (runtimePlatform == RuntimePlatform.IPhonePlayer || runtimePlatform == RuntimePlatform.Android || runtimePlatform == RuntimePlatform.tvOS)
            {
                return m_HideMobileInput;
            }

            return true;
        }
        set
        {
            SetStruct(ref m_HideMobileInput, value);
        }
    }

    public virtual bool shouldActivateOnSelect
    {
        get
        {
            return m_ShouldActivateOnSelect && Application.platform != RuntimePlatform.tvOS;
        }
        set
        {
            m_ShouldActivateOnSelect = value;
        }
    }

    public string text
    {
        get
        {
            return m_Text;
        }
        set
        {
            SetText(value);
        }
    }

    public bool isFocused => m_AllowInput;

    public float caretBlinkRate
    {
        get
        {
            return m_CaretBlinkRate;
        }
        set
        {
            if (SetStruct(ref m_CaretBlinkRate, value) && m_AllowInput)
            {
                SetCaretActive();
            }
        }
    }

    public int caretWidth
    {
        get
        {
            return m_CaretWidth;
        }
        set
        {
            if (SetStruct(ref m_CaretWidth, value))
            {
                MarkGeometryAsDirty();
            }
        }
    }

    public Text textComponent
    {
        get
        {
            return m_TextComponent;
        }
        set
        {
            if (m_TextComponent != null)
            {
                m_TextComponent.UnregisterDirtyVerticesCallback(MarkGeometryAsDirty);
                m_TextComponent.UnregisterDirtyVerticesCallback(UpdateLabel);
                m_TextComponent.UnregisterDirtyMaterialCallback(UpdateCaretMaterial);
            }

            if (SetClass(ref m_TextComponent, value))
            {
                EnforceTextHOverflow();
                if (m_TextComponent != null)
                {
                    m_TextComponent.RegisterDirtyVerticesCallback(MarkGeometryAsDirty);
                    m_TextComponent.RegisterDirtyVerticesCallback(UpdateLabel);
                    m_TextComponent.RegisterDirtyMaterialCallback(UpdateCaretMaterial);
                }
            }
        }
    }

    public Graphic placeholder
    {
        get
        {
            return m_Placeholder;
        }
        set
        {
            SetClass(ref m_Placeholder, value);
        }
    }

    public Color caretColor
    {
        get
        {
            return customCaretColor ? m_CaretColor : textComponent.color;
        }
        set
        {
            if (SetColor(ref m_CaretColor, value))
            {
                MarkGeometryAsDirty();
            }
        }
    }

    public bool customCaretColor
    {
        get
        {
            return m_CustomCaretColor;
        }
        set
        {
            if (m_CustomCaretColor != value)
            {
                m_CustomCaretColor = value;
                MarkGeometryAsDirty();
            }
        }
    }

    public Color selectionColor
    {
        get
        {
            return m_SelectionColor;
        }
        set
        {
            if (SetColor(ref m_SelectionColor, value))
            {
                MarkGeometryAsDirty();
            }
        }
    }

    public EndEditEvent onEndEdit
    {
        get
        {
            return m_OnDidEndEdit;
        }
        set
        {
            SetClass(ref m_OnDidEndEdit, value);
        }
    }

    public SubmitEvent onSubmit
    {
        get
        {
            return m_OnSubmit;
        }
        set
        {
            SetClass(ref m_OnSubmit, value);
        }
    }

    [Obsolete("onValueChange has been renamed to onValueChanged")]
    public OnChangeEvent onValueChange
    {
        get
        {
            return onValueChanged;
        }
        set
        {
            onValueChanged = value;
        }
    }

    public OnChangeEvent onValueChanged
    {
        get
        {
            return m_OnValueChanged;
        }
        set
        {
            SetClass(ref m_OnValueChanged, value);
        }
    }

    public OnValidateInput onValidateInput
    {
        get
        {
            return m_OnValidateInput;
        }
        set
        {
            SetClass(ref m_OnValidateInput, value);
        }
    }

    public int characterLimit
    {
        get
        {
            return m_CharacterLimit;
        }
        set
        {
            if (SetStruct(ref m_CharacterLimit, Math.Max(0, value)))
            {
                UpdateLabel();
                if (m_Keyboard != null)
                {
                    m_Keyboard.characterLimit = value;
                }
            }
        }
    }

    public ContentType contentType
    {
        get
        {
            return m_ContentType;
        }
        set
        {
            if (SetStruct(ref m_ContentType, value))
            {
                EnforceContentType();
            }
        }
    }

    public LineType lineType
    {
        get
        {
            return m_LineType;
        }
        set
        {
            if (SetStruct(ref m_LineType, value))
            {
                SetToCustomIfContentTypeIsNot(ContentType.Standard, ContentType.Autocorrected);
                EnforceTextHOverflow();
            }
        }
    }

    public InputType inputType
    {
        get
        {
            return m_InputType;
        }
        set
        {
            if (SetStruct(ref m_InputType, value))
            {
                SetToCustom();
            }
        }
    }

    public TouchScreenKeyboard touchScreenKeyboard => m_Keyboard;

    public TouchScreenKeyboardType keyboardType
    {
        get
        {
            return m_KeyboardType;
        }
        set
        {
            if (SetStruct(ref m_KeyboardType, value))
            {
                SetToCustom();
            }
        }
    }

    public CharacterValidation characterValidation
    {
        get
        {
            return m_CharacterValidation;
        }
        set
        {
            if (SetStruct(ref m_CharacterValidation, value))
            {
                SetToCustom();
            }
        }
    }

    public bool readOnly
    {
        get
        {
            return m_ReadOnly;
        }
        set
        {
            m_ReadOnly = value;
        }
    }

    public bool multiLine => m_LineType == LineType.MultiLineNewline || lineType == LineType.MultiLineSubmit;

    public char asteriskChar
    {
        get
        {
            return m_AsteriskChar;
        }
        set
        {
            if (SetStruct(ref m_AsteriskChar, value))
            {
                UpdateLabel();
            }
        }
    }

    public bool wasCanceled => m_WasCanceled;

    protected int caretPositionInternal
    {
        get
        {
            return m_CaretPosition + compositionString.Length;
        }
        set
        {
            m_CaretPosition = value;
            ClampPos(ref m_CaretPosition);
        }
    }

    protected int caretSelectPositionInternal
    {
        get
        {
            return m_CaretSelectPosition + compositionString.Length;
        }
        set
        {
            m_CaretSelectPosition = value;
            ClampPos(ref m_CaretSelectPosition);
        }
    }

    private bool hasSelection => caretPositionInternal != caretSelectPositionInternal;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("caretSelectPosition has been deprecated. Use selectionFocusPosition instead (UnityUpgradable) -> selectionFocusPosition", true)]
    public int caretSelectPosition
    {
        get
        {
            return selectionFocusPosition;
        }
        protected set
        {
            selectionFocusPosition = value;
        }
    }

    public int caretPosition
    {
        get
        {
            return m_CaretSelectPosition + compositionString.Length;
        }
        set
        {
            selectionAnchorPosition = value;
            selectionFocusPosition = value;
        }
    }

    public int selectionAnchorPosition
    {
        get
        {
            return m_CaretPosition + compositionString.Length;
        }
        set
        {
            if (compositionString.Length == 0)
            {
                m_CaretPosition = value;
                ClampPos(ref m_CaretPosition);
            }
        }
    }

    public int selectionFocusPosition
    {
        get
        {
            return m_CaretSelectPosition + compositionString.Length;
        }
        set
        {
            if (compositionString.Length == 0)
            {
                m_CaretSelectPosition = value;
                ClampPos(ref m_CaretSelectPosition);
            }
        }
    }

    private static string clipboard
    {
        get
        {
            return GUIUtility.systemCopyBuffer;
        }
        set
        {
            GUIUtility.systemCopyBuffer = value;
        }
    }

    public virtual float minWidth => 5f;

    public virtual float preferredWidth
    {
        get
        {
            if (textComponent == null)
            {
                return 0f;
            }

            TextGenerationSettings generationSettings = textComponent.GetGenerationSettings(Vector2.zero);
            return textComponent.cachedTextGeneratorForLayout.GetPreferredWidth(m_Text, generationSettings) / textComponent.pixelsPerUnit;
        }
    }

    public virtual float flexibleWidth => -1f;

    public virtual float minHeight => 0f;

    public virtual float preferredHeight
    {
        get
        {
            if (textComponent == null)
            {
                return 0f;
            }

            TextGenerationSettings generationSettings = textComponent.GetGenerationSettings(new Vector2(textComponent.rectTransform.rect.size.x, 0f));
            return textComponent.cachedTextGeneratorForLayout.GetPreferredHeight(m_Text, generationSettings) / textComponent.pixelsPerUnit;
        }
    }

    public virtual float flexibleHeight => -1f;

    public virtual int layoutPriority => 1;


    public List<GameObject> selectIncludeObjects = new List<GameObject>(); //选中时包含的对象

    protected MyInputField()
    {
        EnforceTextHOverflow();
    }

    public void SetTextWithoutNotify(string input)
    {
        SetText(input, sendCallback: false);
    }

    private void SetText(string value, bool sendCallback = true)
    {
        if (text == value)
        {
            return;
        }

        if (value == null)
        {
            value = "";
        }

        value = value.Replace("\0", string.Empty);
        if (m_LineType == LineType.SingleLine)
        {
            value = value.Replace("\n", "").Replace("\t", "");
        }

        if (this.onValidateInput != null || characterValidation != 0)
        {
            m_Text = "";
            OnValidateInput onValidateInput = this.onValidateInput ?? new OnValidateInput(Validate);
            m_CaretPosition = (m_CaretSelectPosition = value.Length);
            int num = ((characterLimit > 0) ? Math.Min(characterLimit, value.Length) : value.Length);
            for (int i = 0; i < num; i++)
            {
                char c = onValidateInput(m_Text, m_Text.Length, value[i]);
                if (c != 0)
                {
                    m_Text += c;
                }
            }
        }
        else
        {
            m_Text = ((characterLimit > 0 && value.Length > characterLimit) ? value.Substring(0, characterLimit) : value);
        }

        if (!Application.isPlaying)
        {
            SendOnValueChangedAndUpdateLabel();
            return;
        }

        if (m_Keyboard != null)
        {
            m_Keyboard.text = m_Text;
        }

        if (m_CaretPosition > m_Text.Length)
        {
            m_CaretPosition = (m_CaretSelectPosition = m_Text.Length);
        }
        else if (m_CaretSelectPosition > m_Text.Length)
        {
            m_CaretSelectPosition = m_Text.Length;
        }

        if (sendCallback)
        {
            SendOnValueChanged();
        }

        UpdateLabel();
    }

    protected void ClampPos(ref int pos)
    {
        if (pos < 0)
        {
            pos = 0;
        }
        else if (pos > text.Length)
        {
            pos = text.Length;
        }
    }
#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        EnforceContentType();
        EnforceTextHOverflow();
        m_CharacterLimit = Math.Max(0, m_CharacterLimit);
        if (IsActive())
        {
            ClampPos(ref m_CaretPosition);
            ClampPos(ref m_CaretSelectPosition);
            UpdateLabel();
            if (m_AllowInput)
            {
                SetCaretActive();
            }
        }
    }
#endif
    protected override void Awake()
    {
        base.Awake();
        if (!s_IsQuestDeviceEvaluated)
        {
            s_IsQuestDevice = SystemInfo.deviceModel == "Oculus Quest";
            s_IsQuestDeviceEvaluated = true;
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        if (m_Text == null)
        {
            m_Text = string.Empty;
        }

        m_DrawStart = 0;
        m_DrawEnd = m_Text.Length;
        if (m_CachedInputRenderer != null)
        {
            m_CachedInputRenderer.SetMaterial(m_TextComponent.GetModifiedMaterial(Graphic.defaultGraphicMaterial), Texture2D.whiteTexture);
        }

        if (m_TextComponent != null)
        {
            m_TextComponent.RegisterDirtyVerticesCallback(MarkGeometryAsDirty);
            m_TextComponent.RegisterDirtyVerticesCallback(UpdateLabel);
            m_TextComponent.RegisterDirtyMaterialCallback(UpdateCaretMaterial);
            UpdateLabel();
        }
    }

    protected override void OnDisable()
    {
        m_BlinkCoroutine = null;
        DeactivateInputField();
        if (m_TextComponent != null)
        {
            m_TextComponent.UnregisterDirtyVerticesCallback(MarkGeometryAsDirty);
            m_TextComponent.UnregisterDirtyVerticesCallback(UpdateLabel);
            m_TextComponent.UnregisterDirtyMaterialCallback(UpdateCaretMaterial);
        }

        CanvasUpdateRegistry.DisableCanvasElementForRebuild(this);
        if (m_CachedInputRenderer != null)
        {
            m_CachedInputRenderer.Clear();
        }

        if (m_Mesh != null)
        {
            GameObject.DestroyImmediate(m_Mesh);
        }

        m_Mesh = null;
        base.OnDisable();
    }

    protected override void OnDestroy()
    {
        CanvasUpdateRegistry.UnRegisterCanvasElementForRebuild(this);
        base.OnDestroy();
    }

    private IEnumerator CaretBlink()
    {
        m_CaretVisible = true;
        yield return null;
        while (isFocused && m_CaretBlinkRate > 0f)
        {
            float blinkPeriod = 1f / m_CaretBlinkRate;
            bool blinkState = (Time.unscaledTime - m_BlinkStartTime) % blinkPeriod < blinkPeriod / 2f;
            if (m_CaretVisible != blinkState)
            {
                m_CaretVisible = blinkState;
                if (!hasSelection)
                {
                    MarkGeometryAsDirty();
                }
            }

            yield return null;
        }

        m_BlinkCoroutine = null;
    }

    private void SetCaretVisible()
    {
        if (m_AllowInput)
        {
            m_CaretVisible = true;
            m_BlinkStartTime = Time.unscaledTime;
            SetCaretActive();
        }
    }

    private void SetCaretActive()
    {
        if (!m_AllowInput)
        {
            return;
        }

        if (m_CaretBlinkRate > 0f)
        {
            if (m_BlinkCoroutine == null)
            {
                m_BlinkCoroutine = StartCoroutine(CaretBlink());
            }
        }
        else
        {
            m_CaretVisible = true;
        }
    }

    private void UpdateCaretMaterial()
    {
        if (m_TextComponent != null && m_CachedInputRenderer != null)
        {
            m_CachedInputRenderer.SetMaterial(m_TextComponent.GetModifiedMaterial(Graphic.defaultGraphicMaterial), Texture2D.whiteTexture);
        }
    }

    protected void OnFocus()
    {
        SelectAll();
    }

    protected void SelectAll()
    {
        caretPositionInternal = text.Length;
        caretSelectPositionInternal = 0;
    }

    public void MoveTextEnd(bool shift)
    {
        int length = text.Length;
        if (shift)
        {
            caretSelectPositionInternal = length;
        }
        else
        {
            caretPositionInternal = length;
            caretSelectPositionInternal = caretPositionInternal;
        }

        UpdateLabel();
    }

    public void MoveTextStart(bool shift)
    {
        int num = 0;
        if (shift)
        {
            caretSelectPositionInternal = num;
        }
        else
        {
            caretPositionInternal = num;
            caretSelectPositionInternal = caretPositionInternal;
        }

        UpdateLabel();
    }

    private bool TouchScreenKeyboardShouldBeUsed()
    {
        switch (Application.platform)
        {
            case RuntimePlatform.Android:
                if (s_IsQuestDevice)
                {
                    return TouchScreenKeyboard.isSupported;
                }

                return !TouchScreenKeyboard.isInPlaceEditingAllowed;
            case RuntimePlatform.WebGLPlayer:
                return !TouchScreenKeyboard.isInPlaceEditingAllowed;
            default:
                return TouchScreenKeyboard.isSupported;
        }
    }

    private bool InPlaceEditing()
    {
        return !TouchScreenKeyboard.isSupported || m_TouchKeyboardAllowsInPlaceEditing;
    }

    private bool InPlaceEditingChanged()
    {
        return !s_IsQuestDevice && m_TouchKeyboardAllowsInPlaceEditing != TouchScreenKeyboard.isInPlaceEditingAllowed;
    }

    private RangeInt GetInternalSelection()
    {
        int start = Mathf.Min(caretSelectPositionInternal, caretPositionInternal);
        int length = Mathf.Abs(caretSelectPositionInternal - caretPositionInternal);
        return new RangeInt(start, length);
    }

    private void UpdateKeyboardCaret()
    {
        if (m_HideMobileInput && m_Keyboard != null && m_Keyboard.canSetSelection && (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.tvOS))
        {
            m_Keyboard.selection = GetInternalSelection();
        }
    }

    private void UpdateCaretFromKeyboard()
    {
        RangeInt selection = m_Keyboard.selection;
        int start = selection.start;
        int end = selection.end;
        bool flag = false;
        if (caretPositionInternal != start)
        {
            flag = true;
            caretPositionInternal = start;
        }

        if (caretSelectPositionInternal != end)
        {
            caretSelectPositionInternal = end;
            flag = true;
        }

        if (flag)
        {
            m_BlinkStartTime = Time.unscaledTime;
            UpdateLabel();
        }
    }

    protected virtual void LateUpdate()
    {
        if (m_ShouldActivateNextUpdate)
        {
            if (!isFocused)
            {
                ActivateInputFieldInternal();
                m_ShouldActivateNextUpdate = false;
                return;
            }

            m_ShouldActivateNextUpdate = false;
        }

        AssignPositioningIfNeeded();
        if (isFocused && InPlaceEditingChanged())
        {
            if (m_CachedInputRenderer != null)
            {
                using (VertexHelper vertexHelper = new VertexHelper())
                {
                    vertexHelper.FillMesh(mesh);
                }

                m_CachedInputRenderer.SetMesh(mesh);
            }

            DeactivateInputField();
        }

        if (!isFocused || InPlaceEditing())
        {
            return;
        }

        if (m_Keyboard == null || m_Keyboard.status != 0)
        {
            if (m_Keyboard != null)
            {
                if (!m_ReadOnly)
                {
                    this.text = m_Keyboard.text;
                }

                if (m_Keyboard.status == TouchScreenKeyboard.Status.Canceled)
                {
                    m_WasCanceled = true;
                }
                else if (m_Keyboard.status == TouchScreenKeyboard.Status.Done)
                {
                    SendOnSubmit();
                }
            }

            OnDeselect(null);
            return;
        }

        string text = m_Keyboard.text;
        if (m_Text != text)
        {
            if (m_ReadOnly)
            {
                m_Keyboard.text = m_Text;
            }
            else
            {
                m_Text = "";
                for (int i = 0; i < text.Length; i++)
                {
                    char c = text[i];
                    if (c == '\r' || c == '\u0003')
                    {
                        c = '\n';
                    }

                    if (onValidateInput != null)
                    {
                        c = onValidateInput(m_Text, m_Text.Length, c);
                    }
                    else if (characterValidation != 0)
                    {
                        c = Validate(m_Text, m_Text.Length, c);
                    }

                    if (lineType == LineType.MultiLineSubmit && c == '\n')
                    {
                        m_Keyboard.text = m_Text;
                        SendOnSubmit();
                        OnDeselect(null);
                        return;
                    }

                    if (c != 0)
                    {
                        m_Text += c;
                    }
                }

                if (characterLimit > 0 && m_Text.Length > characterLimit)
                {
                    m_Text = m_Text.Substring(0, characterLimit);
                }

                if (m_Keyboard.canGetSelection)
                {
                    UpdateCaretFromKeyboard();
                }
                else
                {
                    int num = (caretSelectPositionInternal = m_Text.Length);
                    caretPositionInternal = num;
                }

                if (m_Text != text)
                {
                    m_Keyboard.text = m_Text;
                }

                SendOnValueChangedAndUpdateLabel();
            }
        }
        else if (m_HideMobileInput && m_Keyboard != null && m_Keyboard.canSetSelection && Application.platform != RuntimePlatform.IPhonePlayer && Application.platform != RuntimePlatform.tvOS)
        {
            m_Keyboard.selection = GetInternalSelection();
        }
        else if (m_Keyboard != null && m_Keyboard.canGetSelection)
        {
            UpdateCaretFromKeyboard();
        }

        if (m_Keyboard.status != 0)
        {
            if (m_Keyboard.status == TouchScreenKeyboard.Status.Canceled)
            {
                m_WasCanceled = true;
            }
            else if (m_Keyboard.status == TouchScreenKeyboard.Status.Done)
            {
                SendOnSubmit();
            }

            OnDeselect(null);
        }
    }

    [Obsolete("This function is no longer used. Please use RectTransformUtility.ScreenPointToLocalPointInRectangle() instead.")]
    public Vector2 ScreenToLocal(Vector2 screen)
    {
        Canvas canvas = m_TextComponent.canvas;
        if (canvas == null)
        {
            return screen;
        }

        Vector3 vector = Vector3.zero;
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            vector = m_TextComponent.transform.InverseTransformPoint(screen);
        }
        else if (canvas.worldCamera != null)
        {
            Ray ray = canvas.worldCamera.ScreenPointToRay(screen);
            new Plane(m_TextComponent.transform.forward, m_TextComponent.transform.position).Raycast(ray, out var enter);
            vector = m_TextComponent.transform.InverseTransformPoint(ray.GetPoint(enter));
        }

        return new Vector2(vector.x, vector.y);
    }

    private int GetUnclampedCharacterLineFromPosition(Vector2 pos, TextGenerator generator)
    {
        if (!multiLine)
        {
            return 0;
        }

        float num = pos.y * m_TextComponent.pixelsPerUnit;
        float num2 = 0f;
        for (int i = 0; i < generator.lineCount; i++)
        {
            float topY = generator.lines[i].topY;
            float num3 = topY - (float)generator.lines[i].height;
            if (num > topY)
            {
                float num4 = topY - num2;
                if (num > topY - 0.5f * num4)
                {
                    return i - 1;
                }

                return i;
            }

            if (num > num3)
            {
                return i;
            }

            num2 = num3;
        }

        return generator.lineCount;
    }

    protected int GetCharacterIndexFromPosition(Vector2 pos)
    {
        TextGenerator cachedTextGenerator = m_TextComponent.cachedTextGenerator;
        if (cachedTextGenerator.lineCount == 0)
        {
            return 0;
        }

        int unclampedCharacterLineFromPosition = GetUnclampedCharacterLineFromPosition(pos, cachedTextGenerator);
        if (unclampedCharacterLineFromPosition < 0)
        {
            return 0;
        }

        if (unclampedCharacterLineFromPosition >= cachedTextGenerator.lineCount)
        {
            return cachedTextGenerator.characterCountVisible;
        }

        int startCharIdx = cachedTextGenerator.lines[unclampedCharacterLineFromPosition].startCharIdx;
        int lineEndPosition = GetLineEndPosition(cachedTextGenerator, unclampedCharacterLineFromPosition);
        for (int i = startCharIdx; i < lineEndPosition && i < cachedTextGenerator.characterCountVisible; i++)
        {
            UICharInfo uICharInfo = cachedTextGenerator.characters[i];
            Vector2 vector = uICharInfo.cursorPos / m_TextComponent.pixelsPerUnit;
            float num = pos.x - vector.x;
            float num2 = vector.x + uICharInfo.charWidth / m_TextComponent.pixelsPerUnit - pos.x;
            if (num < num2)
            {
                return i;
            }
        }

        return lineEndPosition;
    }

    private bool MayDrag(PointerEventData eventData)
    {
        return IsActive() && IsInteractable() && eventData.button == PointerEventData.InputButton.Left && m_TextComponent != null && (InPlaceEditing() || m_HideMobileInput);
    }

    public virtual void OnBeginDrag(PointerEventData eventData)
    {
        if (MayDrag(eventData))
        {
            m_UpdateDrag = true;
        }
    }

    public virtual void OnDrag(PointerEventData eventData)
    {
        if (!MayDrag(eventData))
        {
            return;
        }

        Vector2 position = eventData.position;
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(textComponent.rectTransform, position, eventData.pressEventCamera, out var localPoint);
            caretSelectPositionInternal = GetCharacterIndexFromPosition(localPoint) + m_DrawStart;
            MarkGeometryAsDirty();
            m_DragPositionOutOfBounds = !RectTransformUtility.RectangleContainsScreenPoint(textComponent.rectTransform, eventData.position, eventData.pressEventCamera);
            if (m_DragPositionOutOfBounds && m_DragCoroutine == null)
            {
                m_DragCoroutine = StartCoroutine(MouseDragOutsideRect(eventData));
            }

            UpdateKeyboardCaret();
            eventData.Use();
        }
    }

    private IEnumerator MouseDragOutsideRect(PointerEventData eventData)
    {
        while (m_UpdateDrag && m_DragPositionOutOfBounds)
        {
            Vector2 position = eventData.position;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(textComponent.rectTransform, position, eventData.pressEventCamera, out var localMousePos);
            Rect rect = textComponent.rectTransform.rect;
            if (multiLine)
            {
                if (localMousePos.y > rect.yMax)
                {
                    MoveUp(shift: true, goToFirstChar: true);
                }
                else if (localMousePos.y < rect.yMin)
                {
                    MoveDown(shift: true, goToLastChar: true);
                }
            }
            else if (localMousePos.x < rect.xMin)
            {
                MoveLeft(shift: true, ctrl: false);
            }
            else if (localMousePos.x > rect.xMax)
            {
                MoveRight(shift: true, ctrl: false);
            }

            UpdateLabel();
            float delay = (multiLine ? 0.1f : 0.05f);
            if (m_WaitForSecondsRealtime == null)
            {
                m_WaitForSecondsRealtime = new WaitForSecondsRealtime(delay);
            }
            else
            {
                m_WaitForSecondsRealtime.waitTime = delay;
            }

            yield return m_WaitForSecondsRealtime;
            localMousePos = default(Vector2);
        }

        m_DragCoroutine = null;
    }

    public virtual void OnEndDrag(PointerEventData eventData)
    {
        if (MayDrag(eventData))
        {
            m_UpdateDrag = false;
        }
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (!MayDrag(eventData))
        {
            return;
        }

        EventSystem.current.SetSelectedGameObject(base.gameObject, eventData);
        bool allowInput = m_AllowInput;
        base.OnPointerDown(eventData);
        if (!InPlaceEditing() && (m_Keyboard == null || !m_Keyboard.active))
        {
            OnSelect(eventData);
            return;
        }

        if (allowInput)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(textComponent.rectTransform, eventData.pointerPressRaycast.screenPosition, eventData.pressEventCamera, out var localPoint);
            int num2 = (caretPositionInternal = GetCharacterIndexFromPosition(localPoint) + m_DrawStart);
            caretSelectPositionInternal = num2;
        }

        UpdateLabel();
        UpdateKeyboardCaret();
        eventData.Use();
    }

    protected EditState KeyPressed(Event evt)
    {
        EventModifiers modifiers = evt.modifiers;
        bool flag = ((SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX) ? ((modifiers & EventModifiers.Command) != 0) : ((modifiers & EventModifiers.Control) != 0));
        bool flag2 = (modifiers & EventModifiers.Shift) != 0;
        bool flag3 = (modifiers & EventModifiers.Alt) != 0;
        bool flag4 = flag && !flag3 && !flag2;
        bool flag5 = flag2 && !flag && !flag3;
        switch (evt.keyCode)
        {
            case KeyCode.Backspace:
                Backspace();
                return EditState.Continue;
            case KeyCode.Delete:
                ForwardSpace();
                return EditState.Continue;
            case KeyCode.Home:
                MoveTextStart(flag2);
                return EditState.Continue;
            case KeyCode.End:
                MoveTextEnd(flag2);
                return EditState.Continue;
            case KeyCode.A:
                if (flag4)
                {
                    SelectAll();
                    return EditState.Continue;
                }

                break;
            case KeyCode.C:
                if (flag4)
                {
                    if (inputType != InputType.Password)
                    {
                        clipboard = GetSelectedString();
                    }
                    else
                    {
                        clipboard = "";
                    }

                    return EditState.Continue;
                }

                break;
            case KeyCode.V:
                if (flag4)
                {
                    Append(clipboard);
                    UpdateLabel();
                    return EditState.Continue;
                }

                break;
            case KeyCode.X:
                if (flag4)
                {
                    if (inputType != InputType.Password)
                    {
                        clipboard = GetSelectedString();
                    }
                    else
                    {
                        clipboard = "";
                    }

                    Delete();
                    UpdateTouchKeyboardFromEditChanges();
                    SendOnValueChangedAndUpdateLabel();
                    return EditState.Continue;
                }

                break;
            case KeyCode.Insert:
                if (flag4)
                {
                    if (inputType != InputType.Password)
                    {
                        clipboard = GetSelectedString();
                    }
                    else
                    {
                        clipboard = "";
                    }

                    return EditState.Continue;
                }

                if (flag5)
                {
                    Append(clipboard);
                    UpdateLabel();
                    return EditState.Continue;
                }

                break;
            case KeyCode.LeftArrow:
                MoveLeft(flag2, flag);
                return EditState.Continue;
            case KeyCode.RightArrow:
                MoveRight(flag2, flag);
                return EditState.Continue;
            case KeyCode.UpArrow:
                MoveUp(flag2);
                return EditState.Continue;
            case KeyCode.DownArrow:
                MoveDown(flag2);
                return EditState.Continue;
            case KeyCode.Return:
            case KeyCode.KeypadEnter:
                if (lineType != LineType.MultiLineNewline)
                {
                    return EditState.Finish;
                }

                break;
            case KeyCode.Escape:
                m_WasCanceled = true;
                return EditState.Finish;
        }

        char c = evt.character;
        if (!multiLine && (c == '\t' || c == '\r' || c == '\n'))
        {
            return EditState.Continue;
        }

        if (c == '\r' || c == '\u0003')
        {
            c = '\n';
        }

        if (IsValidChar(c))
        {
            Append(c);
        }

        if (c == '\0' && compositionString.Length > 0)
        {
            UpdateLabel();
        }

        return EditState.Continue;
    }

    private bool IsValidChar(char c)
    {
        switch (c)
        {
            case '\0':
                return false;
            case '\u007f':
                return false;
            default:
                if (c != '\n')
                {
                    return m_TextComponent.font.HasCharacter(c);
                }

                goto case '\t';
            case '\t':
                return true;
        }
    }

    public void ProcessEvent(Event e)
    {
        KeyPressed(e);
    }

    public virtual void OnUpdateSelected(BaseEventData eventData)
    {
        if (!isFocused)
        {
            return;
        }

        bool flag = false;
        while (Event.PopEvent(m_ProcessingEvent))
        {
            if (m_ProcessingEvent.rawType == EventType.KeyDown)
            {
                flag = true;
                if (m_IsCompositionActive && compositionString.Length == 0 && m_ProcessingEvent.character == '\0' && m_ProcessingEvent.modifiers == EventModifiers.None)
                {
                    continue;
                }

                EditState editState = KeyPressed(m_ProcessingEvent);
                if (editState == EditState.Finish)
                {
                    if (!m_WasCanceled)
                    {
                        SendOnSubmit();
                    }

                    DeactivateInputField();
                    continue;
                }

                UpdateLabel();
            }

            EventType type = m_ProcessingEvent.type;
            EventType eventType = type;
            if ((uint)(eventType - 13) <= 1u)
            {
                string commandName = m_ProcessingEvent.commandName;
                string text = commandName;
                if (text == "SelectAll")
                {
                    SelectAll();
                    flag = true;
                }
            }
        }

        if (flag)
        {
            UpdateLabel();
        }

        eventData.Use();
    }

    private string GetSelectedString()
    {
        if (!hasSelection)
        {
            return "";
        }

        int num = caretPositionInternal;
        int num2 = caretSelectPositionInternal;
        if (num > num2)
        {
            int num3 = num;
            num = num2;
            num2 = num3;
        }

        return text.Substring(num, num2 - num);
    }

    private int FindtNextWordBegin()
    {
        if (caretSelectPositionInternal + 1 >= text.Length)
        {
            return text.Length;
        }

        int num = text.IndexOfAny(kSeparators, caretSelectPositionInternal + 1);
        return (num != -1) ? (num + 1) : text.Length;
    }

    private void MoveRight(bool shift, bool ctrl)
    {
        int num2;
        if (hasSelection && !shift)
        {
            num2 = (caretSelectPositionInternal = Mathf.Max(caretPositionInternal, caretSelectPositionInternal));
            caretPositionInternal = num2;
            return;
        }

        int num3 = ((!ctrl) ? (caretSelectPositionInternal + 1) : FindtNextWordBegin());
        if (shift)
        {
            caretSelectPositionInternal = num3;
            return;
        }

        num2 = (caretPositionInternal = num3);
        caretSelectPositionInternal = num2;
    }

    private int FindtPrevWordBegin()
    {
        if (caretSelectPositionInternal - 2 < 0)
        {
            return 0;
        }

        int num = text.LastIndexOfAny(kSeparators, caretSelectPositionInternal - 2);
        return (num != -1) ? (num + 1) : 0;
    }

    private void MoveLeft(bool shift, bool ctrl)
    {
        int num2;
        if (hasSelection && !shift)
        {
            num2 = (caretSelectPositionInternal = Mathf.Min(caretPositionInternal, caretSelectPositionInternal));
            caretPositionInternal = num2;
            return;
        }

        int num3 = ((!ctrl) ? (caretSelectPositionInternal - 1) : FindtPrevWordBegin());
        if (shift)
        {
            caretSelectPositionInternal = num3;
            return;
        }

        num2 = (caretPositionInternal = num3);
        caretSelectPositionInternal = num2;
    }

    private int DetermineCharacterLine(int charPos, TextGenerator generator)
    {
        for (int i = 0; i < generator.lineCount - 1; i++)
        {
            if (generator.lines[i + 1].startCharIdx > charPos)
            {
                return i;
            }
        }

        return generator.lineCount - 1;
    }

    private int LineUpCharacterPosition(int originalPos, bool goToFirstChar)
    {
        if (originalPos >= cachedInputTextGenerator.characters.Count)
        {
            return 0;
        }

        UICharInfo uICharInfo = cachedInputTextGenerator.characters[originalPos];
        int num = DetermineCharacterLine(originalPos, cachedInputTextGenerator);
        if (num <= 0)
        {
            return (!goToFirstChar) ? originalPos : 0;
        }

        int num2 = cachedInputTextGenerator.lines[num].startCharIdx - 1;
        for (int i = cachedInputTextGenerator.lines[num - 1].startCharIdx; i < num2; i++)
        {
            if (cachedInputTextGenerator.characters[i].cursorPos.x >= uICharInfo.cursorPos.x)
            {
                return i;
            }
        }

        return num2;
    }

    private int LineDownCharacterPosition(int originalPos, bool goToLastChar)
    {
        if (originalPos >= cachedInputTextGenerator.characterCountVisible)
        {
            return text.Length;
        }

        UICharInfo uICharInfo = cachedInputTextGenerator.characters[originalPos];
        int num = DetermineCharacterLine(originalPos, cachedInputTextGenerator);
        if (num + 1 >= cachedInputTextGenerator.lineCount)
        {
            return goToLastChar ? text.Length : originalPos;
        }

        int lineEndPosition = GetLineEndPosition(cachedInputTextGenerator, num + 1);
        for (int i = cachedInputTextGenerator.lines[num + 1].startCharIdx; i < lineEndPosition; i++)
        {
            if (cachedInputTextGenerator.characters[i].cursorPos.x >= uICharInfo.cursorPos.x)
            {
                return i;
            }
        }

        return lineEndPosition;
    }

    private void MoveDown(bool shift)
    {
        MoveDown(shift, goToLastChar: true);
    }

    private void MoveDown(bool shift, bool goToLastChar)
    {
        int num2;
        if (hasSelection && !shift)
        {
            num2 = (caretSelectPositionInternal = Mathf.Max(caretPositionInternal, caretSelectPositionInternal));
            caretPositionInternal = num2;
        }

        int num3 = (multiLine ? LineDownCharacterPosition(caretSelectPositionInternal, goToLastChar) : text.Length);
        if (shift)
        {
            caretSelectPositionInternal = num3;
            return;
        }

        num2 = (caretSelectPositionInternal = num3);
        caretPositionInternal = num2;
    }

    private void MoveUp(bool shift)
    {
        MoveUp(shift, goToFirstChar: true);
    }

    private void MoveUp(bool shift, bool goToFirstChar)
    {
        int num2;
        if (hasSelection && !shift)
        {
            num2 = (caretSelectPositionInternal = Mathf.Min(caretPositionInternal, caretSelectPositionInternal));
            caretPositionInternal = num2;
        }

        int num3 = (multiLine ? LineUpCharacterPosition(caretSelectPositionInternal, goToFirstChar) : 0);
        if (shift)
        {
            caretSelectPositionInternal = num3;
            return;
        }

        num2 = (caretPositionInternal = num3);
        caretSelectPositionInternal = num2;
    }

    private void Delete()
    {
        if (!m_ReadOnly && caretPositionInternal != caretSelectPositionInternal)
        {
            if (caretPositionInternal < caretSelectPositionInternal)
            {
                m_Text = text.Substring(0, caretPositionInternal) + text.Substring(caretSelectPositionInternal, text.Length - caretSelectPositionInternal);
                caretSelectPositionInternal = caretPositionInternal;
            }
            else
            {
                m_Text = text.Substring(0, caretSelectPositionInternal) + text.Substring(caretPositionInternal, text.Length - caretPositionInternal);
                caretPositionInternal = caretSelectPositionInternal;
            }
        }
    }

    private void ForwardSpace()
    {
        if (!m_ReadOnly)
        {
            if (hasSelection)
            {
                Delete();
                UpdateTouchKeyboardFromEditChanges();
                SendOnValueChangedAndUpdateLabel();
            }
            else if (caretPositionInternal < text.Length)
            {
                m_Text = text.Remove(caretPositionInternal, 1);
                UpdateTouchKeyboardFromEditChanges();
                SendOnValueChangedAndUpdateLabel();
            }
        }
    }

    private void Backspace()
    {
        if (!m_ReadOnly)
        {
            if (hasSelection)
            {
                Delete();
                UpdateTouchKeyboardFromEditChanges();
                SendOnValueChangedAndUpdateLabel();
            }
            else if (caretPositionInternal > 0 && caretPositionInternal - 1 < text.Length)
            {
                m_Text = text.Remove(caretPositionInternal - 1, 1);
                caretSelectPositionInternal = --caretPositionInternal;
                UpdateTouchKeyboardFromEditChanges();
                SendOnValueChangedAndUpdateLabel();
            }
        }
    }

    private void Insert(char c)
    {
        if (!m_ReadOnly)
        {
            string text = c.ToString();
            Delete();
            if (characterLimit <= 0 || this.text.Length < characterLimit)
            {
                m_Text = this.text.Insert(m_CaretPosition, text);
                caretSelectPositionInternal = (caretPositionInternal += text.Length);
                UpdateTouchKeyboardFromEditChanges();
                SendOnValueChanged();
            }
        }
    }

    private void UpdateTouchKeyboardFromEditChanges()
    {
        if (m_Keyboard != null && InPlaceEditing())
        {
            m_Keyboard.text = m_Text;
        }
    }

    private void SendOnValueChangedAndUpdateLabel()
    {
        SendOnValueChanged();
        UpdateLabel();
    }

    private void SendOnValueChanged()
    {
        UISystemProfilerApi.AddMarker("InputField.value", this);
        if (onValueChanged != null)
        {
            onValueChanged.Invoke(text);
        }
    }

    protected void SendOnEndEdit()
    {
        UISystemProfilerApi.AddMarker("InputField.onEndEdit", this);
        if (onEndEdit != null)
        {
            onEndEdit.Invoke(m_Text);
        }
    }

    protected void SendOnSubmit()
    {
        UISystemProfilerApi.AddMarker("InputField.onSubmit", this);
        if (onSubmit != null)
        {
            onSubmit.Invoke(m_Text);
        }
    }

    protected virtual void Append(string input)
    {
        if (m_ReadOnly || !InPlaceEditing())
        {
            return;
        }

        int i = 0;
        for (int length = input.Length; i < length; i++)
        {
            char c = input[i];
            if (c >= ' ' || c == '\t' || c == '\r' || c == '\n' || c == '\n')
            {
                Append(c);
            }
        }
    }

    protected virtual void Append(char input)
    {
        if (!char.IsSurrogate(input) && !m_ReadOnly && this.text.Length < 16382 && InPlaceEditing())
        {
            int num = Math.Min(selectionFocusPosition, selectionAnchorPosition);
            string text = this.text;
            if (selectionFocusPosition != selectionAnchorPosition)
            {
                text = ((caretPositionInternal >= caretSelectPositionInternal) ? (this.text.Substring(0, caretSelectPositionInternal) + this.text.Substring(caretPositionInternal, this.text.Length - caretPositionInternal)) : (this.text.Substring(0, caretPositionInternal) + this.text.Substring(caretSelectPositionInternal, this.text.Length - caretSelectPositionInternal)));
            }

            if (onValidateInput != null)
            {
                input = onValidateInput(text, num, input);
            }
            else if (characterValidation != 0)
            {
                input = Validate(text, num, input);
            }

            if (input != 0)
            {
                Insert(input);
            }
        }
    }

    protected void UpdateLabel()
    {
        if (m_TextComponent != null && m_TextComponent.font != null && !m_PreventFontCallback)
        {
            m_PreventFontCallback = true;
            string text;
            if (EventSystem.current != null && base.gameObject == EventSystem.current.currentSelectedGameObject && compositionString.Length > 0)
            {
                m_IsCompositionActive = true;
                text = this.text.Substring(0, m_CaretPosition) + compositionString + this.text.Substring(m_CaretPosition);
            }
            else
            {
                m_IsCompositionActive = false;
                text = this.text;
            }

            string text2 = ((inputType != InputType.Password) ? text : new string(asteriskChar, text.Length));
            bool flag = string.IsNullOrEmpty(text);
            if (m_Placeholder != null)
            {
                m_Placeholder.enabled = flag;
            }

            if (!m_AllowInput)
            {
                m_DrawStart = 0;
                m_DrawEnd = m_Text.Length;
            }

            textComponent.SetLayoutDirty();
            if (!flag)
            {
                Vector2 size = m_TextComponent.rectTransform.rect.size;
                TextGenerationSettings generationSettings = m_TextComponent.GetGenerationSettings(size);
                generationSettings.generateOutOfBounds = true;
                cachedInputTextGenerator.PopulateWithErrors(text2, generationSettings, base.gameObject);
                SetDrawRangeToContainCaretPosition(caretSelectPositionInternal);
                text2 = text2.Substring(m_DrawStart, Mathf.Min(m_DrawEnd, text2.Length) - m_DrawStart);
                SetCaretVisible();
            }

            m_TextComponent.text = text2;
            MarkGeometryAsDirty();
            m_PreventFontCallback = false;
        }
    }

    private bool IsSelectionVisible()
    {
        if (m_DrawStart > caretPositionInternal || m_DrawStart > caretSelectPositionInternal)
        {
            return false;
        }

        if (m_DrawEnd < caretPositionInternal || m_DrawEnd < caretSelectPositionInternal)
        {
            return false;
        }

        return true;
    }

    private static int GetLineStartPosition(TextGenerator gen, int line)
    {
        line = Mathf.Clamp(line, 0, gen.lines.Count - 1);
        return gen.lines[line].startCharIdx;
    }

    private static int GetLineEndPosition(TextGenerator gen, int line)
    {
        line = Mathf.Max(line, 0);
        if (line + 1 < gen.lines.Count)
        {
            return gen.lines[line + 1].startCharIdx - 1;
        }

        return gen.characterCountVisible;
    }

    private void SetDrawRangeToContainCaretPosition(int caretPos)
    {
        if (cachedInputTextGenerator.lineCount <= 0)
        {
            return;
        }

        Vector2 size = cachedInputTextGenerator.rectExtents.size;
        if (multiLine)
        {
            IList<UILineInfo> lines = cachedInputTextGenerator.lines;
            int num = DetermineCharacterLine(caretPos, cachedInputTextGenerator);
            if (caretPos > m_DrawEnd)
            {
                m_DrawEnd = GetLineEndPosition(cachedInputTextGenerator, num);
                float num2 = lines[num].topY - (float)lines[num].height;
                if (num == lines.Count - 1)
                {
                    num2 += lines[num].leading;
                }

                int num3;
                for (num3 = num; num3 > 0; num3--)
                {
                    float topY = lines[num3 - 1].topY;
                    if (topY - num2 > size.y)
                    {
                        break;
                    }
                }

                m_DrawStart = GetLineStartPosition(cachedInputTextGenerator, num3);
                return;
            }

            if (caretPos < m_DrawStart)
            {
                m_DrawStart = GetLineStartPosition(cachedInputTextGenerator, num);
            }

            int num4 = DetermineCharacterLine(m_DrawStart, cachedInputTextGenerator);
            int i = num4;
            float topY2 = lines[num4].topY;
            float num5 = lines[i].topY - (float)lines[i].height;
            if (i == lines.Count - 1)
            {
                num5 += lines[i].leading;
            }

            for (; i < lines.Count - 1; i++)
            {
                num5 = lines[i + 1].topY - (float)lines[i + 1].height;
                if (i + 1 == lines.Count - 1)
                {
                    num5 += lines[i + 1].leading;
                }

                if (topY2 - num5 > size.y)
                {
                    break;
                }
            }

            m_DrawEnd = GetLineEndPosition(cachedInputTextGenerator, i);
            while (num4 > 0)
            {
                topY2 = lines[num4 - 1].topY;
                if (topY2 - num5 > size.y)
                {
                    break;
                }

                num4--;
            }

            m_DrawStart = GetLineStartPosition(cachedInputTextGenerator, num4);
            return;
        }

        IList<UICharInfo> characters = cachedInputTextGenerator.characters;
        if (m_DrawEnd > cachedInputTextGenerator.characterCountVisible)
        {
            m_DrawEnd = cachedInputTextGenerator.characterCountVisible;
        }

        float num6 = 0f;
        if (caretPos > m_DrawEnd || (caretPos == m_DrawEnd && m_DrawStart > 0))
        {
            m_DrawEnd = caretPos;
            m_DrawStart = m_DrawEnd - 1;
            while (m_DrawStart >= 0 && !(num6 + characters[m_DrawStart].charWidth > size.x))
            {
                num6 += characters[m_DrawStart].charWidth;
                m_DrawStart--;
            }

            m_DrawStart++;
        }
        else
        {
            if (caretPos < m_DrawStart)
            {
                m_DrawStart = caretPos;
            }

            m_DrawEnd = m_DrawStart;
        }

        while (m_DrawEnd < cachedInputTextGenerator.characterCountVisible)
        {
            num6 += characters[m_DrawEnd].charWidth;
            if (num6 > size.x)
            {
                break;
            }

            m_DrawEnd++;
        }
    }

    public void ForceLabelUpdate()
    {
        UpdateLabel();
    }

    private void MarkGeometryAsDirty()
    {
        if (Application.isPlaying)
        {
#if UNITY_EDITOR
            if (!UnityEditor.PrefabUtility.IsPartOfPrefabAsset(base.gameObject))
            {
                CanvasUpdateRegistry.RegisterCanvasElementForGraphicRebuild(this);
            }
#else
            CanvasUpdateRegistry.RegisterCanvasElementForGraphicRebuild(this);
#endif
        }
    }

    public virtual void Rebuild(CanvasUpdate update)
    {
        if (update == CanvasUpdate.LatePreRender)
        {
            UpdateGeometry();
        }
    }

    public virtual void LayoutComplete()
    {
    }

    public virtual void GraphicUpdateComplete()
    {
    }

    private void UpdateGeometry()
    {
        if (Application.isPlaying && (InPlaceEditing() || shouldHideMobileInput))
        {
            if (m_CachedInputRenderer == null && m_TextComponent != null)
            {
                GameObject gameObject = new GameObject(base.transform.name + " Input Caret", typeof(RectTransform), typeof(CanvasRenderer));
                gameObject.hideFlags = HideFlags.DontSave;
                gameObject.transform.SetParent(m_TextComponent.transform.parent);
                gameObject.transform.SetAsFirstSibling();
                gameObject.layer = base.gameObject.layer;
                caretRectTrans = gameObject.GetComponent<RectTransform>();
                m_CachedInputRenderer = gameObject.GetComponent<CanvasRenderer>();
                m_CachedInputRenderer.SetMaterial(m_TextComponent.GetModifiedMaterial(Graphic.defaultGraphicMaterial), Texture2D.whiteTexture);
                gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                AssignPositioningIfNeeded();
            }

            if (!(m_CachedInputRenderer == null))
            {
                OnFillVBO(mesh);
                m_CachedInputRenderer.SetMesh(mesh);
            }
        }
    }

    private void AssignPositioningIfNeeded()
    {
        if (m_TextComponent != null && caretRectTrans != null && (caretRectTrans.localPosition != m_TextComponent.rectTransform.localPosition || caretRectTrans.localRotation != m_TextComponent.rectTransform.localRotation || caretRectTrans.localScale != m_TextComponent.rectTransform.localScale || caretRectTrans.anchorMin != m_TextComponent.rectTransform.anchorMin || caretRectTrans.anchorMax != m_TextComponent.rectTransform.anchorMax || caretRectTrans.anchoredPosition != m_TextComponent.rectTransform.anchoredPosition || caretRectTrans.sizeDelta != m_TextComponent.rectTransform.sizeDelta || caretRectTrans.pivot != m_TextComponent.rectTransform.pivot))
        {
            caretRectTrans.localPosition = m_TextComponent.rectTransform.localPosition;
            caretRectTrans.localRotation = m_TextComponent.rectTransform.localRotation;
            caretRectTrans.localScale = m_TextComponent.rectTransform.localScale;
            caretRectTrans.anchorMin = m_TextComponent.rectTransform.anchorMin;
            caretRectTrans.anchorMax = m_TextComponent.rectTransform.anchorMax;
            caretRectTrans.anchoredPosition = m_TextComponent.rectTransform.anchoredPosition;
            caretRectTrans.sizeDelta = m_TextComponent.rectTransform.sizeDelta;
            caretRectTrans.pivot = m_TextComponent.rectTransform.pivot;
        }
    }

    private void OnFillVBO(Mesh vbo)
    {
        using VertexHelper vertexHelper = new VertexHelper();
        if (!isFocused)
        {
            vertexHelper.FillMesh(vbo);
            return;
        }

        Vector2 roundingOffset = m_TextComponent.PixelAdjustPoint(Vector2.zero);
        if (!hasSelection)
        {
            GenerateCaret(vertexHelper, roundingOffset);
        }
        else
        {
            GenerateHighlight(vertexHelper, roundingOffset);
        }

        vertexHelper.FillMesh(vbo);
    }

    private void GenerateCaret(VertexHelper vbo, Vector2 roundingOffset)
    {
        if (!m_CaretVisible)
        {
            return;
        }

        if (m_CursorVerts == null)
        {
            CreateCursorVerts();
        }

        float num = m_CaretWidth;
        int num2 = Mathf.Max(0, caretPositionInternal - m_DrawStart);
        TextGenerator cachedTextGenerator = m_TextComponent.cachedTextGenerator;
        if (cachedTextGenerator == null || cachedTextGenerator.lineCount == 0)
        {
            return;
        }

        Vector2 zero = Vector2.zero;
        if (num2 < cachedTextGenerator.characters.Count)
        {
            zero.x = cachedTextGenerator.characters[num2].cursorPos.x;
        }

        zero.x /= m_TextComponent.pixelsPerUnit;
        if (zero.x > m_TextComponent.rectTransform.rect.xMax)
        {
            zero.x = m_TextComponent.rectTransform.rect.xMax;
        }

        int index = DetermineCharacterLine(num2, cachedTextGenerator);
        zero.y = cachedTextGenerator.lines[index].topY / m_TextComponent.pixelsPerUnit;
        float num3 = (float)cachedTextGenerator.lines[index].height / m_TextComponent.pixelsPerUnit;
        for (int i = 0; i < m_CursorVerts.Length; i++)
        {
            m_CursorVerts[i].color = caretColor;
        }

        m_CursorVerts[0].position = new Vector3(zero.x, zero.y - num3, 0f);
        m_CursorVerts[1].position = new Vector3(zero.x + num, zero.y - num3, 0f);
        m_CursorVerts[2].position = new Vector3(zero.x + num, zero.y, 0f);
        m_CursorVerts[3].position = new Vector3(zero.x, zero.y, 0f);
        if (roundingOffset != Vector2.zero)
        {
            for (int j = 0; j < m_CursorVerts.Length; j++)
            {
                UIVertex uIVertex = m_CursorVerts[j];
                uIVertex.position.x += roundingOffset.x;
                uIVertex.position.y += roundingOffset.y;
            }
        }

        vbo.AddUIVertexQuad(m_CursorVerts);
        int num4 = Screen.height;
        int targetDisplay = m_TextComponent.canvas.targetDisplay;
        if (targetDisplay > 0 && targetDisplay < Display.displays.Length)
        {
            num4 = Display.displays[targetDisplay].renderingHeight;
        }

        Camera cam = ((m_TextComponent.canvas.renderMode != 0) ? m_TextComponent.canvas.worldCamera : null);
        Vector3 worldPoint = m_CachedInputRenderer.gameObject.transform.TransformPoint(m_CursorVerts[0].position);
        Vector2 compositionCursorPos = RectTransformUtility.WorldToScreenPoint(cam, worldPoint);
        compositionCursorPos.y = (float)num4 - compositionCursorPos.y;
        if (input != null)
        {
            input.compositionCursorPos = compositionCursorPos;
        }
    }

    private void CreateCursorVerts()
    {
        m_CursorVerts = new UIVertex[4];
        for (int i = 0; i < m_CursorVerts.Length; i++)
        {
            m_CursorVerts[i] = UIVertex.simpleVert;
            m_CursorVerts[i].uv0 = Vector2.zero;
        }
    }

    private void GenerateHighlight(VertexHelper vbo, Vector2 roundingOffset)
    {
        int num = Mathf.Max(0, caretPositionInternal - m_DrawStart);
        int num2 = Mathf.Max(0, caretSelectPositionInternal - m_DrawStart);
        if (num > num2)
        {
            int num3 = num;
            num = num2;
            num2 = num3;
        }

        num2--;
        TextGenerator cachedTextGenerator = m_TextComponent.cachedTextGenerator;
        if (cachedTextGenerator.lineCount <= 0)
        {
            return;
        }

        int num4 = DetermineCharacterLine(num, cachedTextGenerator);
        int lineEndPosition = GetLineEndPosition(cachedTextGenerator, num4);
        UIVertex simpleVert = UIVertex.simpleVert;
        simpleVert.uv0 = Vector2.zero;
        simpleVert.color = selectionColor;
        for (int i = num; i <= num2 && i < cachedTextGenerator.characterCount; i++)
        {
            if (i == lineEndPosition || i == num2)
            {
                UICharInfo uICharInfo = cachedTextGenerator.characters[num];
                UICharInfo uICharInfo2 = cachedTextGenerator.characters[i];
                Vector2 vector = new Vector2(uICharInfo.cursorPos.x / m_TextComponent.pixelsPerUnit, cachedTextGenerator.lines[num4].topY / m_TextComponent.pixelsPerUnit);
                Vector2 vector2 = new Vector2((uICharInfo2.cursorPos.x + uICharInfo2.charWidth) / m_TextComponent.pixelsPerUnit, vector.y - (float)cachedTextGenerator.lines[num4].height / m_TextComponent.pixelsPerUnit);
                if (vector2.x > m_TextComponent.rectTransform.rect.xMax || vector2.x < m_TextComponent.rectTransform.rect.xMin)
                {
                    vector2.x = m_TextComponent.rectTransform.rect.xMax;
                }

                int currentVertCount = vbo.currentVertCount;
                simpleVert.position = new Vector3(vector.x, vector2.y, 0f) + (Vector3)roundingOffset;
                vbo.AddVert(simpleVert);
                simpleVert.position = new Vector3(vector2.x, vector2.y, 0f) + (Vector3)roundingOffset;
                vbo.AddVert(simpleVert);
                simpleVert.position = new Vector3(vector2.x, vector.y, 0f) + (Vector3)roundingOffset;
                vbo.AddVert(simpleVert);
                simpleVert.position = new Vector3(vector.x, vector.y, 0f) + (Vector3)roundingOffset;
                vbo.AddVert(simpleVert);
                vbo.AddTriangle(currentVertCount, currentVertCount + 1, currentVertCount + 2);
                vbo.AddTriangle(currentVertCount + 2, currentVertCount + 3, currentVertCount);
                num = i + 1;
                num4++;
                lineEndPosition = GetLineEndPosition(cachedTextGenerator, num4);
            }
        }
    }

    protected char Validate(string text, int pos, char ch)
    {
        if (characterValidation == CharacterValidation.None || !base.enabled)
        {
            return ch;
        }

        if (characterValidation == CharacterValidation.Integer || characterValidation == CharacterValidation.Decimal)
        {
            bool flag = pos == 0 && text.Length > 0 && text[0] == '-';
            bool flag2 = text.Length > 0 && text[0] == '-' && ((caretPositionInternal == 0 && caretSelectPositionInternal > 0) || (caretSelectPositionInternal == 0 && caretPositionInternal > 0));
            bool flag3 = caretPositionInternal == 0 || caretSelectPositionInternal == 0;
            if (!flag || flag2)
            {
                if (ch >= '0' && ch <= '9')
                {
                    return ch;
                }

                if (ch == '-' && (pos == 0 || flag3))
                {
                    return ch;
                }

                if ((ch == '.' || ch == ',') && characterValidation == CharacterValidation.Decimal && text.IndexOfAny(new char[2] { '.', ',' }) == -1)
                {
                    return ch;
                }
            }
        }
        else if (characterValidation == CharacterValidation.Alphanumeric)
        {
            if (ch >= 'A' && ch <= 'Z')
            {
                return ch;
            }

            if (ch >= 'a' && ch <= 'z')
            {
                return ch;
            }

            if (ch >= '0' && ch <= '9')
            {
                return ch;
            }
        }
        else if (characterValidation == CharacterValidation.Name)
        {
            if (char.IsLetter(ch))
            {
                if (char.IsLower(ch) && (pos == 0 || text[pos - 1] == ' ' || text[pos - 1] == '-'))
                {
                    return char.ToUpper(ch);
                }

                if (char.IsUpper(ch) && pos > 0 && text[pos - 1] != ' ' && text[pos - 1] != '\'' && text[pos - 1] != '-')
                {
                    return char.ToLower(ch);
                }

                return ch;
            }

            if (ch == '\'' && !text.Contains("'") && (pos <= 0 || (text[pos - 1] != ' ' && text[pos - 1] != '\'' && text[pos - 1] != '-')) && (pos >= text.Length || (text[pos] != ' ' && text[pos] != '\'' && text[pos] != '-')))
            {
                return ch;
            }

            if ((ch == ' ' || ch == '-') && pos != 0 && (pos <= 0 || (text[pos - 1] != ' ' && text[pos - 1] != '\'' && text[pos - 1] != '-')) && (pos >= text.Length || (text[pos] != ' ' && text[pos] != '\'' && text[pos - 1] != '-')))
            {
                return ch;
            }
        }
        else if (characterValidation == CharacterValidation.EmailAddress)
        {
            if (ch >= 'A' && ch <= 'Z')
            {
                return ch;
            }

            if (ch >= 'a' && ch <= 'z')
            {
                return ch;
            }

            if (ch >= '0' && ch <= '9')
            {
                return ch;
            }

            if (ch == '@' && text.IndexOf('@') == -1)
            {
                return ch;
            }

            if ("!#$%&'*+-/=?^_`{|}~".IndexOf(ch) != -1)
            {
                return ch;
            }

            if (ch == '.')
            {
                char c = ((text.Length > 0) ? text[Mathf.Clamp(pos, 0, text.Length - 1)] : ' ');
                char c2 = ((text.Length > 0) ? text[Mathf.Clamp(pos + 1, 0, text.Length - 1)] : '\n');
                if (c != '.' && c2 != '.')
                {
                    return ch;
                }
            }
        }

        return '\0';
    }

    public void ActivateInputField()
    {
        if (!(m_TextComponent == null) && !(m_TextComponent.font == null) && IsActive() && IsInteractable())
        {
            if (isFocused && m_Keyboard != null && !m_Keyboard.active)
            {
                m_Keyboard.active = true;
                m_Keyboard.text = m_Text;
            }

            m_ShouldActivateNextUpdate = true;
        }
    }

    private void ActivateInputFieldInternal()
    {
        if (EventSystem.current == null)
        {
            return;
        }

        if (EventSystem.current.currentSelectedGameObject != base.gameObject)
        {
            EventSystem.current.SetSelectedGameObject(base.gameObject);
        }

        m_TouchKeyboardAllowsInPlaceEditing = !s_IsQuestDevice && TouchScreenKeyboard.isInPlaceEditingAllowed;
        if (TouchScreenKeyboardShouldBeUsed())
        {
            if (input != null && input.touchSupported)
            {
                TouchScreenKeyboard.hideInput = shouldHideMobileInput;
            }

            m_Keyboard = ((inputType == InputType.Password) ? TouchScreenKeyboard.Open(m_Text, keyboardType, autocorrection: false, multiLine, secure: true, alert: false, "", characterLimit) : TouchScreenKeyboard.Open(m_Text, keyboardType, inputType == InputType.AutoCorrect, multiLine, secure: false, alert: false, "", characterLimit));
            if (!m_TouchKeyboardAllowsInPlaceEditing)
            {
                MoveTextEnd(shift: false);
            }
        }

        if (!TouchScreenKeyboard.isSupported || m_TouchKeyboardAllowsInPlaceEditing)
        {
            if (input != null)
            {
                input.imeCompositionMode = IMECompositionMode.On;
            }

            OnFocus();
        }

        m_AllowInput = true;
        m_OriginalText = text;
        m_WasCanceled = false;
        SetCaretVisible();
        UpdateLabel();
    }

    public override void OnSelect(BaseEventData eventData)
    {
        base.OnSelect(eventData);
        if (shouldActivateOnSelect)
        {
            ActivateInputField();
        }
    }

    public virtual void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            ActivateInputField();
        }
    }

    public void DeactivateInputField(bool canHideKeyboard = true)
    {
        if (!m_AllowInput)
        {
            return;
        }

        m_HasDoneFocusTransition = false;
        m_AllowInput = false;
        if (m_Placeholder != null)
        {
            m_Placeholder.enabled = string.IsNullOrEmpty(m_Text);
        }

        if (m_TextComponent != null && IsInteractable())
        {
            if (m_WasCanceled)
            {
                text = m_OriginalText;
            }

            SendOnEndEdit();
            if (m_Keyboard != null && canHideKeyboard)
            {
                m_Keyboard.active = false;
                m_Keyboard = null;
            }

            m_CaretPosition = (m_CaretSelectPosition = 0);
            if (input != null)
            {
                input.imeCompositionMode = IMECompositionMode.Auto;
            }
        }

        MarkGeometryAsDirty();
    }

    public void HideKeyboard()
    {
        if (m_Keyboard != null)
        {
            m_Keyboard.active = false;
            m_Keyboard = null;
        }
    }

    /// <summary>
    /// 获取当前指针位置下的所有 hovered 对象（跨平台兼容）
    /// </summary>
    private List<GameObject> GetCurrentHoveredObjects(PointerEventData pointerEventData)
    {
        List<GameObject> hoveredObjects = new List<GameObject>();

        // 方法1: 从 PointerEventData 获取 hovered（编辑器有效）
        if (pointerEventData != null && pointerEventData.hovered != null && pointerEventData.hovered.Count > 0)
        {
            hoveredObjects.AddRange(pointerEventData.hovered);
            return hoveredObjects;
        }

        // 方法2: 从 PointerInputModule 获取所有指针数据的 hovered（需要反射）
        if (EventSystem.current != null && EventSystem.current.currentInputModule != null)
        {
            var inputModule = EventSystem.current.currentInputModule;
            if (inputModule is PointerInputModule pointerInputModule)
            {
                // 使用反射获取 m_PointerData 字典
                var field = typeof(PointerInputModule).GetField("m_PointerData",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    var pointerData = field.GetValue(pointerInputModule) as System.Collections.IDictionary;
                    if (pointerData != null)
                    {
                        foreach (System.Collections.DictionaryEntry entry in pointerData)
                        {
                            if (entry.Value is PointerEventData ped && ped.hovered != null)
                            {
                                foreach (var obj in ped.hovered)
                                {
                                    if (!hoveredObjects.Contains(obj))
                                    {
                                        hoveredObjects.Add(obj);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        // 方法3: 如果前两种方法都失败，使用 GraphicRaycaster 进行射线检测
        if (hoveredObjects.Count == 0 && EventSystem.current != null)
        {
            Vector2 screenPosition = pointerEventData != null ? pointerEventData.position : Input.mousePosition;
            PointerEventData raycastEventData = new PointerEventData(EventSystem.current)
            {
                position = screenPosition
            };

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(raycastEventData, results);

            foreach (var result in results)
            {
                if (result.gameObject != null && !hoveredObjects.Contains(result.gameObject))
                {
                    hoveredObjects.Add(result.gameObject);
                }
            }
        }

        return hoveredObjects;
    }

    public override void OnDeselect(BaseEventData eventData)
    {
        // 将 BaseEventData 转换为 PointerEventData 以访问 hovered 属性
        PointerEventData pointerEventData = eventData as PointerEventData;

        bool canHideKeyboard = true;
        // 获取当前 hovered 对象（跨平台兼容）
        var hovered = GetCurrentHoveredObjects(pointerEventData);
        foreach (var obj in hovered)
        {
            if (selectIncludeObjects.Contains(obj))
            {
                canHideKeyboard = false;
            }
        }
        Debug.LogError("canHideKeyboard: " + canHideKeyboard);
        DeactivateInputField(canHideKeyboard);
        base.OnDeselect(eventData);
    }

    public virtual void OnSubmit(BaseEventData eventData)
    {
        if (IsActive() && IsInteractable() && !isFocused)
        {
            m_ShouldActivateNextUpdate = true;
        }
    }

    private void EnforceContentType()
    {
        switch (contentType)
        {
            case ContentType.Standard:
                m_InputType = InputType.Standard;
                m_KeyboardType = TouchScreenKeyboardType.Default;
                m_CharacterValidation = CharacterValidation.None;
                break;
            case ContentType.Autocorrected:
                m_InputType = InputType.AutoCorrect;
                m_KeyboardType = TouchScreenKeyboardType.Default;
                m_CharacterValidation = CharacterValidation.None;
                break;
            case ContentType.IntegerNumber:
                m_LineType = LineType.SingleLine;
                m_InputType = InputType.Standard;
                m_KeyboardType = TouchScreenKeyboardType.NumberPad;
                m_CharacterValidation = CharacterValidation.Integer;
                break;
            case ContentType.DecimalNumber:
                m_LineType = LineType.SingleLine;
                m_InputType = InputType.Standard;
                m_KeyboardType = TouchScreenKeyboardType.NumbersAndPunctuation;
                m_CharacterValidation = CharacterValidation.Decimal;
                break;
            case ContentType.Alphanumeric:
                m_LineType = LineType.SingleLine;
                m_InputType = InputType.Standard;
                m_KeyboardType = TouchScreenKeyboardType.ASCIICapable;
                m_CharacterValidation = CharacterValidation.Alphanumeric;
                break;
            case ContentType.Name:
                m_LineType = LineType.SingleLine;
                m_InputType = InputType.Standard;
                m_KeyboardType = TouchScreenKeyboardType.NamePhonePad;
                m_CharacterValidation = CharacterValidation.Name;
                break;
            case ContentType.EmailAddress:
                m_LineType = LineType.SingleLine;
                m_InputType = InputType.Standard;
                m_KeyboardType = TouchScreenKeyboardType.EmailAddress;
                m_CharacterValidation = CharacterValidation.EmailAddress;
                break;
            case ContentType.Password:
                m_LineType = LineType.SingleLine;
                m_InputType = InputType.Password;
                m_KeyboardType = TouchScreenKeyboardType.Default;
                m_CharacterValidation = CharacterValidation.None;
                break;
            case ContentType.Pin:
                m_LineType = LineType.SingleLine;
                m_InputType = InputType.Password;
                m_KeyboardType = TouchScreenKeyboardType.NumberPad;
                m_CharacterValidation = CharacterValidation.Integer;
                break;
        }

        EnforceTextHOverflow();
    }

    private void EnforceTextHOverflow()
    {
        if (m_TextComponent != null)
        {
            if (multiLine)
            {
                m_TextComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            }
            else
            {
                m_TextComponent.horizontalOverflow = HorizontalWrapMode.Overflow;
            }
        }
    }

    private void SetToCustomIfContentTypeIsNot(params ContentType[] allowedContentTypes)
    {
        if (contentType == ContentType.Custom)
        {
            return;
        }

        for (int i = 0; i < allowedContentTypes.Length; i++)
        {
            if (contentType == allowedContentTypes[i])
            {
                return;
            }
        }

        contentType = ContentType.Custom;
    }

    private void SetToCustom()
    {
        if (contentType != ContentType.Custom)
        {
            contentType = ContentType.Custom;
        }
    }

    protected override void DoStateTransition(SelectionState state, bool instant)
    {
        if (m_HasDoneFocusTransition)
        {
            state = SelectionState.Selected;
        }
        else if (state == SelectionState.Pressed)
        {
            m_HasDoneFocusTransition = true;
        }

        base.DoStateTransition(state, instant);
    }

    public virtual void CalculateLayoutInputHorizontal()
    {
    }

    public virtual void CalculateLayoutInputVertical()
    {
    }



    public static bool SetColor(ref Color currentValue, Color newValue)
    {
        if (currentValue.r == newValue.r && currentValue.g == newValue.g && currentValue.b == newValue.b && currentValue.a == newValue.a)
        {
            return false;
        }

        currentValue = newValue;
        return true;
    }

    public static bool SetStruct<T>(ref T currentValue, T newValue) where T : struct
    {
        if (EqualityComparer<T>.Default.Equals(currentValue, newValue))
        {
            return false;
        }

        currentValue = newValue;
        return true;
    }

    public static bool SetClass<T>(ref T currentValue, T newValue) where T : class
    {
        if ((currentValue == null && newValue == null) || (currentValue != null && currentValue.Equals(newValue)))
        {
            return false;
        }

        currentValue = newValue;
        return true;
    }















    AndroidJavaClass unityClass;
    AndroidJavaObject currentActivity;
    AndroidJavaObject unityPlayer;

    void Init()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            if (unityClass != null)
            {
                currentActivity = unityClass.GetStatic<AndroidJavaObject>("currentActivity");
                if (currentActivity != null)
                {
                    //unityPlayer = currentActivity.Get<AndroidJavaObject>("mUnityPlayer");
                    unityPlayer = currentActivity.Get<AndroidJavaObject>("gameView");
                    if (unityPlayer == null)
                    {
                        unityPlayer = currentActivity.Call<AndroidJavaObject>("getUnityPlayer");
                    }
                }
            }
        }

        TouchScreenKeyboard.hideInput = true;
    }

    public int GetKeyboardHeight()
    {
        if (touchScreenKeyboard != null)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                if (unityClass == null)
                {
                    Debug.LogError("unity unityClass is null");
                    return 0;
                }
                if (currentActivity == null)
                {
                    Debug.LogError("unity currentActivity is null");
                    return 0;
                }
                if (unityPlayer == null)
                {
                    Debug.LogError("unity unityPlayer is null");
                    return 0;
                }
            }

            var _h = _GetKeyboardHeight(true, 2436); //* 2436 / Screen.height;

#if UNITY_IOS
            if (_h > 0)
            {
                _h -= 180; //加上mobileInput的高度
                           // _h += GetIOSKeyboardAccessoryViewHeight(); //加上mobileInput的高度
            }
#endif
            return _h;
        }
        return 0;
    }



    /// <summary>
    /// ��ȡ��׿ƽ̨�ϼ��̵ĸ߶�
    /// </summary>
    /// <returns></returns>
    public int _GetKeyboardHeight(bool includeInput, int screenHeight)
    {
        if (Application.platform == RuntimePlatform.IPhonePlayer)
        {
            //Debug.LogError("keyboard1 " + TouchScreenKeyboard.area.height);
            var val = (int)TouchScreenKeyboard.area.height * screenHeight / GetViewHeight();
            //Debug.LogError("keyboard2 " + val);
            return val;
        }
        if (Application.platform == RuntimePlatform.Android)
        {
            var view = unityPlayer.Call<AndroidJavaObject>("getView");
            var dialog = unityPlayer.Get<AndroidJavaObject>("mSoftInputDialog");

            if (view == null || dialog == null)
                return 0;

            var decorHeight = 0;

            if (includeInput)
            {
                var decorView = dialog.Call<AndroidJavaObject>("getWindow").Call<AndroidJavaObject>("getDecorView");

                if (decorView != null)
                    decorHeight = decorView.Call<int>("getHeight");
            }

            using (var rect = new AndroidJavaObject("android.graphics.Rect"))
            {
                view.Call("getWindowVisibleDisplayFrame", rect);
                int androidRectH = rect.Call<int>("height"); // ���ǷǼ��̲��ֵĸ߶ȡ�       
                int h = Math.Abs(GetViewHeight() - androidRectH); // �����̸߶�         
                int keyboardHeight = h + decorHeight;
                int finalres = keyboardHeight * screenHeight / GetViewHeight();
                //Debug.LogFormat(
                //    "���찲׿ƽ̨��⵽���̸߶ȣ�{0},Screen.height: {1},�Ǽ��̲��ֵĸ߶�: {2},�����ĸ߶�: {3},UIʵ�ʸ߶�: {4},���ռ��̸߶�: {5},��׿����ʾ�߶�: {6}",
                //    keyboardHeight,
                //    Screen.height, androidRectH, decorHeight, screenHeight, finalres, GetViewHeight());
                return finalres;
            }
        }
        return 0;
    }

    /// <summary>
    /// ��ȡ��Ļ�߶�
    /// </summary>
    /// <returns></returns>
    public int GetViewHeight()
    {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        return Screen.height;
#elif UNITY_STANDALONE_OSX || UNITY_IPHONE
        return  Screen.height;
#elif UNITY_ANDROID
            var view = unityPlayer.Call<AndroidJavaObject>("getView");
            if (view == null)
                return 0;
            int result = view.Call<int>("getHeight");
            //Debug.LogFormat("Screen.height: {0},��Ļ�ֱ���: {1}", Screen.height, result);
            return result;
#endif
    }

    /// <summary>
    /// 获取iOS键盘上方空白区域的高度（当hideInput为true时）
    /// 当hideInput为true时，iOS会在键盘上方创建一个输入辅助视图（input accessory view），
    /// 这个视图会显示一个空白区域，用于显示输入框但被隐藏了
    /// </summary>
    /// <returns>空白区域的高度（像素），如果键盘未显示或hideInput为false则返回0</returns>
    public int GetIOSKeyboardAccessoryViewHeight()
    {
        if (Application.platform != RuntimePlatform.IPhonePlayer)
        {
            return 0;
        }

        if (touchScreenKeyboard == null)
        {
            return 0;
        }

        if (!shouldHideMobileInput)
        {
            return 0;
        }

        // 获取键盘区域信息
        var keyboardArea = TouchScreenKeyboard.area;

        // 在iOS中，当hideInput为true时，系统会在键盘上方创建一个输入辅助视图
        // TouchScreenKeyboard.area返回的Rect中：
        // - area.y 表示键盘底部到屏幕底部的距离（在逻辑像素中）
        // - area.height 表示键盘的高度（在逻辑像素中）
        // - 键盘顶部到屏幕底部的距离 = area.y + area.height

        // 获取屏幕高度（逻辑像素）
        int viewHeight = GetViewHeight();
        if (viewHeight <= 0)
        {
            return 0;
        }

        // 计算键盘顶部到屏幕底部的总高度（逻辑像素）
        // 这个总高度包括键盘本身和上方的输入辅助视图
        float totalHeightFromBottom = keyboardArea.y + keyboardArea.height;

        // 转换为实际像素（根据屏幕分辨率）
        int screenHeight = Screen.height;
        float totalHeightPixels = totalHeightFromBottom * screenHeight / viewHeight;

        // 键盘本身的高度（实际像素）
        float keyboardHeightPixels = keyboardArea.height * screenHeight / viewHeight;

        // 空白区域（输入辅助视图）的高度 = 总高度 - 键盘高度
        float accessoryViewHeight = totalHeightPixels - keyboardHeightPixels;

        // 确保返回值不为负数
        return Mathf.Max(0, Mathf.RoundToInt(accessoryViewHeight));
    }

}
