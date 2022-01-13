using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("Sync Pipes", "Joe 90", "0.9.28")]
    [Description("Allows players to transfer items between containers. All pipes from a container are used synchronously to enable advanced sorting and splitting.")]
    public partial class SyncPipesDevelopment : RustPlugin
    {
        /// <summary>
        /// The instance of syncPipes on the server to allow child classes to access it
        /// </summary>
        private static SyncPipesDevelopment Instance;

        private const string ToolCupboardPrefab = "cupboard.tool.deployed";

#pragma warning disable CS0649
        // Reference to the Furnace Splitter plugin https://umod.org/plugins/furnace-splitter
        [PluginReference]
        Plugin FurnaceSplitter;

        // Refernce to the Quick Smelt plugin https://umod.org/plugins/quick-smelt
        [PluginReference] 
        Plugin QuickSmelt;
#pragma warning restore CS0649

        /// <summary>
        /// Hook: Initializes syncPipes when the server starts to load it
        /// </summary>
        void Init()
        {
            Instance = this;
            _config = SyncPipesConfig.Load();
            Commands.InitializeChat();
            permission.RegisterPermission($"{Name}.user", this);
            permission.RegisterPermission($"{Name}.admin", this);
            InstanceConfig.RegisterPermissions();
        }

        /// <summary>
        /// Hook: Cleans up syncPipes when the server unloads it
        /// </summary>
        void Unload()
        {
            Instance.Puts("SyncPipes is unloading...");
            DataStore.OnePointOne.Save(false);
            Puts("Unloading All Pipes");
            Pipe.Cleanup();
            ContainerManager.Cleanup();
            PlayerHelper.Cleanup();
            ExperimentalUnload();
            Instance.Puts("SyncPipes unloaded");
        }

        partial void ExperimentalUnload();
    }
}
