﻿using System;
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
        public class ContainerManager : MonoBehaviour
        {
            

            ///// <summary>
            ///// Get the save data for all container managers
            ///// </summary>
            ///// <returns>data for all container managers</returns>
            //public static IEnumerable<DataStore> Save()
            //{
            //    using (var enumerator = ManagedContainerLookup.GetEnumerator())
            //    {
            //        while (enumerator.MoveNext())
            //        {
            //            if (enumerator.Current.Value.HasAnyPipes)
            //                yield return new DataStore(enumerator.Current.Value);
            //        }
            //    }
            //}

            //private static void LogLoadError(DataStore data)
            //{
            //    Logger.ContainerLoader.Log("------------------- {0} -------------------", data.ContainerId);
            //    Logger.ContainerLoader.Log("Container Type: {0}", data.ContainerType);
            //    Logger.ContainerLoader.Log("Display Name: {0}", data.DisplayName);
            //    Logger.ContainerLoader.Log("");
            //}

            ///// <summary>
            ///// Load all data into the container managers.
            ///// This must be run after Pipe.Load as it only updates container managers created by the pipes.
            ///// </summary>
            ///// <param name="dataToLoad">DataStore to load into container managers</param>
            //public static void Load(List<DataStore> dataToLoad)
            //{
            //    if (dataToLoad == null) return;
            //    var containerCount = 0;
            //    for(int i = 0; i < dataToLoad.Count; i++)
            //    {
            //        ContainerManager manager;
            //        if (ContainerHelper.IsComplexStorage(dataToLoad[i].ContainerType))
            //        {
            //            var entity = ContainerHelper.Find(dataToLoad[i].ContainerId, dataToLoad[i].ContainerType);
            //            dataToLoad[i].ContainerId = entity?.net.ID ?? 0;
            //        }
            //        if (ManagedContainerLookup.TryGetValue(dataToLoad[i].ContainerId, out manager))
            //        {
            //            containerCount++;
            //            manager.DisplayName = dataToLoad[i].DisplayName;
            //            manager.CombineStacks = dataToLoad[i].CombineStacks;
            //        }
            //        else
            //        {
            //            Instance.PrintWarning("Failed to load manager [{0} - {1} - {2}]: Container not found", dataToLoad[i].ContainerId, dataToLoad[i].ContainerType, dataToLoad[i].DisplayName);
            //            LogLoadError(dataToLoad[i]);
            //        }
            //    }
            //    Instance.Puts("Successfully loaded {0} managers", containerCount);
            //}

            /// <summary>
            ///     Keeps track of all the container managers that have been created.
            /// </summary>
            internal static readonly Dictionary<uint, ContainerManager> ManagedContainerLookup =
                new Dictionary<uint, ContainerManager>();
            public static readonly List<ContainerManager> ManagedContainers = new List<ContainerManager>();

            // Which pipes have been attached to this container manager
            //private readonly Dictionary<ulong, Pipe> _attachedPipeLookup = new Dictionary<ulong, Pipe>();
            private readonly List<Pipe> _attachedPipes = new List<Pipe>();

            // Pull from multiple stack of the same type whe moving or only move one stack per priority level
            // This has been implemented but the controlling systems have not been developed
            public bool CombineStacks { get; internal set; } = true;

            public StorageContainer Container { get; set; } // The storage container this manager is attached to
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

            private bool _isNotAttachedToABuilding  = false;

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
                containerManager.Container = container;
                containerManager.ContainerType = ContainerHelper.GetEntityType(container);
                if(container.buildingID == 0)
                    containerManager._isNotAttachedToABuilding = true;
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
                    Logger.Runtime.LogException(e, nameof(Detach));
                }
            }


            public BuildingBlock GetNearbyBuildingBlock(DecayEntity decayEntity)
            {
                float num1 = float.MaxValue;
                BuildingBlock nearbyBuildingBlock = (BuildingBlock)null;
                Vector3 position = decayEntity.PivotPoint();
                List<BuildingBlock> list = Facepunch.Pool.GetList<BuildingBlock>();
                Vis.Entities<BuildingBlock>(position, 1.5f, list, 2097152);
                for (int index = 0; index < list.Count; ++index)
                {
                    BuildingBlock buildingBlock = list[index];
                    PipeSegment segment = null;
                    if (buildingBlock.isServer == decayEntity.isServer && !buildingBlock.TryGetComponent(out segment))
                    {
                        float num2 = buildingBlock.SqrDistance(position);
                        if (!buildingBlock.grounded)
                            ++num2;
                        if ((double)num2 < (double)num1)
                        {
                            num1 = num2;
                            nearbyBuildingBlock = buildingBlock;
                        }
                    }
                }
                Facepunch.Pool.FreeList<BuildingBlock>(ref list);
                return nearbyBuildingBlock;
            }

            /// <summary>
            /// Hook: Check container and if still valid and cycle time has elapsed then move items along pipes
            /// </summary>
            private void Update()
            {
                try
                {
                    if (!_isNotAttachedToABuilding && Container.buildingID == 0)
                    {
                        var building = GetNearbyBuildingBlock(Container)?.GetBuilding();
                        if (building != null)
                        {
                            Instance.Puts("Re-attaching containers to building after building split");
                            Container.AttachToBuilding(building.ID);
                        }
                    }
                    if (Container == null)
                        Kill();
                    if (_destroyed || !HasAnyPipes) return;
                    _cumulativeDeltaTime += Time.deltaTime;
                    if (_cumulativeDeltaTime < InstanceConfig.UpdateRate) return;
                    _cumulativeDeltaTime = 0f;
                    if (Container.inventory.itemList.Count == 0 || Container.inventory.itemList[0] == null)
                        return;
                    var pipeGroups = new Dictionary<int, Dictionary<int, List<Pipe>>>();
                    for (var i = 0; i < _attachedPipes.Count; i++)
                    {
                        var pipe = _attachedPipes[i];
                        if (_attachedPipes[i].Source.Container != Container || !_attachedPipes[i].IsEnabled)
                            continue;
                        var priority = (int) pipe.Priority;
                        var grade = (int) pipe.Grade;
                        if (!pipeGroups.ContainsKey(priority))
                            pipeGroups.Add(priority, new Dictionary<int, List<Pipe>>());
                        if (!pipeGroups[priority].ContainsKey(grade))
                            pipeGroups[priority].Add(grade, new List<Pipe>());
                        pipeGroups[priority][grade].Add(pipe);
                    }

                    //var pipeGroups = _attachedPipeLookup.Values.Where(a => a.Source.ContainerManager == this)
                    //    .GroupBy(a => a.Priority).OrderByDescending(a => a.Key).ToArray();
                    for (int i = (int) Pipe.PipePriority.Highest; i > (int) Pipe.PipePriority.Demand; i--)
                    {
                        if (!pipeGroups.ContainsKey(i)) continue;
                        if (CombineStacks)
                            MoveCombineStacks(pipeGroups[i]);
                        else
                            MoveIndividualStacks(pipeGroups[i]);
                    }
                }
                catch (Exception e)
                {
                    Logger.Runtime.LogException(e, nameof(Update));
                }
            }


            private List<Item> ItemList
            {
                get
                {
                    if (Container is Recycler)
                    {
                        var itemList = new List<Item>();
                        for (int i = 6; i < 12; i++)
                        {
                            var item = Container.inventory.GetSlot(i);
                            if (item == null) continue;
                            itemList.Add(item);
                        }
                        return itemList;
                    }

                    return Container.inventory.itemList;
                }
            }

            public ContainerType ContainerType { get; set; }

            private MovableType CanPuItem(Item item)
            {
                try
                {
                    if (!(Container is BaseOven)) return MovableType.Allowed;
                    if (!CanCook(item)) return MovableType.Rejected;
                    var burnable = OvenFuel(item);
                    if (burnable.HasValue)
                        return burnable.GetValueOrDefault() ? MovableType.Fuel : MovableType.Rejected;
                    return CorrectOven(item) ? MovableType.Cookable : MovableType.Rejected;
                }
                catch (Exception e)
                {
                    Logger.Runtime.LogException(e, nameof(CanPuItem));
                    return MovableType.Rejected;
                }
            }

            private static bool CanCook(Item item)
            {
                return !(item.info.category != ItemCategory.Resources &&
                       item.info.category != ItemCategory.Food ||
                       item.info.shortname.EndsWith("cooked") ||
                       item.info.shortname.EndsWith("burned"));
            }

            private bool CorrectOven(Item item)
            {
                var oven = Container as BaseOven;
                if (oven == null) return false;
                ItemModCookable cookable = null;
                return item.info.TryGetComponent(out cookable) &&
                       cookable.lowTemp <= oven.cookingTemperature &&
                       cookable.highTemp >= oven.cookingTemperature;
            }

            private bool? OvenFuel(Item item)
            {
                var oven = Container as BaseOven;
                if (oven == null) return null;
                ItemModBurnable burnable = null;
                if (item.info.TryGetComponent(out burnable))
                    return oven.fuelType.Equals(item.info);
                return null;
            }

            private bool CanTakeItem(Item item)
            {
                try
                {
                    if (!(Container is BaseOven)) return true;
                    if (!CanCook(item)) return true;
                    var burnable = OvenFuel(item);
                    if (burnable.HasValue)
                        return !burnable.GetValueOrDefault();
                    return !CorrectOven(item);
                }
                catch (Exception e)
                {
                    Logger.Runtime.LogException(e, nameof(CanTakeItem));
                    return false;
                }
            }

            private enum MovableType
            {
                Allowed,
                Cookable,
                Fuel,
                Rejected
            }


            /// <summary>
            ///     Attempt to move all items from all stacks of the same type down the pipes in this priroity group
            ///     Items will be split as evenly as possible down all the pipes (limited by flow rate)
            /// </summary>
            /// <param name="pipeGroup">Pipes grouped by their priority</param>
            private void MoveCombineStacks(Dictionary<int, List<Pipe>> pipeGroup)
            {
                try
                {
                    var distinctItemIds = new List<int>();
                    var distinctItems = new Dictionary<int, List<Item>>();
                    var itemList = ItemList;
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
                            var vendingMachine = pipe.Destination.Storage as VendingMachine;
                            var oven = pipe.Destination.Storage as BaseOven;
                            if (vendingMachine != null)
                            {
                                var sellableItem = false;
                                for (int j = 0; j < vendingMachine.sellOrders.sellOrders.Count; j++)
                                {
                                    var sellOrder = vendingMachine.sellOrders.sellOrders[j];
                                    if (sellOrder.itemToSellID == item[0].info.itemid)
                                    {
                                        sellableItem = true;
                                        break;
                                    }
                                }
                                if (!sellableItem)
                                    continue;
                            }
                            else if (oven != null)
                            {
                                var allowedSlots = oven.GetAllowedSlots(item[0]);
                                if (!allowedSlots.HasValue)
                                    continue;
                            }
                            else if (pipe.Destination.Storage.inventory.CanAcceptItem(item[0], 0) ==
                                     ItemContainer.CanAcceptResult.CannotAccept)
                            {
                                continue;
                            }

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
                        for (var i = 0; i < validPipes.Count; i++)
                        {
                            var validPipe = validPipes[i];
                            var recycler = validPipe.Destination.Storage as Recycler;
                            if (recycler != null && !recycler.RecyclerItemFilter(item[0], -1))
                                continue;
                            var canPut = validPipe.Destination.ContainerManager.CanPuItem(item[0]);
                            var canTake = CanTakeItem(item[0]);
                            if (canPut == MovableType.Rejected || !canTake)
                                continue;
                            var amountToMove = GetAmountToMove(itemId, quantity, pipesLeft--, validPipe,
                                item[0]?.MaxStackable() ?? 0, validPipe.IsMultiStack && canPut != MovableType.Fuel);
                            if (amountToMove <= 0)
                                break;
                            quantity -= amountToMove;
                            for (var j = 0; j < item.Count; j++)
                            {
                                var itemStack = item[j];
                                var toMove = itemStack;
                                if (amountToMove <= 0) break;
                                if (amountToMove < itemStack.amount)
                                    toMove = itemStack.SplitItem(amountToMove);

                                unusedPipes.Remove(validPipe);
                                if (Instance.FurnaceSplitter != null &&
                                    canPut != MovableType.Fuel &&
                                    validPipe.Destination.ContainerType == ContainerType.Oven &&
                                    validPipe.IsFurnaceSplitterEnabled && validPipe.FurnaceSplitterStacks > 1)
                                {
                                    var result = Instance.FurnaceSplitter.Call("MoveSplitItem", toMove,
                                        validPipe.Destination.Storage,
                                        validPipe.FurnaceSplitterStacks);
                                    if (!result.ToString().Equals("ok", StringComparison.InvariantCultureIgnoreCase))
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
                catch (Exception e)
                {
                    Logger.Runtime.LogException(e, nameof(MoveCombineStacks));
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
                        var item = Container.inventory.itemList.Count > 0 ? Container.inventory.itemList[0] : null;
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
            private int GetAmountToMove(int itemId, int itemQuantity, int pipesLeft, Pipe pipe, int maxStackable, bool multiStack)
            {
                try
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
                    if (!multiStack)
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
                catch (Exception e)
                {
                    Logger.Runtime.LogException(e, nameof(GetAmountToMove));
                    return 0;
                }
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
                try
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
                catch (Exception e)
                {
                    Logger.Runtime.LogException(e, nameof(GetItemToMove));
                    return item;
                }
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
        }

    }
}