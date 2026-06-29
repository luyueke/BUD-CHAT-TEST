using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEngine.UI.Toggle;

public class GoToggle : Selectable, IPointerClickHandler
{

    public ToggleEvent onValueChanged = new ToggleEvent();

    // Whether the toggle is on
    [Tooltip("Is the toggle currently on or off?")]
    [SerializeField]
    private bool m_IsOn;

    [SerializeField]
    private GameObject GameObjectOn;
    [SerializeField]
    private GameObject GameObjectOff;

    [SerializeField]
    private GoToggleGroup m_Group;

    public bool isOn
    {
        get { return m_IsOn; }

        set
        {
            Set(value);
        }
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        if (m_Group == null)
        {
            if (GameObjectOn != null)
            {
                GameObjectOn.SetActive(m_IsOn);
            }
            if (GameObjectOff != null)
            {
                GameObjectOff.SetActive(!m_IsOn);
            }
            return;
        }

        // 编辑模式下 Awake 不执行，m_Group.Toggles 为空，必须手动查找
        // 从 m_Group 的根节点向下搜索：在 Prefab Stage 和普通场景中均可正常工作
        // 不用 FindObjectsOfType（Prefab Stage 中只搜主场景）
        // 不用 PrefabStageUtility.prefabContentsRoot（在 Awake/OnEnable 期间会抛 InvalidOperationException）
        var allToggles = m_Group.transform.root.GetComponentsInChildren<GoToggle>(true);

        // AllowSwitchOff=false：若当前改为 false 且组内无其他 Toggle 处于 On，则回滚
        if (!m_IsOn && !m_Group.allowSwitchOff)
        {
            bool anyOtherOn = false;
            foreach (var t in allToggles)
            {
                if (t != this && t.m_Group == m_Group && t.m_IsOn)
                {
                    anyOtherOn = true;
                    break;
                }
            }
            if (!anyOtherOn)
            {
                m_IsOn = true;
            }
        }

        if (GameObjectOn != null)
        {
            GameObjectOn.SetActive(m_IsOn);
        }
        if (GameObjectOff != null)
        {
            GameObjectOff.SetActive(!m_IsOn);
        }

        // 互斥：当前变为 On，关闭同组其他 Toggle
        if (m_IsOn)
        {
            foreach (var t in allToggles)
            {
                if (t != this && t.m_Group == m_Group && t.m_IsOn)
                {
                    t.m_IsOn = false;
                    if (t.GameObjectOn != null)
                    {
                        t.GameObjectOn.SetActive(false);
                    }
                    if (t.GameObjectOff != null)
                    {
                        t.GameObjectOff.SetActive(true);
                    }
                    UnityEditor.EditorUtility.SetDirty(t);
                }
            }
        }
    }
#endif

    protected override void Awake()
    {
        base.Awake();
        if (m_Group != null)
        {
            m_Group.RegisterToggle(this);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (m_Group != null)
        {
            m_Group.UnregisterToggle(this);
        }
    }

    public void SetGroup(GoToggleGroup group)
    {
        if (m_Group != null)
        {
            m_Group.UnregisterToggle(this);
        }

        m_Group = group;

        if (m_Group != null)
        {
            m_Group.RegisterToggle(this);
        }
    }

    public void Set(bool value, bool sendCallback = true)
    {
        if (m_IsOn == value)
            return;
        m_IsOn = value;
 
        GameObjectOn.SetActive(m_IsOn);
        GameObjectOff.SetActive(!m_IsOn);

        if (m_IsOn && m_Group != null)
        {
            m_Group.NotifyToggleOn(this);
        }

        if (sendCallback)
        {
            UISystemProfilerApi.AddMarker("Toggle.value", this);
            onValueChanged.Invoke(m_IsOn);
        }
    }

    private void InternalToggle()
    {
        if (!IsActive() || !IsInteractable())
            return;

        if (m_Group != null && !m_Group.allowSwitchOff && m_IsOn)
            return;

        isOn = !isOn;
    }

    /// <summary>
    /// React to clicks.
    /// </summary>
    public virtual void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        InternalToggle();
    }

}
