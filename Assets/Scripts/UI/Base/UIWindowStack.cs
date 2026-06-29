using System;
using System.Collections.Generic;
using UnityEngine;

namespace UI.Base
{
    internal class UIWindowStack
    {
        private const string DebugName = "UIWindowStack(UNITY_DEBUG)";
        public LinkedList<BaseWindow> _windows;

        public UIWindowStack()
        {
            _windows = new LinkedList<BaseWindow>();
        }

        public bool Contains(BaseWindow window)
        {
            return _windows.Contains(window);
        }

        public BaseWindow GetLastWindow(BaseWindow window)
        {
            if (!_windows.Contains(window)) return null;
            var node = _windows.Find(window);
            return node?.Previous?.Value;
        }

        public BaseWindow GetNextWindow(BaseWindow window)
        {
            if (!_windows.Contains(window)) return null;
            var node = _windows.Find(window);
            return node?.Next?.Value;
        }

        public bool Push(BaseWindow window)
        {
            if (!_windows.Contains(window))
            {
                _windows.AddLast(window);
                RefreshWinStackDebug();
                return true;
            }

            return false;
        }

        public BaseWindow Peek()
        {
            return _windows.Count <= 0 ? null : _windows.Last.Value;
        }

        public BaseWindow Pop()
        {
            var last = Peek();
            _windows.RemoveLast();
            RefreshWinStackDebug();
            return last;
        }

        public bool TryPeek(out BaseWindow window)
        {
            var peekValue = Peek();
            window = peekValue;
            return peekValue;
        }

        public bool TryPop(out BaseWindow window)
        {
            var popValue = Pop();
            window = popValue;
            return popValue;
        }

        /// <summary>
        /// 跳转到制定window, window栈顺序之后的出栈
        /// </summary>
        public bool TryPopToTarget(BaseWindow window, Action<BaseWindow> popWindowAct)
        {
            if (_windows.Contains(window))
            {
                var node = _windows.Find(window);

                var nextNode = node?.Next;
                while (nextNode != null && nextNode.Value)
                {
                    popWindowAct?.Invoke(nextNode.Value);
                    var removeNode = nextNode;
                    nextNode = nextNode.Next;

                    if (_windows.Contains(removeNode.Value))
                    {
                        _windows.Remove(removeNode);
                    }
                }

                if (node?.Next != null)
                {
                    node.Next.Value = null;
                }

                RefreshWinStackDebug();
                return true;
            }

            return false;
        }

        public bool RemoveWindow(BaseWindow window)
        {
            if (!_windows.Contains(window))
            {
                return false;
            }

            _windows.Remove(window);
            RefreshWinStackDebug();
            return true;
        }

        public void ClearAll()
        {
            foreach (var w in _windows)
            {
                if (w) w.Clear();
            }

            _windows?.Clear();
        }

        #region Windows堆栈可视化显示

        private void RefreshWinStackDebug()
        {
#if UNITY_EDITOR
            var stackNode = GameObject.Find(DebugName);
            if (stackNode == null)
            {
                stackNode = new GameObject(DebugName);
                stackNode.transform.parent = UIManager.Inst.Canvas.transform;
                stackNode.SetActive(true);
                stackNode.transform.SetAsFirstSibling();
            }

            var childs = stackNode.GetAllChildren();
            foreach (var c in childs)
            {
                GameObject.Destroy(c);
            }

            foreach (var w in _windows)
            {
                var wNode = new GameObject(w.name);
                wNode.transform.parent = stackNode.transform;
                wNode.transform.SetAsFirstSibling();
            }
#endif
        }

        #endregion
    }
}