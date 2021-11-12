using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using Random = System.Random;

namespace Oxide.Plugins
{
    public partial class SyncPipesDevelopment
    {
        /// <summary>
        /// This is the serializable data format for creating, loading or saving pipes with
        /// </summary>
        public class PipeData
        {
            public bool IsEnabled = true;
            public BuildingGrade.Enum Grade = BuildingGrade.Enum.Twigs;
            public uint SourceId;
            public uint DestinationId;
            public ContainerType SourceContainerType;
            public ContainerType DestinationContainerType;
            public float Health;
            public List<int> ItemFilter = new List<int>();
            public bool IsMultiStack = true;
            public bool IsAutoStart = false;
            public bool IsFurnaceSplitter = false;
            public int FurnaceSplitterStacks = 1;
            public Pipe.PipePriority Priority = Pipe.PipePriority.Medium;
            public ulong OwnerId;
            public string OwnerName;
            private BaseEntity _source;
            private BaseEntity _destination;

            [JsonIgnore]
            public BaseEntity Source
            {
                get
                {
                    return _source ?? (_source = ContainerHelper.Find(SourceId, SourceContainerType));
                }
                private set
                {
                    _source = value;
                }
            }

            [JsonIgnore]
            public BaseEntity Destination
            {
                get
                {
                    return _destination ?? (_destination = ContainerHelper.Find(DestinationId, DestinationContainerType));
                }
                private set
                {
                    _destination = value;
                }
            }


            /// <summary>
            /// This is required to deserialize from json
            /// </summary>
            public PipeData() { }

            /// <summary>
            /// Create data from a pipe for saving
            /// </summary>
            /// <param name="pipe">Pipe to extract settings from</param>
            public PipeData(Pipe pipe)
            {
                IsEnabled = pipe.IsEnabled;
                Grade = pipe.Grade == BuildingGrade.Enum.None ? BuildingGrade.Enum.Twigs : pipe.Grade;
                SourceId = ContainerHelper.IsComplexStorage(pipe.Source.ContainerType) ? pipe.Source.Container.parentEntity.uid : pipe.Source.Id;
                DestinationId = ContainerHelper.IsComplexStorage(pipe.Destination.ContainerType) ? pipe.Destination.Container.parentEntity.uid : pipe.Destination.Id;
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
            /// Create a pipe data from the player helper's source and destination
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
        }
        [JsonConverter(typeof(Pipe.Converter))]
        public class Pipe
        {

            /// <summary>
            /// Get the save data for all pipes
            /// </summary>
            /// <returns>Data for all pipes</returns>
            public static IEnumerable<PipeData> Save()
            {
                for (int i = 0; i < Pipes.Count; i++)
                {
                    yield return new PipeData(Pipes[i]);
                }
            }

            private static void LogLoadError(ulong pipeId, Status status, PipeData pipeData)
            {
                Instance.LogToFile("PipeLoadErrors", string.Format("------------------- {0} -------------------", pipeId), Instance);
                Instance.LogToFile("PipeLoadErrors", string.Format("Status: {0}", status), Instance);
                Instance.LogToFile("PipeLoadErrors", string.Format("Source Id: {0}", pipeData.SourceId), Instance);
                Instance.LogToFile("PipeLoadErrors", string.Format("Destination Id: {0}", pipeData.DestinationId), Instance);
                Instance.LogToFile("PipeLoadErrors", string.Format("Source Type: {0}", pipeData.SourceContainerType), Instance);
                Instance.LogToFile("PipeLoadErrors", string.Format("Destination Type: {0}", pipeData.DestinationContainerType), Instance);
                Instance.LogToFile("PipeLoadErrors", string.Format("Material: {0}", pipeData.Grade), Instance);
                Instance.LogToFile("PipeLoadErrors", string.Format("Enabled: {0}", pipeData.IsEnabled), Instance);
                Instance.LogToFile("PipeLoadErrors", string.Format("Auto-start: {0}", pipeData.IsAutoStart), Instance);
                Instance.LogToFile("PipeLoadErrors", string.Format("Health: {0}", pipeData.Health), Instance);
                Instance.LogToFile("PipeLoadErrors", string.Format("Priority: {0}", pipeData.Priority), Instance);
                Instance.LogToFile("PipeLoadErrors", string.Format("Splitter Enabled: {0}", pipeData.IsFurnaceSplitter), Instance);
                Instance.LogToFile("PipeLoadErrors", string.Format("Splitter Count: {0}", pipeData.FurnaceSplitterStacks), Instance);
                Instance.LogToFile("PipeLoadErrors", string.Format("Item Filter: ({0})", pipeData.ItemFilter.Count), Instance);
                for (int i = 0; i < pipeData.ItemFilter.Count; i++)
                    Instance.LogToFile("PipeLoadErrors", string.Format("    Filter[{0}]: {1}", i, pipeData.ItemFilter[i]), Instance);
                Instance.LogToFile("PipeLoadErrors", "", Instance);

            }

            /// <summary>
            /// Load all data and re-create the saved pipes.
            /// </summary>
            /// <param name="dataToLoad">Data to create the pipes from</param>
            public static void Load(PipeData[] dataToLoad)
            {
                if (dataToLoad == null) return;
                var validCount = 0;
                for (var i = 0; i < dataToLoad.Length; i++)
                {
                    var newPipe = new Pipe(dataToLoad[i]);
                    if (newPipe.Validity != Status.Success)
                    {
                        Instance.PrintWarning("Failed to load pipe [{0}]: {1}", newPipe.Id, newPipe.Validity);
                        LogLoadError(newPipe.Id, newPipe.Validity, dataToLoad[i]);
                    }
                    else
                        validCount++;
                }
                Instance.Puts("Successfully loaded {0} pipes", validCount);
            }

            // Length of each segment
            const float PipeLength = 3f;

            // Offset of every other pipe segment to remove z fighting when wall segments overlap
            static readonly Vector3 PipeFightOffset = new Vector3(0.0001f, 0.0001f, 0);

            /// <summary>
            /// Allowed priority values of the pipe
            /// </summary>
            [EnumWithLanguage]
            public enum PipePriority
            {
                [English("Highest")]
                Highest = 2,
                [English("High")]
                High = 1,
                [English("Medium")]
                Medium = 0,
                [English("Low")]
                Low = -1,
                [English("Lowest")]
                Lowest = -2,

                //This has not been implemented yet but should allow a pipe to draw required fuel for furnaces when needed
                [English("Demand")]
                Demand = -3
            }

            /// <summary>
            /// The statuses the pipe can be in.
            /// Pending until it has initialized.
            /// Then will indicate any errors.
            /// </summary>
            [Flags]
            [EnumWithLanguage]
            public enum Status
            {
                [MessageType(MessageType.Info)]
                [English("It's not quite ready yet.")]
                Pending,

                [MessageType(MessageType.Success)]
                [English("Your pipe was built successfully")]
                Success,

                [MessageType(MessageType.Error)]
                [English("The first container you hit has gone missing. Give it another go.")]
                SourceError,

                [MessageType(MessageType.Error)]
                [English("The destination container you hit has gone missing. Please try again.")]
                DestinationError,

                [MessageType(MessageType.Error)]
                [English("We'll this is embarrassing, I seem to have failed to id that pipe. Can you try again for me.")]
                IdGenerationFailed
            }

            // The random generator used to generate the id for this pipe.
            private static readonly Random RandomGenerator;

            /// <summary>
            /// Initializes the random generator
            /// </summary>
            static Pipe()
            {
                RandomGenerator = new Random();
            }

            /// <summary>
            /// Creates a new pipe from PipeData
            /// </summary>
            /// <param name="data">Pipe data used to initialize the pipe.</param>
            public Pipe(PipeData data)
            {
                Id = GenerateId();
                IsEnabled = data.IsEnabled;
                Grade = data.Grade;
                Source = new PipeEndContainer(data.Source, data.SourceContainerType, this);
                Destination = new PipeEndContainer(data.Destination, data.DestinationContainerType, this);
                IsMultiStack = data.IsMultiStack;
                IsAutoStart = data.IsAutoStart;
                IsFurnaceSplitterEnabled = data.IsFurnaceSplitter;
                FurnaceSplitterStacks = data.FurnaceSplitterStacks;
                Priority = data.Priority;
                OwnerId = data.OwnerId;
                OwnerName = data.OwnerName;
                _initialFilterItems = data.ItemFilter;
                Validate();
                Create();
            }

            private void LogLoadError(ulong pipeId, Status status, uint sourceId, uint destinationId)
            {
                Instance.LogToFile("PipeLoadErrors", string.Format("------------------- {0} -------------------", pipeId), Instance);
                Instance.LogToFile("PipeLoadErrors", string.Format("Status: {0}", status), Instance);
                Instance.LogToFile("PipeLoadErrors", string.Format("Source Id: {0}", sourceId), Instance);
                Instance.LogToFile("PipeLoadErrors", string.Format("Destination Id: {0}", destinationId), Instance);
                Instance.LogToFile("PipeLoadErrors", string.Format("Source Type: {0}", Source?.ContainerType), Instance);
                Instance.LogToFile("PipeLoadErrors", string.Format("Destination Type: {0}", Destination?.ContainerType), Instance);
                Instance.LogToFile("PipeLoadErrors", string.Format("Material: {0}", Grade), Instance);
                Instance.LogToFile("PipeLoadErrors", string.Format("Enabled: {0}", IsEnabled), Instance);
                Instance.LogToFile("PipeLoadErrors", string.Format("Auto-start: {0}", IsAutoStart), Instance);
                Instance.LogToFile("PipeLoadErrors", string.Format("Health: {0}", _initialHealth), Instance);
                Instance.LogToFile("PipeLoadErrors", string.Format("Priority: {0}", Priority), Instance);
                Instance.LogToFile("PipeLoadErrors", string.Format("Splitter Enabled: {0}", IsFurnaceSplitterEnabled), Instance);
                Instance.LogToFile("PipeLoadErrors", string.Format("Splitter Count: {0}", FurnaceSplitterStacks), Instance);
                Instance.LogToFile("PipeLoadErrors", string.Format("Item Filter: ({0})", PipeFilter?.Items.Count), Instance);
                for (int i = 0; i < PipeFilter?.Items.Count; i++)
                    Instance.LogToFile("PipeLoadErrors", string.Format("    Item[{0}]: {1}", i, PipeFilter.Items[i]?.info.displayName.english), Instance);
                Instance.LogToFile("PipeLoadErrors", "", Instance);
            }

            private Pipe(JsonReader reader, JsonSerializer serializer)
            {
                Id = GenerateId();
                var depth = 1;
                if (reader.TokenType != JsonToken.StartObject)
                    return;
                uint sourceId = 0, destinationId = 0;
                ContainerType sourceType = ContainerType.General, destinationType = ContainerType.General;
                while (reader.Read() && depth > 0)
                {
                    switch (reader.TokenType)
                    {
                        case JsonToken.StartObject:
                            depth++;
                            break;
                        case JsonToken.EndObject:
                            depth--;
                            break;
                        case JsonToken.PropertyName:
                            switch (reader.Value.ToString())
                            {
                                case "enb":
                                    IsEnabled = reader.ReadAsBoolean() ?? false;
                                    break;
                                case "grd":
                                    Grade = (BuildingGrade.Enum)reader.ReadAsInt32().GetValueOrDefault(0);
                                    break;
                                case "sid":
                                    reader.Read();
                                    uint.TryParse(reader.Value.ToString(), out sourceId);
                                    break;
                                case "did":
                                    reader.Read();
                                    uint.TryParse(reader.Value.ToString(), out destinationId);
                                    break;
                                case "sct":
                                    sourceType = (ContainerType)reader.ReadAsInt32().GetValueOrDefault(0);
                                    break;
                                case "dct":
                                    destinationType = (ContainerType)reader.ReadAsInt32().GetValueOrDefault(0);
                                    break;
                                case "hth":
                                    _initialHealth = (float)reader.ReadAsDecimal().GetValueOrDefault(0);
                                    break;
                                case "mst":
                                    IsMultiStack = reader.ReadAsBoolean() ?? false;
                                    break;
                                case "ast":
                                    IsAutoStart = reader.ReadAsBoolean() ?? false;
                                    break;
                                case "fso":
                                    IsFurnaceSplitterEnabled = reader.ReadAsBoolean() ?? false;
                                    break;
                                case "fss":
                                    FurnaceSplitterStacks = reader.ReadAsInt32() ?? 1;
                                    break;
                                case "prt":
                                    Priority = (PipePriority)reader.ReadAsInt32().GetValueOrDefault(0);
                                    break;
                                case "oid":
                                    reader.Read();
                                    ulong ownerId;
                                    if (ulong.TryParse(reader.Value.ToString(), out ownerId))
                                        OwnerId = ownerId;
                                    break;
                                case "onm":
                                    OwnerName = reader.ReadAsString();
                                    break;
                                case "nme":
                                    DisplayName = reader.ReadAsString();
                                    break;
                                case "flr":
                                    var filterIds = new List<int>();
                                    while (reader.Read() && reader.TokenType != JsonToken.EndArray)
                                    {
                                        int value;
                                        if (reader.Value != null && int.TryParse(reader.Value?.ToString(), out value))
                                            filterIds.Add(value);
                                    }
                                    _initialFilterItems = filterIds;
                                    break;
                            }
                            break;
                    }
                }
                var source = ContainerHelper.Find(sourceId, sourceType);
                var destination = ContainerHelper.Find(destinationId, destinationType);
                Source = new PipeEndContainer(source, sourceType, this);
                Destination = new PipeEndContainer(destination, destinationType, this);
                Validate();
                if (Validity != Status.Success)
                    LogLoadError(Id, Validity, sourceId, destinationId);
            }

            public void Create()
            {
                if(PrimarySegment != null || Validity != Status.Success)
                    return;
                Source.Attach();
                Destination.Attach();
                CreatePipeSegmentEntities();
                if (!PipeLookup.ContainsKey(Id))
                {
                    PipeLookup.Add(Id, this);
                    Pipes.Add(this);
                }
                if(!ConnectedContainers.ContainsKey(Source.Id))
                    ConnectedContainers.Add(Source.Id, new Dictionary<uint, bool>());
                ConnectedContainers[Source.Id][Destination.Id] = true;
                PlayerHelper.AddPipe(this);
                if(_initialHealth > 0)
                    SetHealth(_initialHealth);
            }

            // Filter object. This remains null until it is needed
            private PipeFilter _pipeFilter;

            // This is the initial state of the filter. This is the fallback if the filter is not initialized
            private List<int> _initialFilterItems = new List<int>();


            /// <summary>
            /// The Filter object for this pipe.
            /// It will auto-initialize when required
            /// </summary>
            public PipeFilter PipeFilter => _pipeFilter ?? (_pipeFilter = new PipeFilter(_initialFilterItems, FilterCapacity, this));

            /// <summary>
            /// This will return all the items in the filter.
            /// If the Filter object has been created then it will pull from that otherwise it will pull from the initial filter items
            /// </summary>
            public IEnumerable<int> FilterItems
            {
                get
                {
                    for (var i = 0; i < PipeFilter.Items.Count; i++)
                    {
                        yield return PipeFilter.Items[i].info.itemid;
                    }
                }
            }

            /// <summary>
            /// Is furnace splitter enabled
            /// </summary>
            public bool IsFurnaceSplitterEnabled { get; private set; }

            /// <summary>
            /// Number of stacks to use in the furnace splitter
            /// </summary>
            public int FurnaceSplitterStacks { get; private set; }

            /// <summary>
            /// Should the pipe attempt to auto-start the destination container of the pipe.
            /// Must be and Oven, Recycler or Quarry/Pump Jack
            /// </summary>
            public bool IsAutoStart { get; private set; }

            /// <summary>
            /// Should the pipe stack to multiple stacks in the destination or a single stack
            /// </summary>
            public bool IsMultiStack { get; private set; }

            /// <summary>
            /// The Id of the player who built the pipe
            /// </summary>
            public ulong OwnerId { get; private set; }
            /// <summary>
            /// Name of the player who built the pipe
            /// </summary>
            public string OwnerName { get; private set; }

            /// <summary>
            /// List of all players who are viewing the Pipe menu
            /// </summary>
            private List<PlayerHelper> PlayersViewingMenu { get; } = new List<PlayerHelper>();

            /// <summary>
            /// List of all entities that physically make up the pipe in game
            /// </summary>
            private List<BaseEntity> Segments { get; } = new List<BaseEntity>();

            /// <summary>
            /// The primary physical section of the pipe
            /// </summary>
            public BaseEntity PrimarySegment => Segments.Count > 0 ? Segments[0] : null;// Segments.FirstOrDefault();

            /// <summary>
            /// The name a player has given to the pipe.
            /// </summary>
            public string DisplayName { get; set; }

            /// <summary>
            /// The material grade of the pipe. This determines Filter capacity and Flow Rate.
            /// </summary>
            public BuildingGrade.Enum Grade { get; private set; }

            /// <summary>
            /// Gets the Filter Capacity based on the Grade of the pipe.
            /// </summary>
            public int FilterCapacity => InstanceConfig.FilterSizes[(int) Grade];

            /// <summary>
            /// Gets the Flow Rate based on the Grade of the pipe.
            /// </summary>
            public int FlowRate => InstanceConfig.FlowRates[(int)Grade];

            /// <summary>
            /// Health of the pipe
            /// Used to ensure the pipe is damaged and repaired evenly
            /// </summary>
            public float Health => PrimarySegment.Health();

            private float _initialHealth;
            /// <summary>
            /// Used to indicate this pipe is being repaired to prevent multiple repair triggers
            /// </summary>
            public bool Repairing { get; set; }

            /// <summary>
            /// Id of the pipe
            /// </summary>
            public ulong Id { get; }

            /// <summary>
            /// Allows for the filter to be used to reject rather than allow items
            /// Not Yet Implemented
            /// </summary>
            public bool InvertFilter { get; private set; }

            /// <summary>
            /// The priority of this pipe.
            /// Each band of priority will be grouped together and if anything is left it will move to the next band down.
            /// </summary>
            public PipePriority Priority { get; private set; } = PipePriority.Medium;

            /// <summary>
            /// The validity of the pipe
            /// This will indicate any errors in creating the pipe and prevent it from being fully created or stored
            /// </summary>
            public Status Validity { get; private set; } = Status.Pending;

            /// <summary>
            /// Destination of the Pipe
            /// </summary>
            public PipeEndContainer Destination { get; private set; }

            /// <summary>
            /// Source of the Pipe
            /// </summary>
            public PipeEndContainer Source { get; private set; }

            /// <summary>
            /// All pipes that have been created
            /// </summary>
            public static Dictionary<ulong, Pipe> PipeLookup { get; } = new Dictionary<ulong, Pipe>();
            public static List<Pipe> Pipes { get; } = new List<Pipe>();

            /// <summary>
            /// All the connections between containers to prevent duplications
            /// </summary>
            public static Dictionary<uint, Dictionary<uint, bool>> ConnectedContainers { get; } =
                new Dictionary<uint, Dictionary<uint, bool>>();

            /// <summary>
            /// Controls if the pipe will transfer any items along it
            /// </summary>
            public bool IsEnabled { get; private set; }

            /// <summary>
            /// Gets information on whether the destination is an entity type that can be auto-started
            /// </summary>
            public bool CanAutoStart => Destination.CanAutoStart;

            /// <summary>
            /// The length of the pipe
            /// </summary>
            public float Distance { get; private set; }

            /// <summary>
            /// The rotation of the pipe
            /// </summary>
            private Quaternion Rotation { get; set; }

            /// <summary>
            /// Checks if there is already a connection between these two containers
            /// </summary>
            /// <param name="data">Pipe data which includes the source and destination Ids</param>
            /// <returns>True if the is an overlap to prevent another pipe being created
            /// False if it is fine to create the pipe</returns>
            private static bool IsOverlapping(PipeData data)
            {
                var sourceId = data.SourceId;
                var destinationId = data.DestinationId;
                Dictionary<uint, bool> linked;
                return
                    ConnectedContainers.TryGetValue(sourceId, out linked) && linked.ContainsKey(destinationId) ||
                    ConnectedContainers.TryGetValue(destinationId, out linked) && linked.ContainsKey(sourceId);
            }

            /// <summary>
            /// Generate a new Id for this pipe
            /// </summary>
            /// <returns></returns>
            private ulong GenerateId()
            {
                ulong id;
                var safetyCheck = 0;
                do
                {
                    var buf = new byte[8];
                    RandomGenerator.NextBytes(buf);
                    id = (ulong) BitConverter.ToInt64(buf, 0);
                    if (safetyCheck++ > 50)
                    {
                        Validity = Status.IdGenerationFailed;
                        return 0;
                    }
                } while (PipeLookup.ContainsKey(Id));

                return id;
            }

            /// <summary>
            /// Get a pipe object from a pipe entity
            /// </summary>
            /// <param name="entity">Entity to get the pipe object from</param>
            /// <returns></returns>
            public static Pipe Get(BaseEntity entity)
            {
                return entity.GetComponent<PipeSegment>()?.Pipe;
            }

            /// <summary>
            /// Get a pipe from a pipe Id
            /// </summary>
            /// <param name="id">Id to get the pipe object for</param>
            /// <returns></returns>
            public static Pipe Get(ulong id)
            {
                Pipe pipe;
                return PipeLookup.TryGetValue(id, out pipe) ? pipe : null;
            }

            /// <summary>
            /// Enable or disable the pipe.
            /// </summary>
            /// <param name="enabled">True will set the pipe to enabled</param>
            public void SetEnabled(bool enabled)
            {
                IsEnabled = enabled;
                RefreshMenu();
            }

            /// <summary>
            /// Returns if the player has permission to open the pipe settings
            /// </summary>
            /// <param name="playerHelper">Player helper of the player to test</param>
            /// <returns>True if the player is allows to open the pipe</returns>
            public bool CanPlayerOpen(PlayerHelper playerHelper) => playerHelper.Player.userID == OwnerId || playerHelper.HasBuildPrivilege;

            /// <summary>
            /// Attempt to create a pipe base on the player helper source and destination
            /// </summary>
            /// <param name="playerHelper">Player helper of the player trying to place the pipe</param>
            /// <returns>True if the pipe was successfully created</returns>
            public static void TryCreate(PlayerHelper playerHelper)
            {
                var newPipeData = new PipeData(playerHelper);
                if (IsOverlapping(newPipeData))
                {
                    playerHelper.ShowOverlay(Overlay.AlreadyConnected);
                }
                else
                {
                    var distance = Vector3.Distance(playerHelper.Source.CenterPoint(),
                        playerHelper.Destination.CenterPoint());
                    if (distance > InstanceConfig.MaximumPipeDistance)
                    {
                        playerHelper.ShowOverlay(Overlay.TooFar);
                    }
                    else if (distance <= InstanceConfig.MinimumPipeDistance)
                    {
                        playerHelper.ShowOverlay(Overlay.TooClose);
                    }
                    else
                    {
                        var pipe = new Pipe(newPipeData);
                        playerHelper.ShowPipeStatusOverlay(pipe.Validity);
                    }
                }
                playerHelper.PipePlacingComplete();
            }

            /// <summary>
            /// Remove all pipes and their segments from the server
            /// </summary>
            public static void Cleanup()
            {
                KillAll();
                PipeLookup.Clear();
                ConnectedContainers.Clear();
            }

            /// <summary>
            /// Validates that the pipe has a valid source and destination
            /// </summary>
            private void Validate()
            {
                if (Source == null || Source.Storage == null)
                    Validity = Status.SourceError;
                if (Destination == null || Destination.Storage == null)
                    Validity |= Status.DestinationError;
                if (Validity == Status.Pending)
                    Validity = Status.Success;
            }

            /// <summary>
            /// Create the pipe segment entities required to join the source and destination containers
            /// </summary>
            private void CreatePipeSegmentEntities()
            {
                GetPositionsAndRotation();
                var segments = (int) Mathf.Ceil(Distance / PipeLength);
                var segmentOffset = segments * PipeLength - Distance;
                var rotationOffset = (Source.Position.y - Destination.Position.y) * Vector3.down * 0.0002f;

                // the position thing centers the pipe if there is only one segment
                CreatePrimaryPipeSegmentEntity(segments == 1, rotationOffset);

                for (var i = 1; i < segments; i++)
                {
                    CreateSecondayPipeSegmentEntity(i, segmentOffset);
                }
            }

            /// <summary>
            /// Create a secondary pipe segment
            /// </summary>
            /// <param name="segmentIndex">The index of this segment of the pipe</param>
            /// <param name="segmentOffset">The offset to get the secondary pipe segments to reach the destination container exactly</param>
            private void CreateSecondayPipeSegmentEntity(int segmentIndex, float segmentOffset)
            {
                var pipe = GameManager.server.CreateEntity(
                    "assets/prefabs/building core/wall.low/wall.low.prefab",
                    Vector3.forward * (PipeLength * segmentIndex - segmentOffset) + (segmentIndex % 2 == 0
                        ? Vector3.zero
                        : PipeFightOffset));
                PreparePipeSegmentEntity(pipe, segmentIndex);
            }

            /// <summary>
            /// Creates the primary pipe segment
            /// </summary>
            /// <param name="singleSegmentPipe">If true the pipe will be centered between the containers (and may overlap the containers)</param>
            /// <param name="rotationOffset">Vertical offset to limit the pipe sticking out the top of the container</param>
            private void CreatePrimaryPipeSegmentEntity(bool singleSegmentPipe, Vector3 rotationOffset)
            {
                var primarySegment = GameManager.server.CreateEntity(
                    "assets/prefabs/building core/wall.low/wall.low.prefab",
                    (singleSegmentPipe
                        ? (Source.Position + Destination.Position) / 2
                        : Source.Position + Rotation * Vector3.forward * (PipeLength/2)) + rotationOffset + Vector3.down * 0.8f, Rotation);
                PreparePipeSegmentEntity(primarySegment, 0);
            }

            /// <summary>
            /// Reverse the direction of the pipe
            /// </summary>
            public void SwapDirections()
            {
                var stash = Source;
                Source = Destination;
                Destination = stash;
                RefreshMenu();
            }

            /// <summary>
            /// Initialize the properties of a pipe segment entity.
            /// Adds lights if enabled in the config
            /// </summary>
            /// <param name="pipeSegment">The pipe segment entity to prepare</param>
            /// <param name="pipeIndex">The index of this pipe segment</param>
            private void PreparePipeSegmentEntity(BaseEntity pipeSegment, int pipeIndex)
            {
                pipeSegment.enableSaving = false;

                var block = pipeSegment.GetComponent<BuildingBlock>();

                if (block != null)
                {
                    block.grounded = true;
                    block.grade = Grade;
                    block.enableSaving = false;
                    block.Spawn();
                    block.SetHealthToMax();
                }

                PipeSegment.Attach(pipeSegment, this);

                if (pipeIndex != 0)
                    pipeSegment.SetParent(PrimarySegment);

                if (InstanceConfig.AttachXmasLights)
                {
                    var lights = GameManager.server.CreateEntity(
                        "assets/prefabs/misc/xmas/christmas_lights/xmas.lightstring.deployed.prefab",
                        Vector3.up * 1.025f +
                        Vector3.forward * 0.13f +
                        (pipeIndex % 2 == 0
                            ? Vector3.zero
                            : PipeFightOffset),
                        Quaternion.Euler(180, 90, 0));
                    lights.enableSaving = false;
                    lights.Spawn();
                    lights.SetParent(pipeSegment);
                    PipeSegmentLights.Attach(lights, this);
                }

                Segments.Add(pipeSegment);
                //pillars.Add(ent);
                pipeSegment.enableSaving = false;
                if (InstanceConfig.NoDecay)
                {
                    ((DecayEntity) pipeSegment).upkeep = null;
                    ((DecayEntity)pipeSegment).decay = null;
                }
            }

            /// <summary>
            /// Get the source and destination positions and the rotation of the pipe
            /// </summary>
            private void GetPositionsAndRotation()
            {
                Source.Position = GetPosition(Source.Storage);
                Destination.Position = GetPosition(Destination.Storage);
                Rotation = Quaternion.LookRotation(Destination.Position - Source.Position) * Quaternion.Euler(0, 0, 0);
                Distance = Vector3.Distance(Source.Position, Destination.Position);
                // Adjust position based on the rotation
                //Source.Position += Rotation * Vector3.forward * PipeSegmentDistance *
                //    0.4f + Rotation * Vector3.down * 0.7f;
            }

            /// <summary>
            /// Get the pipe connection position for the container
            /// </summary>
            /// <param name="container">Container entity to get the connection position for</param>
            /// <returns>The connection position for this container</returns>
            private Vector3 GetPosition(BaseEntity container) => container.CenterPoint() + StorageHelper.GetOffset(container);

            public void OpenMenu(PlayerHelper playerHelper)
            {
                if (playerHelper.IsMenuOpen)
                {
                    playerHelper.Menu.Refresh();
                }
                else if(playerHelper.CanBuild)
                {
                    new PipeMenu(this, playerHelper).Open();
                    PlayersViewingMenu.Add(playerHelper);
                }
                else
                {
                    playerHelper.ShowOverlay(Overlay.NoPrivilegeToEdit);
                    OverlayText.Hide(playerHelper.Player, 3f);
                }
            }

            /// <summary>
            /// Close the Pipe Menu for a specific player
            /// </summary>
            /// <param name="playerHelper">Player Helper of the player to close the menu for</param>
            public void CloseMenu(PlayerHelper playerHelper)
            {
                playerHelper.CloseMenu();
                PlayersViewingMenu.Remove(playerHelper);
            }

            /// <summary>
            /// Refresh the Pipe Menu for all players currently viewing it
            /// </summary>
            private void RefreshMenu()
            {
                for(var i = 0; i < PlayersViewingMenu.Count; i++)
                    PlayersViewingMenu[i].SendSyncPipesConsoleCommand("refreshmenu", Id);
            }

            /// <summary>
            /// Returns if the pipe is still valid and has its source and destination containers
            /// </summary>
            /// <returns>If the pipe is still live</returns>
            public bool IsAlive() => Source?.Container != null && Destination?.Container != null;

            /// <summary>
            /// Destroy this pipe and ensure it is cleaned from the lookups
            /// </summary>
            /// <param name="cleanup">If true then destruction animations are disabled</param>
            public void Kill(bool cleanup = false)
            {
                for(var i = 0; i < PlayersViewingMenu.Count; i++)
                    PlayersViewingMenu[i]?.SendSyncPipesConsoleCommand("forceclosemenu");
                PipeFilter?.Kill();
                ContainerManager.Detach(Source.Id, this);
                ContainerManager.Detach(Destination.Id, this);
                PlayerHelper.RemovePipe(this);
                Dictionary<uint, bool> connectedTo;
                if (ConnectedContainers.ContainsKey(Source.Id) &&
                    ConnectedContainers.TryGetValue(Source.Id, out connectedTo))
                    connectedTo?.Remove(Destination.Id);
                if (ConnectedContainers.ContainsKey(Destination.Id) &&
                    ConnectedContainers.TryGetValue(Destination.Id, out connectedTo))
                    connectedTo?.Remove(Source.Id);
                if (PipeLookup.ContainsKey(Id))
                {
                    PipeLookup.Remove(Id);
                    Pipes.Remove(this);
                }

                if (cleanup)
                {
                    if (!PrimarySegment?.IsDestroyed ?? false)
                        PrimarySegment?.Kill();
                }
                else
                {
                    Instance.NextFrame(() =>
                    {
                        if (!PrimarySegment?.IsDestroyed ?? false)
                            PrimarySegment?.Kill(BaseNetworkable.DestroyMode.Gib);
                    });
                }
            }

            /// <summary>
            /// Change the current priority of the pipe
            /// </summary>
            /// <param name="priorityChange">The how many levels to change the priority by.</param>
            public void ChangePriority(int priorityChange)
            {
                var currPriority = Priority;
                var newPriority = (int) Priority + priorityChange;
                if (newPriority > (int) PipePriority.Highest)
                    Priority = PipePriority.Highest;
                else if (newPriority < (int) PipePriority.Lowest)
                    Priority = PipePriority.Lowest;
                else
                    Priority = (PipePriority) newPriority;
                if (currPriority != Priority)
                    RefreshMenu();
            }

            /// <summary>
            /// Remove this pipe
            /// </summary>
            /// <param name="cleanup">If true it will disable destruction animations of the pipe</param>
            public void Remove(bool cleanup = false)
            {
                PlayerHelper.RemovePipe(this);
                Kill(cleanup);
                if (PipeLookup.ContainsKey(Id))
                {
                    PipeLookup.Remove(Id);
                    Pipes.Remove(this);
                }
            }

            /// <summary>
            /// Destroy all pipes from the server
            /// </summary>
            private static void KillAll()
            {
                while (Pipes.Count > 0)
                {
                    Pipes[0].Kill();
                }
            }

            /// <summary>
            /// Upgrade the pipe and all its segments to a specified building grade
            /// </summary>
            /// <param name="grade">Grade to set the pipe to</param>
            public void Upgrade(BuildingGrade.Enum grade)
            {
                for (var i = 0; i < Segments.Count; i++)
                {
                    var buildingBlock = Segments[i].GetComponent<BuildingBlock>();
                    if(buildingBlock == null)
                        continue;
                    buildingBlock.SetGrade(grade);
                    buildingBlock.SetHealthToMax();
                    buildingBlock.SendNetworkUpdate(BasePlayer.NetworkQueue.UpdateDistance);
                }
                Grade = grade;
                PipeFilter.Upgrade(FilterCapacity);
                RefreshMenu();
            }

            /// <summary>
            /// Set all pipe segments to a specified health value
            /// </summary>
            /// <param name="health">Health value to set the pipe to</param>
            public void SetHealth(float health)
            {
                for (var i = 0; i < Segments.Count; i++)
                {
                    var buildingBlock = Segments[i].GetComponent<BuildingBlock>();
                    if (buildingBlock == null)
                        continue;
                    buildingBlock.health = health;
                    buildingBlock.SendNetworkUpdate(BasePlayer.NetworkQueue.UpdateDistance);
                }
            }

            /// <summary>
            /// Enable or disable Auto start
            /// </summary>
            /// <param name="autoStart">If true AutoStart will be enables</param>
            public void SetAutoStart(bool autoStart)
            {
                IsAutoStart = autoStart;
                RefreshMenu();
            }

            /// <summary>
            /// Set the pipe to multi-stack or single-stack mode
            /// </summary>
            /// <param name="multiStack">If true multi-stack mode will be enabled
            /// If false single-stack mode will be enabled</param>
            public void SetMultiStack(bool multiStack)
            {
                IsMultiStack = multiStack;
                RefreshMenu();
            }

            /// <summary>
            /// Enable or disable the furnace stack splitting
            /// </summary>
            /// <param name="enable">If true furnace splitting will be enabled</param>
            public void SetFurnaceStackEnabled(bool enable)
            {
                IsFurnaceSplitterEnabled = enable;
                RefreshMenu();
            }

            /// <summary>
            /// Set the number of stacks to use with the furnace stack splitter
            /// </summary>
            /// <param name="stackCount">The number of stacks to use</param>
            public void SetFurnaceStackCount(int stackCount)
            {
                FurnaceSplitterStacks = stackCount;
                RefreshMenu();
            }

            /// <summary>
            /// Open the filter for this pipe.
            /// It will create the filter if this is the first time it was opened
            /// </summary>
            /// <param name="playerHelper">The player helper of the player opening the filter</param>
            public void OpenFilter(PlayerHelper playerHelper)
            {
                CloseMenu(playerHelper);
                playerHelper.Player?.EndLooting();
                Instance.timer.Once(0.1f, () =>PipeFilter.Open(playerHelper));
            }

            /// <summary>
            /// Copy the settings from another pipe
            /// </summary>
            /// <param name="pipe">The pipe to copy the settings from</param>
            public void CopyFrom(Pipe pipe)
            {
                for (var i = 0; i < PlayersViewingMenu.Count; i++)
                    PlayersViewingMenu[i].SendSyncPipesConsoleCommand("closemenu", Id);
                _pipeFilter?.Kill();
                _pipeFilter = null;
                _initialFilterItems = new List<int>(pipe.FilterItems);
                IsAutoStart = pipe.IsAutoStart;
                IsEnabled = pipe.IsEnabled;
                IsFurnaceSplitterEnabled = pipe.IsFurnaceSplitterEnabled;
                IsMultiStack = pipe.IsMultiStack;
                InvertFilter = pipe.InvertFilter;
                FurnaceSplitterStacks = pipe.FurnaceSplitterStacks;
                Priority = pipe.Priority;
            }

            public class Converter: JsonConverter
            {
                public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
                {
                    var pipe = value as Pipe;
                    if (pipe == null) return;
                    writer.WriteStartObject();
                    writer.WritePropertyName("enb");
                    writer.WriteValue(pipe.IsEnabled);
                    writer.WritePropertyName("grd");
                    writer.WriteValue(pipe.Grade);
                    writer.WritePropertyName("sid");
                    writer.WriteValue(ContainerHelper.IsComplexStorage(pipe.Source.ContainerType) ? pipe.Source.Container.parentEntity.uid : pipe.Source.Id);
                    writer.WritePropertyName("did");
                    writer.WriteValue(ContainerHelper.IsComplexStorage(pipe.Destination.ContainerType) ? pipe.Destination.Container.parentEntity.uid : pipe.Destination.Id);
                    writer.WritePropertyName("sct");
                    writer.WriteValue(pipe.Source.ContainerType);
                    writer.WritePropertyName("dct");
                    writer.WriteValue(pipe.Destination.ContainerType);
                    writer.WritePropertyName("hth");
                    writer.WriteValue(pipe.Health);
                    writer.WritePropertyName("mst");
                    writer.WriteValue(pipe.IsMultiStack);
                    writer.WritePropertyName("ast");
                    writer.WriteValue(pipe.IsAutoStart);
                    writer.WritePropertyName("fso");
                    writer.WriteValue(pipe.IsFurnaceSplitterEnabled);
                    writer.WritePropertyName("fss");
                    writer.WriteValue(pipe.FurnaceSplitterStacks);
                    writer.WritePropertyName("prt");
                    writer.WriteValue(pipe.Priority);
                    writer.WritePropertyName("oid");
                    writer.WriteValue(pipe.OwnerId);
                    writer.WritePropertyName("onm");
                    writer.WriteValue(pipe.OwnerName);
                    writer.WritePropertyName("nme");
                    writer.WriteValue(pipe.DisplayName);
                    writer.WritePropertyName("flr");
                    writer.WriteStartArray();
                    for (int i = 0; i < pipe.PipeFilter.Items.Count; i++)
                        writer.WriteValue(pipe.PipeFilter.Items[i].info.itemid);
                    writer.WriteEndArray();
                    writer.WriteEndObject();
                }

                public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
                {
                    return new Pipe(reader, serializer);
                }

                public override bool CanConvert(Type objectType)
                {
                    return objectType == typeof(Pipe);
                }
            }
        }
    }
}