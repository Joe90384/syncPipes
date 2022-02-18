using Oxide.Core;

namespace Oxide.Plugins
{
    public partial class SyncPipesDevelopment
    {
        /// <summary>
        /// Hook: Initialize syncPipes when the server starts up
        /// </summary>
        void OnServerInitialized()
        {
            if (DataStore.OnePointOne.FileExists) {
                DataStore.OnePointOne.Load();
            }
            else if (DataStore.OnePointZero.FileExists)
            {
                Instance.PrintWarning("Upgrading from V1.0 to V1.1");
                DataStore.OnePointZero.Load();
            }
            else if(Interface.Oxide.DataFileSystem.ExistsDatafile(Instance.Name))
            {
                Instance.PrintError(@"
+------------------------------------------------------------------------------+
|                     DATA LOAD ERROR UNSUPPORTED VERSION                      |
+------------------------------------------------------------------------------+
| Direct upgrading from this data store format is not supported.               |
|                                                                              |
| To upgrade your existing pipes:                                              |
|  - Unload SyncPipes                                                          |
|  - Delete the SyncPipes_v1-1.json file from the data store                   |
|  - Install (and load) SyncPipes Version 0.9.27                               |
|  - Re-install this version of SyncPipes                                      |
|                                                                              |
| Otherwise ignore this message and any new pipes will be saved as normal.     |
+------------------------------------------------------------------------------+");

            }
            else
            {
                Instance.PrintWarning("No pipe data found.");
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
