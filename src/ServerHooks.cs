namespace Oxide.Plugins
{
    public partial class SyncPipesDevelopment
    {
        /// <summary>
        /// Hook: Initialize syncPipes when the server starts up
        /// </summary>
        void OnServerInitialized()
        {
            //if(!InstanceConfig.Experimental.PermanentEntities || !DataStore.OnePointOne.Load())
                DataStore.OnePointZero.Load();
                DataStore.OnePointOne.Load();
        }

        /// <summary>
        /// Hook: Save all the pipe data when the server saves
        /// </summary>
        void OnServerSave()
        {
            //if (InstanceConfig.Experimental.PermanentEntities)
                DataStore.OnePointOne.Save();
            //else
                DataStore.OnePointZero.Save();
        }
    }
}
