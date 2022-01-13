namespace Oxide.Plugins
{
    public partial class SyncPipesDevelopment
    {
        /// <summary>
        /// Hook: Initialize syncPipes when the server starts up
        /// </summary>
        void OnServerInitialized()
        {
            if (!DataStore.OnePointOne.Load())
            {
                Instance.PrintWarning("Upgrading from V1.0 to V1.1");
                DataStore.OnePointZero.Load();
            }
        }

        /// <summary>
        /// Hook: Save all the pipe data when the server saves
        /// </summary>
        void OnServerSave()
        {
            DataStore.OnePointOne.Save();
        }
    }
}
