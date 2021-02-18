using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Oxide.Plugins
{
    public partial class SyncPipesDevelopment
    {
        /// <summary>
        ///     This is attached to a Storage Container to act as the controller for moving items through pipes.
        ///     This then allows for items to move through pipes in a more synchronous manner.
        ///     Items can be split evenly between all pipes of the same priority.
        /// </summary>
        [JsonConverter(typeof(ContainerManager.Converter))]
        public class ContainerManager : MonoBehaviour
        {
            /// <summary>
            /// This is the serializable data format fro loading or saving container manager data
            /// </summary>
            public class Data
            {
                public uint ContainerId;
                public bool CombineStacks;
                public string DisplayName;

                /// <summary>
                /// This is required to deserialize from json
                /// </summary>
                public Data() { }

                /// <summary>
                /// Create data from a container manager for saving
                /// </summary>
                /// <param name="containerManager">Container manager to extract settings from</param>
                public Data(ContainerManager containerManager)
                {
                    ContainerId = containerManager.ContainerId;
                    CombineStacks = containerManager.CombineStacks;
                    DisplayName = containerManager.DisplayName;
                }
            }

            /// <summary>
            /// Get the save data for all container managers
            /// </summary>
            /// <returns>data for all container managers</returns>
            public static IEnumerable<Data> Save()
            {
                using (var enumerator = ManagedContainerLookup.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        if (enumerator.Current.Value.HasAnyPipes)
                            yield return new Data(enumerator.Current.Value);
                    }
                }
            }

            /// <summary>
            /// Load all data into the container managers.
            /// This must be run after Pipe.Load as it only updates container managers created by the pipes.
            /// </summary>
            /// <param name="dataToLoad">Data to load into container managers</param>
            public static void Load(List<Data> dataToLoad)
            {
                if (dataToLoad == null) return;
                var containerCount = 0;
                for(int i = 0; i < dataToLoad.Count; i++)
                {
                    ContainerManager manager;
                    if (ManagedContainerLookup.TryGetValue(dataToLoad[i].ContainerId, out manager))
                    {
                        containerCount++;
                        manager.DisplayName = dataToLoad[i].DisplayName;
                        manager.CombineStacks = dataToLoad[i].CombineStacks;
                    }
                    else
                    {
                        Instance.PrintWarning("Failed to load manager [{0} - {1}]: Container not found", dataToLoad[i].ContainerId, dataToLoad[i].DisplayName);
                    }
                }
                Instance.Puts("Successfully loaded {0} managers", containerCount);
            }

            /// <summary>
            ///     Keeps track of all the container managers that have been created.
            /// </summary>
            private static readonly Dictionary<uint, ContainerManager> ManagedContainerLookup =
                new Dictionary<uint, ContainerManager>();
            public static readonly List<ContainerManager> ManagedContainers = new List<ContainerManager>();

            // Which pipes have been attached to this container manager
            //private readonly Dictionary<ulong, Pipe> _attachedPipeLookup = new Dictionary<ulong, Pipe>();
            private readonly List<Pipe> _attachedPipes = new List<Pipe>();

            // Pull from multiple stack of the same type whe moving or only move one stack per priority level
            // This has been implemented but the controlling systems have not been developed
            public bool CombineStacks { get; private set; } = true;

            private StorageContainer _container; // The storage container this manager is attached to
            public uint ContainerId; // The id of the storage container this manager is attached to

            private float _cumulativeDeltaTime; // Used to keep track of the time between each cycle
            private bool _destroyed; // Prevents move cycles from happening when the container is being destroyed
            public string DisplayName; // The name of this container

            /// <summary>
            ///     Checks if there are any pipes attached to this container.
            /// </summary>
            public bool HasAnyPipes => _attachedPipes.Count > 0;

            /// <summary>
            ///     Cleanup all container managers. Normally used at unload.
            /// </summary>
            public static void Cleanup()
            {
                while (ManagedContainers.Count > 0)
                {
                    if(ManagedContainers[0] == null)
                        ManagedContainers.RemoveAt(0);
                    else
                        ManagedContainers[0].Kill(true);
                }
                ManagedContainerLookup.Clear();
                ManagedContainers.Clear();
            }

            /// <summary>
            ///     Destroy this Container manager and any attached pipes
            /// </summary>
            /// <param name="cleanup">
            ///     Is this a cleanup call.
            ///     If this is false then the pipes will animate when they are destroyed.
            /// </param>
            private void Kill(bool cleanup = false)
            {
                for(var i = 0; i < _attachedPipes.Count; i++)
                {
                    if (_attachedPipes[i]?.Destination?.ContainerManager == this)
                        _attachedPipes[i].Remove(cleanup);
                    if (_attachedPipes[i]?.Source?.ContainerManager == this)
                        _attachedPipes[i].Remove(cleanup);
                }

                _destroyed = true;
                if (ManagedContainerLookup.ContainsKey(ContainerId))
                {
                    ManagedContainerLookup.Remove(ContainerId);
                    ManagedContainers.Remove(this);
                }

                Destroy(this);
            }

            /// <summary>
            ///     Locate exist container manager for this container or create a new one then attach it to the container.
            /// </summary>
            /// <param name="entity">Entity to attach the manager to</param>
            /// <param name="container">Container for this entity</param>
            /// <param name="pipe">Pipe to attach</param>
            /// <returns></returns>
            public static ContainerManager Attach(BaseEntity entity, StorageContainer container, Pipe pipe)
            {
                if (entity == null || container == null || pipe == null) return null;
                ContainerManager containerManager = null;
                if (!ManagedContainerLookup.ContainsKey(entity.net.ID))
                {
                    containerManager = entity.gameObject.AddComponent<ContainerManager>();
                    ManagedContainerLookup.Add(entity.net.ID, containerManager);
                    ManagedContainers.Add(containerManager);
                }
                else
                {
                    containerManager = ManagedContainerLookup[entity.net.ID];
                }
                if (!containerManager._attachedPipes.Contains(pipe))
                {
                    containerManager._attachedPipes.Add(pipe);
                }
                containerManager.ContainerId = entity.net.ID;
                containerManager._container = container;
                return containerManager;
            }

            /// <summary>
            ///     Detach a pipe from the container manager
            /// </summary>
            /// <param name="containerId">Id of the container to identify the container manager</param>
            /// <param name="pipe">The pipe to detach</param>
            public static void Detach(uint containerId, Pipe pipe)
            {
                try
                {
                    if (pipe != null && ManagedContainerLookup.ContainsKey(containerId))
                    {
                        var containerManager = ManagedContainerLookup[containerId];
                        if (containerManager._attachedPipes?.Contains(pipe) ?? false)
                            containerManager._attachedPipes?.Remove(pipe);
                    }
                }
                catch (Exception e)
                {
                    Instance.PrintError("{0}", e.StackTrace);
                }
            }

            /// <summary>
            /// Hook: Check container and if still valid and cycle time has elapsed then move items along pipes
            /// </summary>
            private void Update()
            {
                if (_container == null)
                    Kill();
                if (_destroyed || !HasAnyPipes) return;
                _cumulativeDeltaTime += Time.deltaTime;
                if (_cumulativeDeltaTime < InstanceConfig.UpdateRate) return;
                _cumulativeDeltaTime = 0f;
                if (_container.inventory.itemList.Count == 0 || _container.inventory.itemList[0] == null)
                    return;
                var pipeGroups = new Dictionary<int, Dictionary<int, List<Pipe>>>();
                for (var i = 0; i < _attachedPipes.Count; i++)
                {
                    var pipe = _attachedPipes[i];
                    if (_attachedPipes[i].Source.Container != _container || !_attachedPipes[i].IsEnabled)
                        continue;
                    var priority = (int) pipe.Priority;
                    var grade = (int) pipe.Grade;
                    if(!pipeGroups.ContainsKey(priority))
                        pipeGroups.Add(priority, new Dictionary<int, List<Pipe>>());
                    if(!pipeGroups[priority].ContainsKey(grade))
                        pipeGroups[priority].Add(grade, new List<Pipe>());
                    pipeGroups[priority][grade].Add(pipe);
                }
                //var pipeGroups = _attachedPipeLookup.Values.Where(a => a.Source.ContainerManager == this)
                //    .GroupBy(a => a.Priority).OrderByDescending(a => a.Key).ToArray();
                for (int i = (int)Pipe.PipePriority.Highest; i > (int)Pipe.PipePriority.Demand; i--)
                {
                    if (!pipeGroups.ContainsKey(i)) continue;
                    if(CombineStacks)
                        MoveCombineStacks(pipeGroups[i]);
                    else
                        MoveIndividualStacks(pipeGroups[i]);
                }
            }

            /// <summary>
            ///     Attempt to move all items from all stacks of the same type down the pipes in this priroity group
            ///     Items will be split as evenly as possible down all the pipes (limited by flow rate)
            /// </summary>
            /// <param name="pipeGroup">Pipes grouped by their priority</param>
            private void MoveCombineStacks(Dictionary<int, List<Pipe>> pipeGroup)
            {
                var distinctItemIds = new List<int>();
                var distinctItems = new Dictionary<int, List<Item>>();
                var itemList = _container.inventory.itemList;
                for (var i = 0; i < itemList.Count; i++)
                {
                    var itemId = itemList[i].info.itemid;
                    if (!distinctItems.ContainsKey(itemList[i].info.itemid))
                    {
                        distinctItems.Add(itemId, new List<Item>());
                        distinctItemIds.Add(itemId);
                    }

                    distinctItems[itemId].Add(itemList[i]);
                }
                var unusedPipes = new List<Pipe>();
                for (var i = (int) BuildingGrade.Enum.Twigs; i <= (int) BuildingGrade.Enum.TopTier; i++)
                {
                    if (!pipeGroup.ContainsKey(i))
                        continue;
                    for (var j = 0; j < pipeGroup[i].Count; j++)
                    {
                        var pipe = pipeGroup[i][j];
                        if (pipe.Source.Id != ContainerId)
                            continue;
                        if (pipe.PipeFilter.Items.Count > 0)
                        {
                            var found = false;
                            for (var k = 0; k < pipe.PipeFilter.Items.Count; k++)
                            {
                                if (distinctItems.ContainsKey(pipe.PipeFilter.Items[k].info.itemid))
                                    found = true;
                            }
                            if (!found) 
                                continue;
                        }
                        unusedPipes.Add(pipe);
                    }
                }

                while (unusedPipes.Count > 0 && distinctItems.Count > 0)
                {
                    var itemId = distinctItemIds[0];
                    var item = distinctItems[itemId];
                    distinctItems.Remove(distinctItemIds[0]);
                    distinctItemIds.RemoveAt(0);
                    var quantity = 0;
                    for (var i = 0; i < item.Count; i++)
                        quantity += item[0].amount;
                    var validPipes = new List<Pipe>();
                    for (var i = 0; i < unusedPipes.Count; i++)
                    {
                        var pipe = unusedPipes[i];
                        if (pipe.PipeFilter.Items.Count > 0)
                        {
                            bool found = false;
                            for (var j = 0; j < pipe.PipeFilter.Items.Count; j++)
                            {
                                if (pipe.PipeFilter.Items[j].info.itemid == itemId)
                                    found = true;
                            }

                            if (!found) 
                                continue;
                        }

                        validPipes.Add(pipe);
                    }
                    var pipesLeft = validPipes.Count;
                    for(var i = 0; i < validPipes.Count; i++)
                    {
                        var validPipe = validPipes[i];
                        unusedPipes.Remove(validPipe);
                        var amountToMove = GetAmountToMove(itemId, quantity, pipesLeft--, validPipe,
                            item[0]?.MaxStackable() ?? 0);
                        if (amountToMove <= 0)
                            break;
                        quantity -= amountToMove;
                        for(var j = 0; j < item.Count; j++)
                        {
                            var itemStack = item[j];
                            var toMove = itemStack;
                            if (amountToMove <= 0) break;
                            if (amountToMove < itemStack.amount)
                                toMove = itemStack.SplitItem(amountToMove);
                            if (Instance.FurnaceSplitter != null &&
                                validPipe.Destination.ContainerType == ContainerType.Oven &&
                                validPipe.IsFurnaceSplitterEnabled && validPipe.FurnaceSplitterStacks > 1)
                            {
                                var result = Instance.FurnaceSplitter.Call("MoveSplitItem", toMove,
                                    validPipe.Destination.Storage,
                                    validPipe.FurnaceSplitterStacks);
                                if(!result.ToString().Equals("ok", StringComparison.InvariantCultureIgnoreCase))
                                    toMove.MoveToContainer(validPipe.Source.Storage.inventory);
                            }
                            else
                            {
                                var toContainer = validPipe.Destination.Storage.inventory;
                                if (!toMove.MoveToContainer(toContainer))
                                {
                                    // Fix for issue with Vending machines not being able to move the end of a stack stack from a container.
                                    // Remove item from container and then move it. If it didn't actually move then add it back to the source.
                                    toMove.RemoveFromContainer();
                                    if (!toMove.MoveToContainer(toContainer))
                                        toMove.MoveToContainer(validPipe.Source.Storage.inventory);
                                }
                            }

                            if (validPipe.IsAutoStart && validPipe.Destination.HasFuel())
                                validPipe.Destination.Start();
                            amountToMove -= toMove.amount;
                        }

                        // If all items have been taken allow the pipe to transport something else. This will only occur if the initial quantity is less than the number of pipes
                        if (quantity <= 0)
                            break;
                    }
                }
            }

            /// <summary>
            ///     Attempt to move items from the first stack down the pipes in this priority group
            ///     Items will be split as evenly as possible down all the pipes (limited by flow rate)
            /// </summary>
            /// <param name="pipeGroup">Pipes grouped by their priority</param>
            private void MoveIndividualStacks(Dictionary<int, List<Pipe>> pipeGroup)
            {
                for (var i = 0; i < (int) BuildingGrade.Enum.TopTier; i++)
                {
                    if (!pipeGroup.ContainsKey(i))
                        continue;
                    var pipes = pipeGroup[i];
                    for (var j = 0; j < pipes.Count; j++)
                    {
                        var pipe = pipes[j];
                        var item = _container.inventory.itemList.Count > 0 ? _container.inventory.itemList[0] : null;
                        if (item == null) return;
                        GetItemToMove(item, pipe)?.MoveToContainer(pipe.Destination.Storage.inventory);
                    }
                }
            }

            /// <summary>
            ///     Determines the maximum quantity of the item can be moved down a pipe in this cycle
            /// </summary>
            /// <param name="itemId">The id of the item to be moved</param>
            /// <param name="itemQuantity">The total number of items available to move</param>
            /// <param name="pipesLeft">How many more pipes in this pipe group are left</param>
            /// <param name="pipe">The pipe to move items along</param>
            /// <param name="maxStackable">The maximum stack size of this item. Used to check available space</param>
            /// <returns></returns>
            private int GetAmountToMove(int itemId, int itemQuantity, int pipesLeft, Pipe pipe, int maxStackable)
            {
                var destinationContainer = pipe?.Destination.Storage;
                if (destinationContainer == null || maxStackable == 0) return 0;
                var amountToMove = (int) Math.Ceiling((decimal) itemQuantity / pipesLeft);
                if (amountToMove > pipe.FlowRate)
                    amountToMove = pipe.FlowRate;
                var emptySlots = destinationContainer.inventory.capacity -
                                 destinationContainer.inventory.itemList.Count;
                var itemStacks = destinationContainer.inventory.FindItemsByItemID(itemId);
                int minStackSize = GetMinStackSize(itemStacks);
                if (minStackSize <= 0 && emptySlots == 0)
                    return 0;
                if (!pipe.IsMultiStack)
                {
                    var stackCapacity = maxStackable - minStackSize;
                    if (minStackSize > 0)
                        return amountToMove <= stackCapacity ? amountToMove : stackCapacity;
                    return amountToMove;
                }

                var slotsRequired = (int) Math.Ceiling((decimal) amountToMove / maxStackable);
                if (slotsRequired <= emptySlots)
                    return amountToMove;
                var neededSpace = amountToMove % maxStackable;
                return maxStackable - minStackSize >= neededSpace
                    ? amountToMove
                    : maxStackable * (slotsRequired - 1) + maxStackable - minStackSize;
            }

            /// <summary>
            ///     Prepare the item to be moved along the pipe
            ///     This takes into account available space in the destination and flow rate
            /// </summary>
            /// <param name="item">The item to be moved</param>
            /// <param name="pipe">The pipe to move the item along</param>
            /// <returns></returns>
            private Item GetItemToMove(Item item, Pipe pipe)
            {
                var destinationContainer = pipe.Destination.Storage;
                if (destinationContainer == null) return null;
                var maxStackable = item.MaxStackable();
                if (item.amount > pipe.FlowRate)
                    item.SplitItem(pipe.FlowRate);
                var noEmptyStacks = destinationContainer.inventory.itemList.Count ==
                                    destinationContainer.inventory.capacity;
                if (!pipe.IsMultiStack || noEmptyStacks)
                {
                    var itemStacks = destinationContainer.inventory.FindItemsByItemID(item.info.itemid);
                    int minStackSize = GetMinStackSize(itemStacks);
                    if (minStackSize == 0 && noEmptyStacks || minStackSize == maxStackable)
                        return null;
                    var space = maxStackable - minStackSize;
                    if (space < item.amount)
                        return item.SplitItem(space);
                    return item;
                }

                return item;
            }
            private static int GetMinStackSize(List<Item> itemStacks)
            {
                int minStackSize = -1;
                for (var i = 0; i < itemStacks.Count; i++)
                {
                    if (minStackSize < 0 || itemStacks[i].amount < minStackSize)
                        minStackSize = itemStacks[i].amount;
                }
                return minStackSize < 0 ? 0 : minStackSize;
            }

            public class Converter : JsonConverter
            {
                public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
                {
                    var container = value as ContainerManager;
                    if (container == null) return;
                    writer.WriteStartObject();
                    writer.WritePropertyName("ci");
                    writer.WriteValue(container.ContainerId);
                    writer.WritePropertyName("cs");
                    writer.WriteValue(container.CombineStacks);
                    writer.WritePropertyName("dn");
                    writer.WriteValue(container.DisplayName);
                    writer.WriteEndObject();
                }

                public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
                {
                    return null;
                }

                public override bool CanConvert(Type objectType)
                {
                    return objectType == typeof(ContainerManager);
                }
            }
        }

    }
}