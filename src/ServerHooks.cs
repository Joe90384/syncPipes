namespace Oxide.Plugins
{
    partial class SyncPipes
    {
        /// <summary>
        /// Hook: Initialize syncPipes when the server starts up
        /// </summary>
        void OnServerInitialized()
        {
            Data.Load();
        }

        /// <summary>
        /// Hook: Save all the pipe data when the server saves
        /// </summary>
        void OnServerSave() => Data.Save();
    }
}
