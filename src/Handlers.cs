namespace Oxide.Plugins
{
    public partial class SyncPipesDevelopment
    {
        /// <summary>
        /// This class holds all the handlers for various events that the player can carry out like hitting containers or pipes
        /// They will all return true if they have handled the event
        /// </summary>
        static class Handlers
        {
            /// <summary>
            /// This will handle if a container or pipe is hit whilst the player is in naming mode
            /// </summary>
            /// <param name="playerHelper">Player Helper for the player invoking the hammer hit</param>
            /// <param name="entity">The entity hit by the hammer</param>
            /// <returns>True indicates the hit was handled</returns>
            public static bool HandleNamingContainerHit(PlayerHelper playerHelper, BaseEntity entity)
            {
                var containerManager = entity?.GetComponent<ContainerManager>();
                var pipe = entity?.GetComponent<PipeSegmentBase>()?.Pipe;
                if (playerHelper.State != PlayerHelper.UserState.Naming)
                    return false;
                if (!playerHelper.IsUser)
                {
                    playerHelper.ShowOverlay(Overlay.NotAuthorisedOnSyncPipes);
                    OverlayText.Hide(playerHelper.Player, 2f);
                    return false;
                }
                if (containerManager != null && containerManager.HasAnyPipes)
                    containerManager.DisplayName = playerHelper.NamingName;
                else if (pipe != null)
                    pipe.DisplayName = playerHelper.NamingName;
                else
                {
                    playerHelper.ShowOverlay(Overlay.CannotNameContainer);
                    playerHelper.ShowNamingOverlay(2f);
                    return true;
                }
                playerHelper.StopNaming();
                return true;
            }

            /// <summary>
            /// Handle a Hammer Hit on a Container when Placing.
            /// </summary>
            /// <param name="playerHelper">Player Helper for the player invoking the hammer hit</param>
            /// <param name="entity">The entity hit by the hammer</param>
            /// <returns>True indicates the hit was handled</returns>
            public static bool HandlePlacementContainerHit(PlayerHelper playerHelper, BaseEntity entity)
            {

                if (playerHelper.State != PlayerHelper.UserState.Placing || 
                    playerHelper.Destination != null ||
                    ContainerHelper.IsBlacklisted(entity) || 
                    entity.GetComponent<StorageContainer>() == null ||
                    entity.GetComponent<PipeSegment>() != null
                    ) 
                    return false;
                if (!playerHelper.IsUser)
                {
                    playerHelper.ShowOverlay(Overlay.NotAuthorisedOnSyncPipes);
                    OverlayText.Hide(playerHelper.Player, 2f);
                    return false;
                }
                if (!playerHelper.HasContainerPrivilege(entity) || !playerHelper.CanBuild)
                {
                    playerHelper.ShowOverlay(Overlay.NoPrivilegeToCreate);
                    playerHelper.ShowPlacingOverlay(2f);
                }
                else
                {
                    if (playerHelper.Source == null)
                    {
                        playerHelper.Source = entity;
                    }
                    else
                    {
                        playerHelper.Destination = entity;
                        Pipe.TryCreate(playerHelper);
                        return true;
                    }
                    playerHelper.ShowPlacingOverlay();
                }
                return true;
            }

            /// <summary>
            /// Handle a hammer hit on a pipe whilst the player is in naming mode
            /// </summary>
            /// <param name="playerHelper">Player Helper for the player invoking the hammer hit</param>
            /// <param name="entity">The entity hit by the hammer</param>
            /// <returns>True indicates the hit was handled</returns>
            public static bool HandlePipeCopy(PlayerHelper playerHelper, BaseEntity entity)
            {
                var pipe = entity?.GetComponent<PipeSegmentBase>()?.Pipe;
                if (playerHelper.State != PlayerHelper.UserState.Copying || pipe == null) return false;
                if (!playerHelper.IsUser)
                {
                    playerHelper.ShowOverlay(Overlay.NotAuthorisedOnSyncPipes);
                    OverlayText.Hide(playerHelper.Player, 2f);
                    return false;
                }
                if (playerHelper.CanBuild)
                {
                    if (playerHelper.CopyFrom == null)
                        playerHelper.CopyFrom = pipe;
                    else
                        pipe.CopyFrom(playerHelper.CopyFrom);
                    playerHelper.ShowCopyOverlay();
                }
                else
                {
                    playerHelper.ShowOverlay(Overlay.NoPrivilegeToEdit);
                    OverlayText.Hide(playerHelper.Player, 3f);
                }

                return true;
            }

            /// <summary>
            /// Handle a hammer hit on a pipe whilst the player is in remove mode
            /// </summary>
            /// <param name="playerHelper">Player Helper for the player invoking the hammer hit</param>
            /// <param name="entity">The entity hit by the hammer</param>
            /// <returns>True indicates the hit was handled</returns>
            public static bool HandlePipeRemove(PlayerHelper playerHelper, BaseEntity entity)
            {
                var pipe = entity?.GetComponent<PipeSegment>()?.Pipe;
                if (playerHelper.State != PlayerHelper.UserState.Removing || pipe == null) return false;
                pipe.Remove();
                playerHelper.ShowRemoveOverlay();
                return true;
            }

            /// <summary>
            /// Handles a hammer hit on a pipe whilst the player is not in any special mode
            /// </summary>
            /// <param name="playerHelper">Player Helper for the player invoking the hammer hit</param>
            /// <param name="entity">The entity hit by the hammer</param>
            /// <returns>True indicates the hit was handled</returns>
            public static bool HandlePipeMenu(PlayerHelper playerHelper, BaseEntity entity)
            {

                var pipe = entity?.GetComponent<PipeSegmentBase>()?.Pipe;
                if (!playerHelper.IsUser)
                {
                    playerHelper.ShowOverlay(Overlay.NotAuthorisedOnSyncPipes);
                    OverlayText.Hide(playerHelper.Player, 2f);
                    return false;
                }
                if (pipe == null || !pipe.CanPlayerOpen(playerHelper)) return false;
                pipe.OpenMenu(playerHelper);
                return true;
            }

            /// <summary>
            /// Handles a hammer hit on a pipe container whilst the player is not in any special mode
            /// </summary>
            /// <param name="playerHelper">Player Helper for the player invoking the hammer hit</param>
            /// <param name="entity">The entity hit by the hammer</param>
            /// <returns>True indicates the hit was handled</returns>
            public static bool HandleContainerManagerHit(PlayerHelper playerHelper, BaseEntity entity)
            {
                return false;
                // var containerManager = entity?.GetComponent<ContainerManager>();
                //
                // if (containerManager == null || !containerManager.HasAnyPipes) return false;
                // if (!playerHelper.IsUser)
                // {
                //     playerHelper.ShowOverlay(Overlay.NotAuthorisedOnSyncPipes);
                //     OverlayText.Hide(playerHelper.Player, 2f);
                //     return false;
                // }
                // var container = entity as StorageContainer;
                // //ToDo: Implement this...
                // return true;
            }

            /// <summary>
            /// Handles any upgrades to ensure pipes are upgraded correctly when any pipe segments are upgraded
            /// </summary>
            /// <param name="entity">The entity hit by the hammer</param>
            /// <param name="playerHelper">Player Helper for the player invoking the hammer hit</param>
            /// <param name="grade">The grade the structure has been upgraded to</param>
            /// <returns>True indicates the upgrade was handled</returns>
            public static bool? HandlePipeUpgrade(BaseCombatEntity entity, PlayerHelper playerHelper, BuildingGrade.Enum grade)
            {
                var pipe = entity?.GetComponent<PipeSegment>()?.Pipe;
                if (pipe == null || playerHelper == null) return null;
                var maxUpgrade = playerHelper.MaxUpgrade;
                if (!playerHelper.IsUser)
                {
                    playerHelper.ShowOverlay(Overlay.NotAuthorisedOnSyncPipes);
                    OverlayText.Hide(playerHelper.Player, 2f);
                    return false;
                }
                if(maxUpgrade != -1 && maxUpgrade < (int)grade)
                {
                    playerHelper.ShowOverlay(Overlay.UpgradeLimitReached);
                    OverlayText.Hide(playerHelper.Player, 2f);
                    return false;
                }
                pipe.Upgrade(grade);
                return null;
            }
        }
    }
}
