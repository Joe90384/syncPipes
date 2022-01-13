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
                internal class WriteDataBuffer
                {
                    public List<string> Pipes { get; } = new List<string>();
                    public List<string> Containers { get; } = new List<string>();
                    public List<string> Factories { get; } = new List<string>();
                }
            }
        }
    }
}
