namespace Oxide.Plugins
{
    public partial class SyncPipesDevelopment
    {
        /// <summary>
        /// Hook: Used to prevent the lights from the pipes being picked up and displays a warning
        /// </summary>
        /// <param name="player">Player trying to pick up an entity</param>
        /// <param name="entity">entity to check to see if it is the lights from a pipe</param>
        /// <returns>false if the entity is the lights from a pipe</returns>
        bool? CanPickupEntity(BasePlayer player, BaseEntity entity)
        {
            var lights = entity?.GetComponent<PipeSegmentLights>();
            if (lights == null) return null;
            var playerHelper = PlayerHelper.Get(player);
            playerHelper?.ShowOverlay(Overlay.CantPickUpLights);
            OverlayText.Hide(player, 2f);
            return false;
        }

        /// <summary>
        /// Hook: Used to ensure pipes are removed when a segment of the pipe is killed
        /// </summary>
        /// <param name="entity">Entity to check to see if it's a pipe segment</param>
        void OnEntityKill(BaseNetworkable entity) => entity?.GetComponent<PipeSegment>()?.Pipe?.Remove();

        /// <summary>
        /// Hook: Used to ensure pies are removed when a segment of the pipe dies
        /// </summary>
        /// <param name="entity">Entity to check to see if it's a pipe segment</param>
        /// <param name="info"></param>
        void OnEntityDeath(BaseCombatEntity entity, HitInfo info) => entity?.GetComponent<PipeSegment>()?.Pipe?.Remove();

        /// <summary>
        /// Hook: Used to handle hits to the pipes or connected containers
        /// </summary>
        /// <param name="player">Player hitting</param>
        /// <param name="hit">Information about the hit</param>
        void OnHammerHit(BasePlayer player, HitInfo hit)
        {
            if (player == null || hit?.HitEntity == null)
                return;
            var playerHelper = PlayerHelper.Get(player);
            var handled =
                Handlers.HandleNamingContainerHit(playerHelper, hit.HitEntity) ||
                Handlers.HandlePlacementContainerHit(playerHelper, hit.HitEntity) ||
                Handlers.HandlePipeCopy(playerHelper, hit.HitEntity) ||
                Handlers.HandlePipeRemove(playerHelper, hit.HitEntity) ||
                Handlers.HandlePipeMenu(playerHelper, hit.HitEntity) ||
                Handlers.HandleContainerManagerHit(playerHelper, hit.HitEntity);
        }

        /// <summary>
        /// New Hook: This allows other plugins to determine if the entity is a pipe
        /// </summary>
        /// <param name="entity">Entity to check to see if it a pipe</param>
        /// <param name="checkRunning">Only return true if the pipe is also running</param>
        /// <returns>True if the entity is a pipe segment (and if it is running)</returns>
        private bool IsPipe(BaseEntity entity, bool checkRunning = false) => checkRunning ? entity?.GetComponent<PipeSegment>()?.enabled ?? false : entity?.GetComponent<PipeSegment>()?.Pipe != null;

        /// <summary>
        /// New Hook: This allows other plugins to determine if the entity is a managed container.
        /// </summary>
        /// <param name="entity">Entity to check to see if it is a managed container</param>
        /// <returns>True if the entity is a managed container</returns>
        private bool IsManagedContainer(BaseEntity entity) => entity?.GetComponent<ContainerManager>()?.HasAnyPipes ?? false;
    }
}
