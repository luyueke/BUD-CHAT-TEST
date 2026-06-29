using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace xasset
{
    public static class Logger
    {
        private const string TAG = "[xasset]";

        public static bool Enabled { get; set; } = false;

        [Conditional("DEBUG")]
        public static void D(object msg, Object context = null)
        {
#if UNITY_EDITOR
            Debug.Log($"{TAG} {msg}", context);
#endif
        }

        public static void I(object msg, Object context = null)
        {
#if UNITY_EDITOR
            Debug.Log($"{TAG} {msg}", context);
#endif
        }

        public static void E(object msg, Object context = null)
        {
            Debug.LogWarning($"{TAG} <Error> {msg}", context);
        }

        public static void W(object msg, Object context = null)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"{TAG} {msg}", context);
#endif
        }
    }
}