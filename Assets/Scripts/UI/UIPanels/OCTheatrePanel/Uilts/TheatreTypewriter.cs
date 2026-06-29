using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class TheatreTypewriter : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private SuperTextMesh targetText;

    [Header("Typing")]
    [SerializeField] private bool enableTypewriter = true;
    [SerializeField] [Min(0f)] private float charInterval = 0.05f;

    [Header("Events")]
    [SerializeField] private UnityEvent onTypingComplete;
    [SerializeField] private CharTypedEvent onCharTyped;

    public event Action<char, int> CharTyped;
    public event Action<string> TypingComplete;

    public bool EnableTypewriter
    {
        get => enableTypewriter;
        set => enableTypewriter = value;
    }

    public float CharInterval
    {
        get => charInterval;
        set => charInterval = Mathf.Max(0f, value);
    }

    public bool IsTyping => typingCoroutine != null;

    private Coroutine typingCoroutine;
    private string currentContent = string.Empty;
    private int typeSessionId;

    [Serializable]
    public class CharTypedEvent : UnityEvent<char> { }

    public void Play(string content)
    {
        currentContent = content ?? string.Empty;
        typeSessionId++;

        StopCurrentTyping();

        if (targetText == null)
        {
            Debug.LogError($"{nameof(TheatreTypewriter)} targetText is null.", this);
            return;
        }

        if (!enableTypewriter || charInterval <= 0f)
        {
            targetText.text = currentContent;
            NotifyTypingComplete();
            return;
        }

        typingCoroutine = StartCoroutine(TypeText(typeSessionId, currentContent));
    }

    public void CompleteImmediately()
    {
        if (targetText == null) return;
        if (!IsTyping) return;

        StopCurrentTyping();
        targetText.text = currentContent;
        NotifyTypingComplete();
    }

    private IEnumerator TypeText(int sessionId, string content)
    {
        targetText.text = string.Empty;
        if (string.IsNullOrEmpty(content))
        {
            typingCoroutine = null;
            NotifyTypingComplete();
            yield break;
        }

        for (int i = 0; i < content.Length; i++)
        {
            if (sessionId != typeSessionId) yield break;

            char ch = content[i];
            targetText.text += ch;
            onCharTyped?.Invoke(ch);
            CharTyped?.Invoke(ch, i);
            yield return new WaitForSeconds(charInterval);
        }

        typingCoroutine = null;
        NotifyTypingComplete();
    }

    private void NotifyTypingComplete()
    {
        onTypingComplete?.Invoke();
        TypingComplete?.Invoke(currentContent);
    }

    private void StopCurrentTyping()
    {
        if (typingCoroutine == null) return;
        StopCoroutine(typingCoroutine);
        typingCoroutine = null;
    }
}
