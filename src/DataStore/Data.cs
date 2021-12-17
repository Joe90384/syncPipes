using System.Collections.Generic;
using Oxide.Core;

namespace Oxide.Plugins
{
    partial class SyncPipesDevelopment
    {
        /// <summary>
        /// The data handler for loading and saving data to disk
        /// </summary>
        class Data
        {
            /// <summary>
            /// The data for all the pipes
            /// </summary>
            public PipeData[] PipeData { get; set; }

            /// <summary>
            /// The data for all the container managers
            /// </summary>
            public List<ContainerManager.Data> ContainerData { get; set; }
            /// <summary>
            /// Load syncPipes data from disk
            /// </summary>
            public static void Load()
            {
                var data = Interface.Oxide.DataFileSystem.ReadObject<Data>(Instance.Name);
                if (data != null)
                {
                    Pipe.Load(data.PipeData);
                    ContainerManager.Load(data.ContainerData);
                }
            }
        }
    }
}
