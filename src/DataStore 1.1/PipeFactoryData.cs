using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    partial class SyncPipesDevelopment
    {
        partial class DataStore
        {
            partial class OnePointOne
            {
                /// <summary>
                /// This is the serializable data format for loading or saving pipe factory data
                /// </summary>
                public class PipeFactoryData
                {
                    public uint PipeId { get; set; }
                    public bool IsBarrel { get; set; }
                    public uint[] SegmentEntityIds { get; set; }
                    public uint[] LightEntityIds { get; set; }

                    /// <summary>
                    /// This is required to deserialize from json
                    /// </summary>
                    public PipeFactoryData()
                    {
                    }

                    /// <summary>
                    /// Create data from a container manager for saving
                    /// </summary>
                    /// <param name="pipe">Pipe to extract factory from</param>
                    public PipeFactoryData(Pipe pipe)
                    {
                        PipeId = pipe.Id;
                        IsBarrel = pipe.Factory is PipeFactoryBarrel;
                        SegmentEntityIds = new uint[pipe.Factory.Segments.Count];
                        for (int i = 0; i < pipe.Factory.Segments.Count; i++)
                        {
                            SegmentEntityIds[i] = pipe.Factory.Segments[i].net.ID;
                        }
                    }
                }
            }
        }
    }
}
