﻿namespace Oxide.Plugins
{
    public partial class SyncPipesDevelopment
    {
        /// <summary>
        /// Hook: Initialize syncPipes when the server starts up
        /// </summary>
        void OnServerInitialized()
        {

            if (!DataStore1_0.Load())
            {
                Instance.Puts("Upgrading from old data store");
                Data.Load();
            }
        }

        /// <summary>
        /// Hook: Save all the pipe data when the server saves
        /// </summary>
        void OnServerSave()
        {
            DataStore1_0.Save();
        }
    }
}
