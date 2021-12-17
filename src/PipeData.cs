using System.Collections.Generic;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    partial class SyncPipesDevelopment
    {
        /// <summary>
        ///     This is the serializable data format for creating, loading or saving pipes with
        /// </summary>
        public class PipeData
        {
            private BaseEntity _destination;
            private BaseEntity _source;
            public ContainerType DestinationContainerType;
            public uint DestinationId;
            public int FurnaceSplitterStacks = 1;
            public BuildingGrade.Enum Grade = BuildingGrade.Enum.Twigs;
            public float Health;
            public bool IsAutoStart;
            public bool IsEnabled = true;
            public bool IsFurnaceSplitter;
            public bool IsMultiStack = true;
            public List<int> ItemFilter = new List<int>();
            public ulong OwnerId;
            public string OwnerName;
            public Pipe.PipePriority Priority = Pipe.PipePriority.Medium;
            public ContainerType SourceContainerType;
            public uint SourceId;


            /// <summary>
            ///     This is required to deserialize from json
            /// </summary>
            public PipeData()
            {
            }

            /// <summary>
            ///     Create data from a pipe for saving
            /// </summary>
            /// <param name="pipe">Pipe to extract settings from</param>
            public PipeData(Pipe pipe)
            {
                IsEnabled = pipe.IsEnabled;
                Grade = pipe.Grade == BuildingGrade.Enum.None ? BuildingGrade.Enum.Twigs : pipe.Grade;
                SourceId = pipe.Source.ContainerType == ContainerType.FuelStorage ||
                           pipe.Source.ContainerType == ContainerType.ResourceExtractor
                    ? pipe.Source.Container.parentEntity.uid
                    : pipe.Source.Id;
                DestinationId =
                    pipe.Destination.ContainerType == ContainerType.FuelStorage ||
                    pipe.Destination.ContainerType == ContainerType.ResourceExtractor
                        ? pipe.Destination.Container.parentEntity.uid
                        : pipe.Destination.Id;
                SourceContainerType = pipe.Source.ContainerType;
                DestinationContainerType = pipe.Destination.ContainerType;
                Health = pipe.Health;
                ItemFilter = new List<int>(pipe.FilterItems);
                IsMultiStack = pipe.IsMultiStack;
                IsAutoStart = pipe.IsAutoStart;
                IsFurnaceSplitter = pipe.IsFurnaceSplitterEnabled;
                FurnaceSplitterStacks = pipe.FurnaceSplitterStacks;
                Priority = pipe.Priority;
                OwnerId = pipe.OwnerId;
                OwnerName = pipe.OwnerName;
            }

            /// <summary>
            ///     Create a pipe data from the player helper's source and destination
            /// </summary>
            /// <param name="playerHelper">Player helper to pull the source and destination from</param>
            public PipeData(PlayerHelper playerHelper)
            {
                OwnerId = playerHelper.Player.userID;
                OwnerName = playerHelper.Player.displayName;
                Source = playerHelper.Source;
                Destination = playerHelper.Destination;
                SourceId = Source.net.ID;
                DestinationId = Destination.net.ID;
                SourceContainerType = ContainerHelper.GetEntityType(playerHelper.Source);
                DestinationContainerType = ContainerHelper.GetEntityType(playerHelper.Destination);
                IsEnabled = true;
            }

            [JsonIgnore]
            public BaseEntity Source
            {
                get { return _source ?? (_source = ContainerHelper.Find(SourceId, SourceContainerType)); }
                private set { _source = value; }
            }

            [JsonIgnore]
            public BaseEntity Destination
            {
                get
                {
                    return _destination ??
                           (_destination = ContainerHelper.Find(DestinationId, DestinationContainerType));
                }
                private set { _destination = value; }
            }
        }
    }
}
