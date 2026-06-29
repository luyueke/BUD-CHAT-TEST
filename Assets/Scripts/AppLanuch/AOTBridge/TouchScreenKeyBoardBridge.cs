using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UI;

/// <summary>
/// AOT bridge：强制 IL2CPP 将 TouchScreenKeyboard 相关成员编入 AOT 程序集，
/// 解决 HybridCLR 热更代码调用时出现的 MissingMethodException。
/// 此类不会在运行时被调用，仅用于编译期保留符号。
/// </summary>
[Preserve]
public static class TouchScreenKeyBoardBridge
{
    [Preserve]
    static void AOTPreserve()
    {
        // ── 静态 Open 重载 ──────────────────────────────────────────
        // KeyBoardTest.OpenKeyboard 使用
        TouchScreenKeyboard kb1 = TouchScreenKeyboard.Open("");

        // KeyBoardTest.Open 使用
        TouchScreenKeyboard kb2 = TouchScreenKeyboard.Open(
            "",
            TouchScreenKeyboardType.Default,
            false,   // autocorrection
            false,   // multiline
            false,   // secure
            false,   // alert
            "",      // textPlaceholder
            0        // characterLimit
        );

        // ── 静态属性 ────────────────────────────────────────────────
        // KeyBoardTest.Awake: TouchScreenKeyboard.hideInput = true
        TouchScreenKeyboard.hideInput = false;

        // KeyBoardTest.GetKeyboardHeight: TouchScreenKeyboard.area.height
        Rect area = TouchScreenKeyboard.area;

        // ── 实例属性 ────────────────────────────────────────────────
        // keyboard.text get / set
        string text = kb1.text;
        kb1.text = text;

        // keyboard.active set
        kb1.active = false;

        // ── InputField.touchScreenKeyboard ──────────────────────────
        // KeyBoardTest.Update: InputField.touchScreenKeyboard
        InputField inputField = null;
        TouchScreenKeyboard kb3 = inputField.touchScreenKeyboard;
    }
}
