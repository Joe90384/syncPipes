using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("syncPipes", "Joe 90", "0.9.0")]
    [Description("Allows players to transfer items between containers. All pipes from a container are used synchronously to enable advanced sorting and splitting.")]
    public partial class SyncPipes : RustPlugin
    {
        /// <summary>
        /// The instance of syncPipes on the server to allow child classes to access it
        /// </summary>
        private static SyncPipes Instance;

        // Name of the plugin that is used in things like the command builder.
        const string PluginName = "syncpipes";

        // Reference to the Furnace Splitter plugin https://umod.org/plugins/furnace-splitter
        [PluginReference]
        Plugin FurnaceSplitter;

        /// <summary>
        /// Hook: Initializes syncPipes when the server starts to load it
        /// </summary>
        void Init()
        {
            Instance = this;
            _config = SyncPipesConfig.Load();
            LocalizationHelpers.Register();
            Commands.InitializeChat();
        }
        
        /// <summary>
        /// Hook: Ensures permissions are registered once syncPipes has loaded
        /// </summary>
        void Loaded()
        {
            permission.RegisterPermission($"{PluginName}.user", this);
            permission.RegisterPermission($"{PluginName}.admin", this);
            InstanceConfig.RegisterPermissions();
        }

        /// <summary>
        /// Hook: Cleans up syncPipes when the server unloads it
        /// </summary>
        void Unload()
        {
            Data.Save();
            Puts("Unloading All Pipes");
            Pipe.Cleanup();
            ContainerManager.Cleanup();
            PlayerHelper.Cleanup();
        }
    }
}
