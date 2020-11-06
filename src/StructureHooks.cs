using Rust;

namespace Oxide.Plugins
{
    public partial class SyncPipesDevelopment
    {
        /// <summary>
        /// Hook: Ensures the pipe is demolished if any segments are destroyed
        /// </summary>
        /// <param name="entity">Entity to check if it is a pipe</param>
        /// <param name="player">Player trying to rotate the entity</param>
        /// <param name="immediate">Whether this is an immediate demolish</param>
        void OnStructureDemolish(BaseCombatEntity entity, BasePlayer player, bool immediate) => entity?.GetComponent<PipeSegment>()?.Pipe?.Remove();

        /// <summary>
        /// Hook: Ensures the who pipe is at the same damage level and to prevent decay when this is switched off
        /// </summary>
        /// <param name="entity">Entity to check if it is a pipe</param>
        /// <param name="hitInfo">The damage information</param>
        /// <returns>True to enable the damage handler to continue</returns>
        bool? OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            var pipe = entity?.GetComponent<PipeSegment>()?.Pipe;
            if (pipe == null || hitInfo == null) return null;
            if (InstanceConfig.NoDecay)
                hitInfo.damageTypes.Scale(DamageType.Decay, 0f);
            var damage = hitInfo.damageTypes.Total();
            if (damage > 0)
            {
                var health = entity.GetComponent<BuildingBlock>()?.health;
                if (health.HasValue)
                {
                    health -= damage;
                    if (health >= 1f)
                        pipe.SetHealth(health.Value);
                    else
                        pipe.Remove();
                }
            }
            return true;
        }

        /// <summary>
        /// Hook: Suppresses the can't repair error when hitting a pipe that is full health and
        /// ensuring that the pipe repairs are carried out on all segments simultaneously
        /// </summary>
        /// <param name="entity">Entity to check if it is a pipe</param>
        /// <param name="player">Player to check the state of</param>
        /// <returns>null if no overrides are in place</returns>
        bool? OnStructureRepair(BaseCombatEntity entity, BasePlayer player)
        {
            var playerHelper = PlayerHelper.Get(player);
            if (playerHelper != null && playerHelper.State != PlayerHelper.UserState.None)
            {
                // This flag is used to prevent repairs being done when completing the placement of a pipe.
                if (playerHelper.State == PlayerHelper.UserState.Completing)
                    playerHelper.State = PlayerHelper.UserState.None;
                if (entity.Health().Equals(entity.MaxHealth()))
                    return false;
                return null;
            }
            var pipe = entity.GetComponent<PipeSegment>()?.Pipe;
            if (pipe != null)
                return OnPipeRepair(entity, player, pipe);
            return null;
        }

        /// <summary>
        /// Repair the pipe and all the pipe segments
        /// </summary>
        /// <param name="entity">Primary entity being repaired</param>
        /// <param name="player">Player doing the repair</param>
        /// <param name="pipe">Pipe being repaired</param>
        /// <returns>null on other segments of the pipe to prevent a cascade
        /// false for everything else to prevent the can't repair error</returns>
        private static bool? OnPipeRepair(BaseCombatEntity entity, BasePlayer player, Pipe pipe)
        {
            if ((int)entity.Health() == (int)entity.MaxHealth())
                return false;
            if (pipe.Repairing)
                return null;
            pipe.Repairing = true;
            entity.DoRepair(player);
            pipe.SetHealth(entity.GetComponent<BuildingBlock>().health);
            pipe.Repairing = false;
            return false;
        }

        /// <summary>
        /// Hook: Prevents the pipes from being rotated as this messes up the alignment
        /// </summary>
        /// <param name="entity">Entity to check if it is a pipe</param>
        /// <param name="player">Player trying to rotate the entity</param>
        /// <returns>False if it is a pipe, null if it isn't</returns>
        bool? OnStructureRotate(BaseCombatEntity entity, BasePlayer player) => entity.GetComponent<PipeSegment>() ? (bool?)false : null;

        /// <summary>
        /// Hook: Ensures the all pipe sections are upgraded together
        /// </summary>
        /// <param name="entity">Entity being upgraded</param>
        /// <param name="player">Player performing the upgrade</param>
        /// <param name="grade">New grade for the structure</param>
        /// <returns>null if this is not a pipe</returns>
        bool? OnStructureUpgrade(BaseCombatEntity entity,
            BasePlayer player,
            BuildingGrade.Enum grade) =>
            Handlers.HandlePipeUpgrade(entity, PlayerHelper.Get(player), grade);
    }
}
