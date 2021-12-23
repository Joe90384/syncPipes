using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using Random = System.Random;

namespace Oxide.Plugins
{
    public partial class SyncPipesDevelopment
    {

        public class Pipe
        {
            /// <summary>
            ///     Allowed priority values of the pipe
            /// </summary>
            [EnumWithLanguage]
            public enum PipePriority
            {
                [English("Highest")] 
                Highest = 2,
                [English("High")] High = 1,
                [English("Medium")] 
                Medium = 0,
                [English("Low")] Low = -1,
                [English("Lowest")] 
                Lowest = -2,

                //This has not been implemented yet but should allow a pipe to draw required fuel for furnaces when needed
                [English("Demand")] 
                Demand = -3
            }

            /// <summary>
            ///     The statuses the pipe can be in.
            ///     Pending until it has initialized.
            ///     Then will indicate any errors.
            /// </summary>
            [Flags]
            [EnumWithLanguage]
            public enum Status
            {
                [MessageType(MessageType.Info)] [English("It's not quite ready yet.")]
                Pending,

                [MessageType(MessageType.Success)] [English("Your pipe was built successfully")]
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
            internal PipeFactoryBase Factory { get; set; }

            // This is the initial state of the filter. This is the fallback if the filter is not initialized
            public List<int> InitialFilterItems { get; internal set; }= new List<int>();

            public float InitialHealth { get; internal set; }

            // Filter object. This remains null until it is needed
            private PipeFilter _pipeFilter;

            /// <summary>
            ///     Initializes the random generator
            /// </summary>
            static Pipe()
            {
                RandomGenerator = new Random();
            }

            public Pipe() { }

            /// <summary>
            ///     Creates a new pipe from PipeData
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
                InitialFilterItems = data.ItemFilter;
                Validate();
                Create();
            }

            //public Pipe(JsonReader reader, JsonSerializer serializer)
            //{
            //    Id = GenerateId();
            //    var depth = 1;
            //    if (reader.TokenType != JsonToken.StartObject)
            //        return;
            //    uint sourceId = 0, destinationId = 0;
            //    ContainerType sourceType = ContainerType.General, destinationType = ContainerType.General;
            //    while (reader.Read() && depth > 0)
            //        switch (reader.TokenType)
            //        {
            //            case JsonToken.StartObject:
            //                depth++;
            //                break;
            //            case JsonToken.EndObject:
            //                depth--;
            //                break;
            //            case JsonToken.PropertyName:
            //                switch (reader.Value.ToString())
            //                {
            //                    case "enb":
            //                        IsEnabled = reader.ReadAsBoolean() ?? false;
            //                        break;
            //                    case "grd":
            //                        Grade = (BuildingGrade.Enum)reader.ReadAsInt32().GetValueOrDefault(0);
            //                        break;
            //                    case "sid":
            //                        reader.Read();
            //                        uint.TryParse(reader.Value.ToString(), out sourceId);
            //                        break;
            //                    case "did":
            //                        reader.Read();
            //                        uint.TryParse(reader.Value.ToString(), out destinationId);
            //                        break;
            //                    case "sct":
            //                        sourceType = (ContainerType)reader.ReadAsInt32().GetValueOrDefault(0);
            //                        break;
            //                    case "dct":
            //                        destinationType = (ContainerType)reader.ReadAsInt32().GetValueOrDefault(0);
            //                        break;
            //                    case "hth":
            //                        InitialHealth = (float)reader.ReadAsDecimal().GetValueOrDefault(0);
            //                        break;
            //                    case "mst":
            //                        IsMultiStack = reader.ReadAsBoolean() ?? false;
            //                        break;
            //                    case "ast":
            //                        IsAutoStart = reader.ReadAsBoolean() ?? false;
            //                        break;
            //                    case "fso":
            //                        IsFurnaceSplitterEnabled = reader.ReadAsBoolean() ?? false;
            //                        break;
            //                    case "fss":
            //                        FurnaceSplitterStacks = reader.ReadAsInt32() ?? 1;
            //                        break;
            //                    case "prt":
            //                        Priority = (PipePriority)reader.ReadAsInt32().GetValueOrDefault(0);
            //                        break;
            //                    case "oid":
            //                        reader.Read();
            //                        ulong ownerId;
            //                        if (ulong.TryParse(reader.Value.ToString(), out ownerId))
            //                            OwnerId = ownerId;
            //                        break;
            //                    case "onm":
            //                        OwnerName = reader.ReadAsString();
            //                        break;
            //                    case "nme":
            //                        DisplayName = reader.ReadAsString();
            //                        break;
            //                    case "flr":
            //                        var filterIds = new List<int>();
            //                        while (reader.Read() && reader.TokenType != JsonToken.EndArray)
            //                        {
            //                            int value;
            //                            if (reader.Value != null && int.TryParse(reader.Value?.ToString(), out value))
            //                                filterIds.Add(value);
            //                        }

            //                        InitialFilterItems = filterIds;
            //                        break;
            //                }

            //                break;
            //        }

            //    var source = ContainerHelper.Find(sourceId, sourceType);
            //    var destination = ContainerHelper.Find(destinationId, destinationType);
            //    Source = new PipeEndContainer(source, sourceType, this);
            //    Destination = new PipeEndContainer(destination, destinationType, this);
            //    Validate();
            //    if (Validity != Status.Success)
            //        LogLoadError(Id, Validity, sourceId, destinationId);
            //}


            /// <summary>
            ///     The Filter object for this pipe.
            ///     It will auto-initialize when required
            /// </summary>
            public PipeFilter PipeFilter =>
                _pipeFilter ?? (_pipeFilter = new PipeFilter(InitialFilterItems, FilterCapacity, this));

            /// <summary>
            ///     This will return all the items in the filter.
            ///     If the Filter object has been created then it will pull from that otherwise it will pull from the initial filter
            ///     items
            /// </summary>
            public IEnumerable<int> FilterItems
            {
                get
                {
                    for (var i = 0; i < PipeFilter.Items.Count; i++) yield return PipeFilter.Items[i].info.itemid;
                }
            }

            /// <summary>
            ///     Is furnace splitter enabled
            /// </summary>
            public bool IsFurnaceSplitterEnabled { get; internal set; }

            /// <summary>
            ///     Number of stacks to use in the furnace splitter
            /// </summary>
            public int FurnaceSplitterStacks { get; internal set; }

            /// <summary>
            ///     Should the pipe attempt to auto-start the destination container of the pipe.
            ///     Must be and Oven, Recycler or Quarry/Pump Jack
            /// </summary>
            public bool IsAutoStart { get; internal set; }

            /// <summary>
            ///     Should the pipe stack to multiple stacks in the destination or a single stack
            /// </summary>
            public bool IsMultiStack { get; internal set; }

            /// <summary>
            ///     The Id of the player who built the pipe
            /// </summary>
            public ulong OwnerId { get; internal set; }

            /// <summary>
            ///     Name of the player who built the pipe
            /// </summary>
            public string OwnerName { get; internal set; }

            /// <summary>
            ///     List of all players who are viewing the Pipe menu
            /// </summary>
            private List<PlayerHelper> PlayersViewingMenu { get; } = new List<PlayerHelper>();

            ///// <summary>
            ///// List of all entities that physically make up the pipe in game
            ///// </summary>
            //private List<BaseEntity> Segments { get; } = new List<BaseEntity>();


            /// <summary>
            ///     The name a player has given to the pipe.
            /// </summary>
            public string DisplayName { get; set; }

            /// <summary>
            ///     The material grade of the pipe. This determines Filter capacity and Flow Rate.
            /// </summary>
            public BuildingGrade.Enum Grade { get; set; }

            /// <summary>
            ///     Gets the Filter Capacity based on the Grade of the pipe.
            /// </summary>
            public int FilterCapacity => InstanceConfig.FilterSizes[(int)Grade];

            /// <summary>
            ///     Gets the Flow Rate based on the Grade of the pipe.
            /// </summary>
            public int FlowRate => InstanceConfig.FlowRates[(int)Grade];

            /// <summary>
            ///     Health of the pipe
            ///     Used to ensure the pipe is damaged and repaired evenly
            /// </summary>
            public float Health => Factory.PrimarySegment.Health();

            /// <summary>
            ///     Used to indicate this pipe is being repaired to prevent multiple repair triggers
            /// </summary>
            public bool Repairing { get; set; }

            /// <summary>
            ///     Id of the pipe
            /// </summary>
            public uint Id { get; set; }

            /// <summary>
            ///     Allows for the filter to be used to reject rather than allow items
            ///     Not Yet Implemented
            /// </summary>
            public bool InvertFilter { get; private set; }

            /// <summary>
            ///     The priority of this pipe.
            ///     Each band of priority will be grouped together and if anything is left it will move to the next band down.
            /// </summary>
            public PipePriority Priority { get; internal set; } = PipePriority.Medium;

            /// <summary>
            ///     The validity of the pipe
            ///     This will indicate any errors in creating the pipe and prevent it from being fully created or stored
            /// </summary>
            public Status Validity { get; private set; } = Status.Pending;

            /// <summary>
            ///     Destination of the Pipe
            /// </summary>
            public PipeEndContainer Destination { get; internal set; }

            /// <summary>
            ///     Source of the Pipe
            /// </summary>
            public PipeEndContainer Source { get; internal set; }

            /// <summary>
            ///     All pipes that have been created
            /// </summary>
            public static Dictionary<ulong, Pipe> PipeLookup { get; } = new Dictionary<ulong, Pipe>();

            public static List<Pipe> Pipes { get; } = new List<Pipe>();

            /// <summary>
            ///     All the connections between containers to prevent duplications
            /// </summary>
            public static Dictionary<uint, Dictionary<uint, bool>> ConnectedContainers { get; } =
                new Dictionary<uint, Dictionary<uint, bool>>();

            /// <summary>
            ///     Controls if the pipe will transfer any items along it
            /// </summary>
            public bool IsEnabled { get; set; }

            /// <summary>
            ///     Gets information on whether the destination is an entity type that can be auto-started
            /// </summary>
            public bool CanAutoStart => Destination.CanAutoStart;

            /// <summary>
            ///     The length of the pipe
            /// </summary>
            public float Distance { get; private set; }

            /// <summary>
            ///     The rotation of the pipe
            /// </summary>
            public Quaternion Rotation { get; private set; }

            public BaseEntity PrimarySegment => Factory?.PrimarySegment;

            /// <summary>
            ///     Get the save data for all pipes
            /// </summary>
            /// <returns>DataStore for all pipes</returns>
            public static IEnumerable<PipeData> Save()
            {
                for (var i = 0; i < Pipes.Count; i++) yield return new PipeData(Pipes[i]);
            }

            public static void LogLoadError(ulong pipeId, Status status, PipeData pipeData)
            {
                Logger.PipeLoader.Log("------------------- {0} -------------------", pipeId);
                Logger.PipeLoader.Log("Status: {0}", status);
                Logger.PipeLoader.Log("Source Id: {0}", pipeData.SourceId);
                Logger.PipeLoader.Log("Destination Id: {0}", pipeData.DestinationId);
                Logger.PipeLoader.Log("Source Type: {0}", pipeData.SourceContainerType);
                Logger.PipeLoader.Log("Destination Type: {0}", pipeData.DestinationContainerType);
                Logger.PipeLoader.Log("Material: {0}", pipeData.Grade);
                Logger.PipeLoader.Log("Enabled: {0}", pipeData.IsEnabled);
                Logger.PipeLoader.Log("Auto-start: {0}", pipeData.IsAutoStart);
                Logger.PipeLoader.Log("Health: {0}", pipeData.Health);
                Logger.PipeLoader.Log("Priority: {0}", pipeData.Priority);
                Logger.PipeLoader.Log("Splitter Enabled: {0}", pipeData.IsFurnaceSplitter);
                Logger.PipeLoader.Log("Splitter Count: {0}", pipeData.FurnaceSplitterStacks);
                Logger.PipeLoader.Log("Item Filter: ({0})", pipeData.ItemFilter.Count);
                for (var i = 0; i < pipeData.ItemFilter.Count; i++)
                    Logger.PipeLoader.Log("    Filter[{0}]: {1}", i, pipeData.ItemFilter[i]);
                Logger.PipeLoader.Log("");
            }

            /// <summary>
            ///     Load all data and re-create the saved pipes.
            /// </summary>
            /// <param name="dataToLoad">DataStore to create the pipes from</param>
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
                    {
                        validCount++;
                    }
                }

                Instance.Puts("Successfully loaded {0} pipes", validCount);
            }


            public void Create()
            {
                if (Validity != Status.Success)
                    return;
                Distance = Vector3.Distance(Source.Position, Destination.Position);
                Rotation = GetRotation();
                if (Factory == null)
                {
                    Factory = InstanceConfig.Experimental?.BarrelPipe ?? false
                        ? new PipeFactoryBarrel(this)
                        : (PipeFactoryBase) new PipeFactoryLowWall(this);
                    Factory.Create();
                }

                if (PrimarySegment == null)
                    return;
                Source.Attach();
                Destination.Attach();
                if (!PipeLookup.ContainsKey(Id))
                {
                    PipeLookup.Add(Id, this);
                    Pipes.Add(this);
                }

                if (!ConnectedContainers.ContainsKey(Source.Id))
                    ConnectedContainers.Add(Source.Id, new Dictionary<uint, bool>());
                ConnectedContainers[Source.Id][Destination.Id] = true;
                PlayerHelper.AddPipe(this);
                if (InitialHealth > 0)
                    SetHealth(InitialHealth);
            }

            private Quaternion GetRotation()
            {
                return Quaternion.LookRotation(Destination.Position - Source.Position);
            }

            /// <summary>
            ///     Checks if there is already a connection between these two containers
            /// </summary>
            /// <param name="data">Pipe data which includes the source and destination Ids</param>
            /// <returns>
            ///     True if the is an overlap to prevent another pipe being created
            ///     False if it is fine to create the pipe
            /// </returns>
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
            ///     Generate a new Id for this pipe
            /// </summary>
            /// <returns></returns>
            internal uint GenerateId()
            {
                uint id;
                var safetyCheck = 0;
                do
                {
                    var buf = new byte[4];
                    RandomGenerator.NextBytes(buf);
                    id = (uint)BitConverter.ToUInt32(buf, 0);
                    if (safetyCheck++ > 50)
                    {
                        Validity = Status.IdGenerationFailed;
                        return 0;
                    }
                } while (PipeLookup.ContainsKey(Id));

                return id;
            }

            /// <summary>
            ///     Get a pipe object from a pipe entity
            /// </summary>
            /// <param name="entity">Entity to get the pipe object from</param>
            /// <returns></returns>
            public static Pipe Get(BaseEntity entity)
            {
                PipeSegment segment = null;
                entity?.TryGetComponent(out segment);
                return segment?.Pipe;
            }

            /// <summary>
            ///     Get a pipe from a pipe Id
            /// </summary>
            /// <param name="id">Id to get the pipe object for</param>
            /// <returns></returns>
            public static Pipe Get(ulong id)
            {
                Pipe pipe;
                return PipeLookup.TryGetValue(id, out pipe) ? pipe : null;
            }

            /// <summary>
            ///     Enable or disable the pipe.
            /// </summary>
            /// <param name="enabled">True will set the pipe to enabled</param>
            public void SetEnabled(bool enabled)
            {
                IsEnabled = enabled;
                RefreshMenu();
            }

            /// <summary>
            ///     Returns if the player has permission to open the pipe settings
            /// </summary>
            /// <param name="playerHelper">Player helper of the player to test</param>
            /// <returns>True if the player is allows to open the pipe</returns>
            public bool CanPlayerOpen(PlayerHelper playerHelper)
            {
                return playerHelper.Player.userID == OwnerId || playerHelper.HasBuildPrivilege;
            }

            /// <summary>
            ///     Attempt to create a pipe base on the player helper source and destination
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
            ///     Remove all pipes and their segments from the server
            /// </summary>
            public static void Cleanup()
            {
                KillAll();
                PipeLookup.Clear();
                ConnectedContainers.Clear();
            }

            /// <summary>
            ///     Validates that the pipe has a valid source and destination
            /// </summary>
            internal void Validate()
            {
                if (Source == null || Source.Storage == null)
                    Validity = Status.SourceError;
                if (Destination == null || Destination.Storage == null)
                    Validity |= Status.DestinationError;
                if (Validity == Status.Pending)
                    Validity = Status.Success;
            }

            /// <summary>
            ///     Reverse the direction of the pipe
            /// </summary>
            public void SwapDirection()
            {
                var stash = Source;
                Source = Destination;
                Destination = stash;
                Rotation = GetRotation();
                Factory.Reverse();
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
            ///     Close the Pipe Menu for a specific player
            /// </summary>
            /// <param name="playerHelper">Player Helper of the player to close the menu for</param>
            public void CloseMenu(PlayerHelper playerHelper)
            {
                playerHelper.CloseMenu();
                PlayersViewingMenu.Remove(playerHelper);
            }

            /// <summary>
            ///     Refresh the Pipe Menu for all players currently viewing it
            /// </summary>
            private void RefreshMenu()
            {
                for (var i = 0; i < PlayersViewingMenu.Count; i++)
                    PlayersViewingMenu[i].SendSyncPipesConsoleCommand("refreshmenu", Id);
            }

            /// <summary>
            ///     Returns if the pipe is still valid and has its source and destination containers
            /// </summary>
            /// <returns>If the pipe is still live</returns>
            public bool IsAlive()
            {
                return Source?.Container != null && Destination?.Container != null;
            }


            /// <summary>
            ///     Destroy this pipe and ensure it is cleaned from the lookups
            /// </summary>
            /// <param name="cleanup">If true then destruction animations are disabled</param>
            public void Kill(bool cleanup = false)
            {
                for (var i = 0; i < PlayersViewingMenu.Count; i++)
                    PlayersViewingMenu[i]?.SendSyncPipesConsoleCommand("forceclosemenu");
                PipeFilter?.Kill();
                KillSegments(cleanup);
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
            }

            private void KillSegments(bool cleanup)
            {
                if (cleanup && InstanceConfig.Experimental.PermanentEntities)
                {
                    for (var index = 0; index < Factory.Segments.Count; index++)
                    {
                        var segment = Factory.Segments[index];
                        PipeSegment pipeSegmentComponent;
                        if(segment.TryGetComponent(out pipeSegmentComponent))
                            UnityEngine.Object.Destroy(pipeSegmentComponent);
                    }

                    for (var index = 0; index < Factory.Lights.Count; index++)
                    {
                        var lights = Factory.Lights[index];
                        PipeSegmentLights pipeSegmentLightsComponent;
                        if (lights.TryGetComponent(out pipeSegmentLightsComponent))
                            UnityEngine.Object.Destroy(pipeSegmentLightsComponent);
                    }
                    return;
                }

                if (cleanup)
                {
                    if (!Factory.PrimarySegment?.IsDestroyed ?? false)
                        Factory.PrimarySegment?.Kill();
                }
                else
                {
                    Instance.NextFrame(() =>
                    {
                        if (!Factory.PrimarySegment?.IsDestroyed ?? false)
                            Factory.PrimarySegment?.Kill(BaseNetworkable.DestroyMode.Gib);
                    });
                }
            }

            /// <summary>
            ///     Change the current priority of the pipe
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
            ///     Remove this pipe
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
            ///     Destroy all pipes from the server
            /// </summary>
            private static void KillAll()
            {
                while (Pipes.Count > 0) Pipes[0].Kill(true);
            }

            /// <summary>
            ///     Upgrade the pipe and all its segments to a specified building grade
            /// </summary>
            /// <param name="grade">Grade to set the pipe to</param>
            public void Upgrade(BuildingGrade.Enum grade)
            {
                Factory.Upgrade(grade);
                Grade = grade;
                PipeFilter.Upgrade(FilterCapacity);
                RefreshMenu();
            }

            /// <summary>
            ///     Set all pipe segments to a specified health value
            /// </summary>
            /// <param name="health">Health value to set the pipe to</param>
            public void SetHealth(float health)
            {
                Factory.SetHealth(health);
            }

            /// <summary>
            ///     Enable or disable Auto start
            /// </summary>
            /// <param name="autoStart">If true AutoStart will be enables</param>
            public void SetAutoStart(bool autoStart)
            {
                IsAutoStart = autoStart;
                RefreshMenu();
            }

            /// <summary>
            ///     Set the pipe to multi-stack or single-stack mode
            /// </summary>
            /// <param name="multiStack">
            ///     If true multi-stack mode will be enabled
            ///     If false single-stack mode will be enabled
            /// </param>
            public void SetMultiStack(bool multiStack)
            {
                IsMultiStack = multiStack;
                RefreshMenu();
            }

            /// <summary>
            ///     Enable or disable the furnace stack splitting
            /// </summary>
            /// <param name="enable">If true furnace splitting will be enabled</param>
            public void SetFurnaceStackEnabled(bool enable)
            {
                IsFurnaceSplitterEnabled = enable;
                RefreshMenu();
            }

            /// <summary>
            ///     Set the number of stacks to use with the furnace stack splitter
            /// </summary>
            /// <param name="stackCount">The number of stacks to use</param>
            public void SetFurnaceStackCount(int stackCount)
            {
                FurnaceSplitterStacks = stackCount;
                RefreshMenu();
            }

            /// <summary>
            ///     Open the filter for this pipe.
            ///     It will create the filter if this is the first time it was opened
            /// </summary>
            /// <param name="playerHelper">The player helper of the player opening the filter</param>
            public void OpenFilter(PlayerHelper playerHelper)
            {
                CloseMenu(playerHelper);
                playerHelper.Player?.EndLooting();
                Instance.timer.Once(0.1f, () => PipeFilter.Open(playerHelper));
            }

            /// <summary>
            ///     Copy the settings from another pipe
            /// </summary>
            /// <param name="pipe">The pipe to copy the settings from</param>
            public void CopyFrom(Pipe pipe)
            {
                for (var i = 0; i < PlayersViewingMenu.Count; i++)
                    PlayersViewingMenu[i].SendSyncPipesConsoleCommand("closemenu", Id);
                _pipeFilter?.Kill();
                _pipeFilter = null;
                InitialFilterItems = new List<int>(pipe.FilterItems);
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