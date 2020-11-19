using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using Random = System.Random;

namespace Oxide.Plugins
{
    public partial class SyncPipesDevelopment
    {
        public class Pipe
        {
            private PipeFactory _factory;

            /// <summary>
            /// This is the serializable data format for creating, loading or saving pipes with
            /// </summary>
            public class Data
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
                public PipePriority Priority = PipePriority.Medium;
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
                public Data() { }

                /// <summary>
                /// Create data from a pipe for saving
                /// </summary>
                /// <param name="pipe">Pipe to extract settings from</param>
                public Data(Pipe pipe)
                {
                    IsEnabled = pipe.IsEnabled;
                    Grade = pipe.Grade == BuildingGrade.Enum.None ? BuildingGrade.Enum.Twigs : pipe.Grade;
                    SourceId = pipe.Source.ContainerType == ContainerType.FuelStorage || pipe.Source.ContainerType == ContainerType.ResourceExtractor ? pipe.Source.Container.parentEntity.uid : pipe.Source.Id;
                    DestinationId = pipe.Destination.ContainerType == ContainerType.FuelStorage || pipe.Destination.ContainerType == ContainerType.ResourceExtractor ? pipe.Destination.Container.parentEntity.uid : pipe.Destination.Id;
                    SourceContainerType = pipe.Source.ContainerType;
                    DestinationContainerType = pipe.Destination.ContainerType;
                    Health = pipe.Health;
                    ItemFilter = pipe.FilterItems;
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
                public Data(PlayerHelper playerHelper)
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

            /// <summary>
            /// Get the save data for all pipes
            /// </summary>
            /// <returns>Data for all pipes</returns>
            public static IEnumerable<Data> Save() => Pipes.Select(a => new Data(a.Value));

            /// <summary>
            /// Load all data and re-create the saved pipes.
            /// </summary>
            /// <param name="dataToLoad">Data to create the pipes from</param>
            public static void Load(IEnumerable<Data> dataToLoad)
            {
                if (dataToLoad == null) return;
                var pipes = dataToLoad.Select(a => new Pipe(a)).ToList();
                foreach (var pipe in pipes.Where(a => a.Validity != Status.Success))
                {
                    Instance.PrintWarning("Failed to load pipe [{0}]: {1}", pipe.Id, pipe.Validity);
                }
                Instance.Puts("Successfully loaded {0} pipes", pipes.Count(a => a.Validity == Status.Success));
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
            public Pipe(Data data)
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
                Validate();
                if (Validity != Status.Success)
                    return;
                Source.Attach();
                Destination.Attach();
                Pipes.TryAdd(Id, this);
                ConnectedContainers.GetOrAdd(data.SourceId, new ConcurrentDictionary<uint, bool>())
                    .TryAdd(data.DestinationId, true);
                PlayerHelper.AddPipe(this);
                _initialFilterItems = data.ItemFilter;

                Distance = Vector3.Distance(Source.Position, Destination.Position);
                Rotation = GetRotation();
                _factory = new PipeFactoryBarrel(this);
                _factory.Create();
                if (data.Health != 0)
                    SetHealth(data.Health);
            }

            private Quaternion GetRotation() => Quaternion.LookRotation(Destination.Position - Source.Position);

            // Filter object. This remains null until it is needed
            private PipeFilter _pipeFilter;

            // This is the initial state of the filter. This is the fallback if the filter is not initialized
            private List<int> _initialFilterItems;

            /// <summary>
            /// The Filter object for this pipe.
            /// It will auto-initialize when required
            /// </summary>
            public PipeFilter PipeFilter => _pipeFilter ?? (_pipeFilter = new PipeFilter(_initialFilterItems, FilterCapacity, this));

            /// <summary>
            /// This will return all the items in the filter.
            /// If the Filter object has been created then it will pull from that otherwise it will pull from the initial filter items
            /// </summary>
            public List<int> FilterItems => _pipeFilter?.Items.Select(a => a.info.itemid).ToList() ?? _initialFilterItems ?? new List<int>();

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
            public ulong OwnerId { get; }
            /// <summary>
            /// Name of the player who built the pipe
            /// </summary>
            public string OwnerName { get; }

            /// <summary>
            /// List of all players who are viewing the Pipe menu
            /// </summary>
            private List<PlayerHelper> PlayersViewingMenu { get; } = new List<PlayerHelper>();

            ///// <summary>
            ///// List of all entities that physically make up the pipe in game
            ///// </summary>
            //private List<BaseEntity> Segments { get; } = new List<BaseEntity>();

            ///// <summary>
            ///// The primary physical section of the pipe
            ///// </summary>
            //public BaseEntity PrimarySegment => Segments.FirstOrDefault();

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
            public int FilterCapacity => InstanceConfig.FilterSizes[(int)Grade];

            /// <summary>
            /// Gets the Flow Rate based on the Grade of the pipe.
            /// </summary>
            public int FlowRate => InstanceConfig.FlowRates[(int)Grade];

            /// <summary>
            /// Health of the pipe
            /// Used to ensure the pipe is damaged and repaired evenly
            /// </summary>
            public float Health => _factory.PrimarySegment.Health();

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
            public static ConcurrentDictionary<ulong, Pipe> Pipes { get; } = new ConcurrentDictionary<ulong, Pipe>();

            /// <summary>
            /// All the connections between containers to prevent duplications
            /// </summary>
            public static ConcurrentDictionary<uint, ConcurrentDictionary<uint, bool>> ConnectedContainers { get; } =
                new ConcurrentDictionary<uint, ConcurrentDictionary<uint, bool>>();

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
            public float Distance { get; }

            /// <summary>
            /// The rotation of the pipe
            /// </summary>
            public Quaternion Rotation { get; private set; }

            public BaseEntity PrimarySegment => _factory.PrimarySegment;

            /// <summary>
            /// Checks if there is already a connection between these two containers
            /// </summary>
            /// <param name="data">Pipe data which includes the source and destination Ids</param>
            /// <returns>True if the is an overlap to prevent another pipe being created
            /// False if it is fine to create the pipe</returns>
            private static bool IsOverlapping(Data data)
            {
                var sourceId = data.SourceId;
                var destinationId = data.DestinationId;
                ConcurrentDictionary<uint, bool> linked;
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
                    id = (ulong)BitConverter.ToInt64(buf, 0);
                    if (safetyCheck++ > 50)
                    {
                        Validity = Status.IdGenerationFailed;
                        return 0;
                    }
                } while (Pipes.ContainsKey(Id));

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
                return Pipes.TryGetValue(id, out pipe) ? pipe : null;
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
                var newPipeData = new Data(playerHelper);
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
                Pipes.Clear();
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
            /// Reverse the direction of the pipe
            /// </summary>
            public void SwapDirection()
            {
                var stash = Source;
                Source = Destination;
                Destination = stash;
                Rotation = GetRotation();
                _factory.Reverse();
                RefreshMenu();
            }

            public void OpenMenu(PlayerHelper playerHelper)
            {
                if (playerHelper.IsMenuOpen)
                {
                    playerHelper.Menu.Refresh();
                }
                else if (playerHelper.CanBuild)
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
                foreach (var player in PlayersViewingMenu)
                    player.SendSyncPipesConsoleCommand("refreshmenu", Id);
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
                foreach (var player in PlayersViewingMenu)
                    player?.SendSyncPipesConsoleCommand("forceclosemenu");
                PipeFilter?.Kill();
                ContainerManager.Detach(Source.Id, this);
                ContainerManager.Detach(Destination.Id, this);
                PlayerHelper.RemovePipe(this);
                bool removed;
                ConcurrentDictionary<uint, bool> connectedTo;
                if (ConnectedContainers.ContainsKey(Source.Id) &&
                    ConnectedContainers.TryGetValue(Source.Id, out connectedTo))
                    connectedTo?.TryRemove(Destination.Id, out removed);
                if (ConnectedContainers.ContainsKey(Destination.Id) &&
                    ConnectedContainers.TryGetValue(Destination.Id, out connectedTo))
                    connectedTo?.TryRemove(Source.Id, out removed);

                Pipe removedPipe;
                Pipes.TryRemove(Id, out removedPipe);
                KillSegments(cleanup);
            }

            private void KillSegments(bool cleanup)
            {
                if (cleanup)
                {
                    if (!_factory.PrimarySegment?.IsDestroyed ?? false)
                        _factory.PrimarySegment?.Kill();
                }
                else
                {
                    Instance.NextFrame(() =>
                    {
                        if (!_factory.PrimarySegment?.IsDestroyed ?? false)
                            _factory.PrimarySegment?.Kill(BaseNetworkable.DestroyMode.Gib);
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
                var newPriority = (int)Priority + priorityChange;
                if (newPriority > (int)PipePriority.Highest)
                    Priority = PipePriority.Highest;
                else if (newPriority < (int)PipePriority.Lowest)
                    Priority = PipePriority.Lowest;
                else
                    Priority = (PipePriority)newPriority;
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
                Pipe deletedPipe;
                Pipes.TryRemove(Id, out deletedPipe);
            }

            /// <summary>
            /// Destroy all pipes from the server
            /// </summary>
            private static void KillAll()
            {
                foreach (var pipe in Pipes.Values)
                    pipe.Kill();
            }

            /// <summary>
            /// Upgrade the pipe and all its segments to a specified building grade
            /// </summary>
            /// <param name="grade">Grade to set the pipe to</param>
            public void Upgrade(BuildingGrade.Enum grade)
            {
                _factory.Upgrade(grade);

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
                _factory.SetHealth(health);
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
                playerHelper.Player.EndLooting();
                Instance.timer.Once(0.1f, () => PipeFilter.Open(playerHelper));
            }

            /// <summary>
            /// Copy the settings from another pipe
            /// </summary>
            /// <param name="pipe">The pipe to copy the settings from</param>
            public void CopyFrom(Pipe pipe)
            {
                foreach (var player in PlayersViewingMenu)
                    player.SendSyncPipesConsoleCommand("closemenu", Id);
                _pipeFilter?.Kill();
                _pipeFilter = null;
                _initialFilterItems = pipe.FilterItems.ToList();
                IsAutoStart = pipe.IsAutoStart;
                IsEnabled = pipe.IsEnabled;
                IsFurnaceSplitterEnabled = pipe.IsFurnaceSplitterEnabled;
                IsMultiStack = pipe.IsMultiStack;
                InvertFilter = pipe.InvertFilter;
                FurnaceSplitterStacks = pipe.FurnaceSplitterStacks;
                Priority = pipe.Priority;
            }
        }
    }
}