using System.Collections.Generic;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    partial class SyncPipesDevelopment
    {
        partial class DataStore
        {
            partial class OnePointOne
            {
                [JsonConverter(typeof(DataConverter))]
                internal class ReadDataBuffer
                {
                    public List<Pipe> Pipes { get; } = new List<Pipe>();
                    public List<PipeFactoryData> Factories { get; } = new List<PipeFactoryData>();
                    public List<ContainerManagerData> Containers { get; } = new List<ContainerManagerData>();
                    public EntityFinder EntityFinder { get; } = new EntityFinder();
                }
            }
        }
    }
}
