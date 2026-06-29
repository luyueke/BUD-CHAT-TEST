using UnityEngine;

namespace Game.COSXML
{
    [ExecuteInEditMode]
    public class CosXmlUpdateDelegator : MonoBehaviour
    {
        /// <summary>
        /// The singleton instance of the HTTPUpdateDelegator
        /// </summary>
        public static CosXmlUpdateDelegator Instance { get; private set; }

        /// <summary>
        /// True, if the Instance property should hold a valid value.
        /// </summary>
        public static bool IsCreated { get; private set; }
        private static bool IsSetupCalled;

        public static void CheckInstance()
        {
            try
            {
                if (!IsCreated)
                {
                    var go = GameObject.Find("CosXml Update Delegator");

                    if (go != null)
                        Instance = go.GetComponent<CosXmlUpdateDelegator>();

                    if (Instance == null)
                    {
                        go = new GameObject("CosXml Update Delegator")
                        {
                            hideFlags = HideFlags.HideAndDontSave
                        };

                        Instance = go.AddComponent<CosXmlUpdateDelegator>();
                    }
                    IsCreated = true;

#if UNITY_EDITOR
                    if (!UnityEditor.EditorApplication.isPlaying)
                    {
                        UnityEditor.EditorApplication.update -= Instance.Update;
                        UnityEditor.EditorApplication.update += Instance.Update;
                    }

                    UnityEditor.EditorApplication.playModeStateChanged -= Instance.OnPlayModeStateChanged;
                    UnityEditor.EditorApplication.playModeStateChanged += Instance.OnPlayModeStateChanged;
#endif

                    // https://docs.unity3d.com/ScriptReference/Application-wantsToQuit.html
                    Application.wantsToQuit -= UnityApplication_WantsToQuit;
                    Application.wantsToQuit += UnityApplication_WantsToQuit;
                    Debug.Log("CosXmlUpdateDelegator:" + "Instance Created!");
                }
            }
            catch
            {
                Debug.LogError("CosXmlUpdateDelegator:" +
                    "Please call the CosXmlUpdateManager.Setup() from one of Unity's event(eg. awake, start) before you upload!");
            }
        }

        private void Setup()
        {
            if (IsSetupCalled)
                return;
            IsSetupCalled = true;

            CosXmlUploadManager.Setup();
            // Unity doesn't tolerate well if the DontDestroyOnLoad called when purely in editor mode. So, we will set the flag
            //  only when we are playing, or not in the editor.
            if (!Application.isEditor || Application.isPlaying)
                GameObject.DontDestroyOnLoad(this.gameObject);
        }

        static void ResetSetup()
        {
            IsSetupCalled = false;
        }

        void Update()
        {
            if (!IsSetupCalled)
                Setup();
            CosXmlUploadManager.OnUpdate();
        }
#if UNITY_EDITOR
        void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange playMode)
        {
            if (playMode == UnityEditor.PlayModeStateChange.EnteredPlayMode)
            {
                UnityEditor.EditorApplication.update -= Update;
            }
            else if (playMode == UnityEditor.PlayModeStateChange.EnteredEditMode)
            {
                UnityEditor.EditorApplication.update -= Update;
                UnityEditor.EditorApplication.update += Update;

                ResetSetup();
                CosXmlUploadManager.ResetSetup();
            }
        }
#endif


        private static bool UnityApplication_WantsToQuit()
        {
            if (!IsCreated)
                return true;

            IsCreated = false;
            CosXmlUploadManager.Shutdown();
            return true;
        }
    }
}
