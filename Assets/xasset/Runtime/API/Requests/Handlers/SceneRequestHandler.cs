using System.IO;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

namespace xasset
{
    public struct SceneRequestHandlerEdit : SceneRequestHandler
    {
        private Dependencies _dependencies;
        private SceneRequest _request;

        public void OnStart()
        {

        }

        public void Update()
        {

        }

        public void Release()
        {

        }

        public AsyncOperation LoadSceneAsync()
        {
#if UNITY_EDITOR
            var loadSceneMode = new LoadSceneParameters { loadSceneMode = _request.withAdditive ? LoadSceneMode.Additive : LoadSceneMode.Single };
            var scenes = AssetDatabase.FindAssets($"t:Scene {Path.GetFileNameWithoutExtension(_request.path)}");
            if (scenes.Length > 0)
                return EditorSceneManager.LoadSceneAsyncInPlayMode(UnityEditor.AssetDatabase.GUIDToAssetPath(scenes[0]), loadSceneMode);
#endif
            return null;
        }

        public bool IsReady()
        {
            return true;
        }

        public void WaitForCompletion()
        {
#if UNITY_EDITOR
            var loadSceneMode = new LoadSceneParameters { loadSceneMode = _request.withAdditive ? LoadSceneMode.Additive : LoadSceneMode.Single };
            var scenes = AssetDatabase.FindAssets($"t:Scene {Path.GetFileNameWithoutExtension(_request.path)}");
            if (scenes.Length > 0)
                EditorSceneManager.LoadSceneInPlayMode(UnityEditor.AssetDatabase.GUIDToAssetPath(scenes[0]), loadSceneMode);
            _request.SetResult(Request.Result.Success);
#else
            _request.SetResult(Request.Result.Success);
#endif
        }

        public float progressRate => 0.5f;

        public static SceneRequestHandler CreateInstance(SceneRequest sceneRequest)
        {
            return new SceneRequestHandlerEdit
            { _request = sceneRequest };
        }
    }

    public struct SceneRequestHandlerRuntime : SceneRequestHandler
    {
        private Dependencies _dependencies;
        private SceneRequest _request;

        public void OnStart()
        {
            _dependencies = Dependencies.LoadAsync(_request.info);
        }

        public void Update()
        {
            if (_dependencies.isDone) return;
            _request.progress = _dependencies.progress * progressRate;
            _dependencies.Update();
        }

        public void Release()
        {
            _dependencies.Release();
        }

        public AsyncOperation LoadSceneAsync()
        {
            var loadSceneMode = _request.withAdditive ? LoadSceneMode.Additive : LoadSceneMode.Single;
            return SceneManager.LoadSceneAsync(Path.GetFileNameWithoutExtension(_request.path), loadSceneMode);
        }

        public bool IsReady()
        {
            return _dependencies.isDone;
        }

        public void WaitForCompletion()
        {
            _dependencies.WaitForCompletion();
            if (!_dependencies.CheckResult(_request))
                return;

            var loadSceneMode = _request.withAdditive ? LoadSceneMode.Additive : LoadSceneMode.Single;
            SceneManager.LoadScene(Path.GetFileNameWithoutExtension(_request.path), loadSceneMode);
            _request.SetResult(Request.Result.Success);
        }

        public float progressRate => 0.5f;

        public static SceneRequestHandler CreateInstance(SceneRequest sceneRequest)
        {
            return new SceneRequestHandlerRuntime
            { _request = sceneRequest };
        }
    }

    public interface SceneRequestHandler
    {
        float progressRate { get; }
        void OnStart();
        void Update();
        void Release();
        AsyncOperation LoadSceneAsync();
        bool IsReady();
        void WaitForCompletion();
    }
}