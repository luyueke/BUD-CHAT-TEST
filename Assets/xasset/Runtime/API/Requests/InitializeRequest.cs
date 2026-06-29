using System;
using UnityEngine;

namespace xasset
{
    public class InitializeRequest : Request
    {
        private readonly InitializeRequestHandler _handler;

        public InitializeRequest()
        {
            _handler = CreateHandler(this);
        }

        public static Func<InitializeRequest, InitializeRequestHandler> CreateHandler { get; set; } = InitializeRequestHandlerRuntime.CreateInstance;

        protected override void OnUpdated()
        {
            _handler.OnUpdated();
        }

        protected override void OnStart()
        {
            if (Application.isEditor && Assets.OfflineMode)
                Assets.PlayerDataPath = Environment.CurrentDirectory + $"/Bundles/{Assets.Platform}";
            _handler.OnStart();
        }

        protected override void OnCompleted()
        {
            Logger.E($"Initialize with: {result} {error}.");
            Logger.E($"API Version:{Assets.APIVersion}");
            Logger.E($"Simulation Mode: {Assets.SimulationMode}");
            Logger.E($"Offline Mode: {Assets.OfflineMode}");
            Logger.E($"Versions: {Assets.Versions}");
            Logger.E($"Platform: {Assets.Platform}");
        }
    }
}