using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    public partial class SyncPipesDevelopment
    {
        /// <summary>
        /// This represents the filter for a pipe.
        /// It creates a virtual loot container with the correct items.
        /// </summary>
        public class PipeFilter
        {
            /// <summary>
            /// All items in the virtual filter container
            /// </summary>
            public List<Item> Items => _filterContainer.itemList;

            // A list if players currently viewing the filter
            private readonly List<BasePlayer> _playersInFilter = new List<BasePlayer>();

            /// <summary>
            /// Ensure the filter is cleaned up and all players are disconnect from it when the container is destoryed.
            /// </summary>
            ~PipeFilter()
            {
                Kill();
            }

            /// <summary>
            /// Disconnect all players and empty the container when the filter is destroyed.
            /// </summary>
            public void Kill()
            {
                ForceClosePlayers();
                KillFilter();
            }

            /// <summary>
            /// Destroy the virtual storage container
            /// </summary>
            private void KillFilter()
            {
                _filterContainer?.Kill();
                _filterContainer = null;
                ItemManager.DoRemoves();
            }

            /// <summary>
            /// force Close the filter loot screen for all players currently viewing it
            /// </summary>
            private void ForceClosePlayers()
            {
                foreach (var player in _playersInFilter.ToArray())
                    ForceClosePlayer(player);
            }

            /// <summary>
            /// Force Close the filter loot screen for a specific player
            /// </summary>
            /// <param name="player">Player to close the filter for</param>
            private void ForceClosePlayer(BasePlayer player)
            {
                player?.inventory.loot.Clear();
                player?.inventory.loot.MarkDirty();
                player?.inventory.loot.SendImmediate();
                Closing(player);
            }

            /// <summary>
            /// Remove the player from the list of players in the filter
            /// </summary>
            /// <param name="player">Player closing the menu</param>
            public void Closing(BasePlayer player)
            {
                if(player != null)
                    _playersInFilter.Remove(player);
            }

            /// <summary>
            /// Creates a virtual storage container with all the items from the pipe and limits it to the pipes filter capacity
            /// </summary>
            /// <param name="filterItems">Items to filter by</param>
            /// <param name="capacity">Maximum items allow for the curent pipe grade</param>
            /// <param name="pipe">The pipe this filter is attached to</param>
            public PipeFilter(List<int> filterItems, int capacity, Pipe pipe)
            {
                _pipe = pipe;
                _filterContainer = new ItemContainer
                {
                    entityOwner = pipe.PrimarySegment,
                    isServer = true,
                    allowedContents = ItemContainer.ContentsType.Generic,
                    capacity = capacity,
                    maxStackSize = 1,
                    canAcceptItem = CanAcceptItem

                };
                _filterContainer.GiveUID();
                for (var i = 0; i < capacity && i < filterItems.Count; i++)
                    ItemManager.CreateByItemID(filterItems[i]).MoveToContainer(_filterContainer);
            }

            /// <summary>
            /// Prevents the filter from taking an item from the player but adds a dummy item to the filter
            /// </summary>
            /// <param name="item">Item to add</param>
            /// <param name="position">Stack position to place the item</param>
            /// <returns>False to the hook caller</returns>
            private bool CanAcceptItem(Item item, int position)
            {
                // Checks if the item is in the list of items to add to the filter.
                // If so return true to allow the item to be added.
                if (_addItem.Contains(item))
                {
                    _addItem.Remove(item);
                    return true;
                }
                // Check if the filter already has this item
                if (_filterContainer.FindItemByItemID(item.info.itemid) == null)
                {
                    // Add a dummy item to the list of items to add and then move it to the filter.
                    var filterItem = ItemManager.CreateByItemID(item.info.itemid);
                    _addItem.Add(filterItem);
                    filterItem.MoveToContainer(_filterContainer, position, false);
                }
                // Prevent the item being taken from the player
                return false;
            }

            // List of dummy items to be added to the filter.
            private readonly List<Item> _addItem = new List<Item>();

            /// <summary>
            /// Upgrade the capacity of the filter.
            /// Cannot be less than the previous capacity.
            /// </summary>
            /// <param name="newCapacity"></param>
            public void Upgrade(int newCapacity)
            {
                if (newCapacity < _filterContainer.capacity) return;
                _filterContainer.capacity = newCapacity;
            }

            /// <summary>
            /// Open the filter as a loot box to the player
            /// </summary>
            /// <param name="playerHelper">Player to show filter to</param>
            public void Open(PlayerHelper playerHelper)
            {
                var player = playerHelper.Player;
                if (player == null)
                    return;
                playerHelper.PipeFilter = this;
                if (_playersInFilter.Contains(player) || !Active)
                    return;
                _playersInFilter.Add(player);
                player.inventory.loot.Clear();
                player.inventory.loot.PositionChecks = false;
                player.inventory.loot.entitySource = _pipe.PrimarySegment;
                player.inventory.loot.itemSource = null;
                player.inventory.loot.MarkDirty();
                player.inventory.loot.AddContainer(_filterContainer);
                player.inventory.loot.SendImmediate();
                player.inventory.loot.useGUILayout = false;
                player.ClientRPCPlayer(null, player, "RPC_OpenLootPanel", "generic_resizable");
            }

            private ItemContainer _filterContainer;
            private readonly Pipe _pipe;
            private bool Active => _filterContainer != null;
        }

        /// <summary>
        /// Hook: Close the players connection to the filter when they disconnect from the filter.
        /// Remove the player from the players in filter list
        /// </summary>
        /// <param name="playerLoot">Used to get the player in the filter</param>
        private void OnPlayerLootEnd(PlayerLoot playerLoot) => PlayerHelper.Get((BasePlayer)playerLoot.gameObject.ToBaseEntity())?.CloseFilter();

        /// <summary>
        /// Hook: This is used to prevent the player from removing anything from the filter
        /// </summary>
        /// <param name="container">Container being viewed</param>
        /// <param name="item">Item being removed</param>
        private void OnItemRemovedFromContainer(ItemContainer container, Item item)
        {
            if (container?.entityOwner?.GetComponent<PipeSegment>() != null)
                item?.Remove();
        }

        /// <summary>
        /// Hook: This is used to prevent a filter item being added to an existing stack in the players inventory
        /// </summary>
        /// <param name="item">Item being removed</param>
        /// <param name="targetItem">Stack being added to</param>
        /// <returns>If the item can be stacked</returns>
        private bool? CanStackItem(Item item, Item targetItem) => targetItem?.parent?.entityOwner?.GetComponent<PipeSegment>() != null ? (bool?)false : null;
    }
}
