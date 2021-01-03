using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
            public static IEnumerable<Data> Save() => Managed.Where(a => a.Value.HasAnyPipes).Select(a => new Data(a.Value));

            /// <summary>
            /// Load all data into the container managers.
            /// This must be run after Pipe.Load as it only updates container managers created by the pipes.
            /// </summary>
            /// <param name="dataToLoad">Data to load into container managers</param>
            public static void Load(IEnumerable<Data> dataToLoad)
            {
                if (dataToLoad == null) return;
                ContainerManager manager;
                var containerCount = 0;
                foreach (var data in dataToLoad)
                {
                    if (Managed.TryGetValue(data.ContainerId, out manager))
                    {
                        containerCount++;
                        manager.DisplayName = data.DisplayName;
                        manager.CombineStacks = data.CombineStacks;
                    }
                    else
                    {
                        Instance.PrintWarning("Failed to load manager [{0} - {1}]: Container not found", data.ContainerId, data.DisplayName);
                    }
                }
                Instance.Puts("Successfully loaded {0} managers", containerCount);
            }

            /// <summary>
            ///     Keeps track of all the container managers that have been created.
            /// </summary>
            private static readonly ConcurrentDictionary<uint, ContainerManager> Managed =
                new ConcurrentDictionary<uint, ContainerManager>();

            // Which pipes have been attached to this container manager
            private readonly ConcurrentDictionary<ulong, Pipe> _attachedPipes = new ConcurrentDictionary<ulong, Pipe>();

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
            public bool HasAnyPipes => _attachedPipes.Any();

            /// <summary>
            ///     Cleanup all container managers. Normally used at unload.
            /// </summary>
            public static void Cleanup()
            {
                foreach (var containerManager in Managed.Values.ToArray())
                    containerManager?.Kill(true);
                Managed.Clear();
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
                foreach (var pipe in _attachedPipes.Values)
                {
                    if (pipe?.Destination?.ContainerManager == this)
                        pipe.Remove(cleanup);
                    if (pipe?.Source?.ContainerManager == this)
                        pipe.Remove(cleanup);
                }

                _destroyed = true;
                ContainerManager manager;
                Managed.TryRemove(ContainerId, out manager);
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
                var containerManager = Managed.GetOrAdd(entity.net.ID, entity.gameObject.AddComponent<ContainerManager>());
                containerManager._attachedPipes.TryAdd(pipe.Id, pipe);
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
                    ContainerManager containerManager;
                    Pipe removedPipe;
                    if (Managed.TryGetValue(containerId, out containerManager) && pipe != null)
                        containerManager._attachedPipes?.TryRemove(pipe.Id, out removedPipe);
                }
                catch (Exception e)
                {
                    Instance.Puts("{0}", e.StackTrace);
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
                _cumulativeDeltaTime += UnityEngine.Time.deltaTime;
                if (_cumulativeDeltaTime < InstanceConfig.UpdateRate) return;
                _cumulativeDeltaTime = 0f;
                if (_container.inventory.itemList.FirstOrDefault() == null)
                    return;
                var pipeGroups = _attachedPipes.Values.Where(a => a.Source.ContainerManager == this)
                    .GroupBy(a => a.Priority).OrderByDescending(a => a.Key).ToArray();
                foreach (var pipeGroup in pipeGroups)
                    if (CombineStacks)
                        MoveCombineStacks(pipeGroup);
                    else
                        MoveIndividualStacks(pipeGroup);
            }

            /// <summary>
            ///     Attempt to move all items from all stacks of the same type down the pipes in this priroity group
            ///     Items will be split as evenly as possible down all the pipes (limited by flow rate)
            /// </summary>
            /// <param name="pipeGroup">Pipes grouped by their priority</param>
            private void MoveCombineStacks(IGrouping<Pipe.PipePriority, Pipe> pipeGroup)
            {
                var distinctItems = _container.inventory.itemList.GroupBy(a => a.info.itemid)
                    .ToDictionary(a => a.Key, a => a.Select(b => b));

                var unusedPipes = pipeGroup
                    .Where(a => a.IsEnabled && !a.PipeFilter.Items.Any() ||
                                a.PipeFilter.Items.Select(b=>b.info.itemid).Any(b => distinctItems.ContainsKey(b)))
                    .OrderBy(a => a.Grade).ToList();
                while (unusedPipes.Any() && distinctItems.Any())
                {
                    var firstItem = distinctItems.First();
                    distinctItems.Remove(firstItem.Key);
                    var quantity = firstItem.Value.Sum(a => a.amount);
                    var validPipes = unusedPipes.Where(a =>
                            !a.PipeFilter.Items.Any() || a.PipeFilter.Items.Any(b => b.info.itemid == firstItem.Key))
                        .ToArray();
                    var pipesLeft = validPipes.Length;
                    foreach (var validPipe in validPipes)
                    {
                        unusedPipes.Remove(validPipe);
                        var amountToMove = GetAmountToMove(firstItem.Key, quantity, pipesLeft--, validPipe,
                            firstItem.Value.FirstOrDefault()?.MaxStackable() ?? 0);
                        if (amountToMove <= 0)
                            break;
                        quantity -= amountToMove;
                        foreach (var itemStack in firstItem.Value)
                        {
                            var toMove = itemStack;
                            if (amountToMove <= 0) break;
                            if (amountToMove < itemStack.amount)
                                toMove = itemStack.SplitItem(amountToMove);
                            if (Instance.FurnaceSplitter != null && validPipe.Destination.ContainerType == ContainerType.Oven &&
                                validPipe.IsFurnaceSplitterEnabled && validPipe.FurnaceSplitterStacks > 1)
                                Instance.FurnaceSplitter.Call("MoveSplitItem", toMove, validPipe.Destination.Storage,
                                    validPipe.FurnaceSplitterStacks);
                            else
                                toMove.MoveToContainer(validPipe.Destination.Storage.inventory);
                            if (validPipe.IsAutoStart && validPipe.Destination.HasFuel())
                                validPipe.Destination.Start();
                            amountToMove -= toMove.amount;
                        }

                        // If all items have been taken allow the pipe to transport something else. This will only occur if the intial quantity is less than the number of pipes
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
            private void MoveIndividualStacks(IGrouping<Pipe.PipePriority, Pipe> pipeGroup)
            {
                foreach (var pipe in pipeGroup)
                {
                    var item = _container.inventory.itemList.FirstOrDefault();
                    if (item == null) return;
                    GetItemToMove(item, pipe)?.MoveToContainer(pipe.Destination.Storage.inventory);
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
                var minStackSize = itemStacks.Any() ? itemStacks.Min(a => a.amount) : 0;
                if (minStackSize == 0 && emptySlots == 0)
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
                    var minStackSize = itemStacks.Any() ? itemStacks.Min(a => a.amount) : 0;
                    if (minStackSize == 0 && noEmptyStacks || minStackSize == maxStackable)
                        return null;
                    var space = maxStackable - minStackSize;
                    if (space < item.amount)
                        return item.SplitItem(space);
                    return item;
                }

                return item;
            }
        }
    }
}