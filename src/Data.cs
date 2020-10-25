using System.Collections.Generic;
using System.Linq;
using Oxide.Core;

namespace Oxide.Plugins
{
    public partial class SyncPipes
    {
        /// <summary>
        /// The data handler for loading and saving data to disk
        /// </summary>
        class Data
        {
            /// <summary>
            /// The data for all the pipes
            /// </summary>
            public IEnumerable<Pipe.Data> PipeData { get; set; }

            /// <summary>
            /// The data for all the container managers
            /// </summary>
            public IEnumerable<ContainerManager.Data> ContainerData { get; set; }

            /// <summary>
            /// Save syncPipes data to disk
            /// </summary>
            public static void Save()
            {
                var data = new Data
                {
                    PipeData = Pipe.Save(),
                    ContainerData = ContainerManager.Save()
                };

                Interface.Oxide.DataFileSystem.WriteObject("SyncPipes", data);
                Instance.Puts("Saved {0} pipes", data.PipeData?.Count());
                Instance.Puts("Saved {0} managers", data.ContainerData?.Count());
            }

            /// <summary>
            /// Load syncPipes data from disk
            /// </summary>
            public static void Load()
            {
                var data = Interface.Oxide.DataFileSystem.ReadObject<Data>("SyncPipes");
                if (data != null)
                {
                    Pipe.Load(data.PipeData);
                    ContainerManager.Load(data.ContainerData);
                }
            }
        }
    }
}
