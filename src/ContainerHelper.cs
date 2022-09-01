using System.Collections.Generic;

namespace Oxide.Plugins
{
    public partial class SyncPipesDevelopment
    {

        public const string FUEL_STORAGE_PREFAB = "fuelstorage";
        public const string QUARRY_OUTPUT_PREFAB = "hopperoutput";
        public const string PUMPJACK_OUTPUT_PREFAB = "crudeoutput";

        /// <summary>
        /// This helps find containers and the required information needed to attach pipes
        /// </summary>
        static class ContainerHelper
        {
            /// <summary>
            /// Lists all the container types that pipes cannot connect to
            /// </summary>
            /// <param name="container">The container to check</param>
            /// <returns>True if the container type is blacklisted</returns>
            public static bool IsBlacklisted(BaseEntity container)
            {
                return (InstanceConfig.BlacklistTC && container.ShortPrefabName == ToolCupboardPrefab) || container is BaseFuelLightSource || container is Locker || container is ShopFront ||
                       container is RepairBench || container is LootContainer;
            }

            /// <summary>
            /// Get a storage container from its Id
            /// </summary>
            /// <param name="id">The Id to search for</param>
            /// <returns>The container that matches the id</returns>
            public static StorageContainer Find(uint id)
            {
                return Find((BaseEntity)BaseNetworkable.serverEntities.Find(id));
            }

            /// <summary>
            /// Get the container id and the startable type from a container
            /// </summary>
            /// <param name="container">The container to get the data for</param>
            public static ContainerType GetEntityType(BaseEntity container)
            {

                if (container is BaseOven)
                    return ContainerType.Oven;
                if (container is Recycler)
                    return ContainerType.Recycler;
                if (container is ResourceExtractorFuelStorage)
                {
                    switch (container.ShortPrefabName)
                    {
                        case FUEL_STORAGE_PREFAB:
                            return ContainerType.FuelStorage;
                        case QUARRY_OUTPUT_PREFAB:
                            return ContainerType.QuarryOutput;
                        case PUMPJACK_OUTPUT_PREFAB:
                            return ContainerType.PumpJackOutput;
                    }
                }
                return ContainerType.General;
            }

            public static bool InMonument(BaseEntity entity)
            {
                switch (GetEntityType(entity))
                {
                    case ContainerType.PumpJackOutput:
                    case ContainerType.QuarryOutput:
                    case ContainerType.FuelStorage:
                    case ContainerType.Recycler:
                        for (int i = 0; i < TerrainMeta.Path.Monuments.Count; i++)
                        {
                            var monument = TerrainMeta.Path.Monuments[i];
                            if (monument.IsInBounds(entity.transform.position))
                                return false;
                        }
                        break;
                }

                return true;
            }

            private static void LogFindError(uint parentId, BaseEntity entity, ContainerType containerType, List<BaseEntity> children = null)
            {
                Logger.FindErrors.Log("------------------- {0} -------------------", parentId);
                if (entity == null)
                    Logger.FindErrors.Log("Entity not found");
                else
                    Logger.FindErrors.Log("Entity: {0} ({1})", entity.ShortPrefabName, entity);
                Logger.FindErrors.Log("Type: {0}", containerType);
                for (int i = 0; i < children?.Count; i++)
                    Logger.FindErrors.Log("Child {0}: {1} ({2})", i, children[i].ShortPrefabName, children[i]);
                Logger.FindErrors.Log("");
            }

            public static BaseEntity Find(uint parentId, ContainerType containerType)
            {
                var entity = (BaseEntity) BaseNetworkable.serverEntities.Find(parentId);
                return Find(entity, containerType);
            }

            public static BaseEntity Find(BaseEntity entity, ContainerType containerType)
            {
                if (entity == null)
                {
                    LogFindError(entity.net.ID, null, containerType);
                    return null;
                }

                if (!IsComplexStorage(containerType))
                    return entity;
                BaseResourceExtractor resourceExtractor = null;
                if (entity?.TryGetComponent<BaseResourceExtractor>(out resourceExtractor) ?? false)
                {
                    var children = resourceExtractor.children;
                    var prefabName = GetShortPrefabName(containerType);
                    for (var i = 0; i < children?.Count; i++)
                    {
                        if (children[i].ShortPrefabName == prefabName)
                            return children[i] as ResourceExtractorFuelStorage;
                    }

                    LogFindError(entity.net.ID, entity, containerType, children);
                }
                else
                {
                    LogFindError(entity.net.ID, entity, containerType);
                }
                return null;
            }
            public static StorageContainer Find(BaseEntity parent)
            {
                StorageContainer container = null;
                return parent?.TryGetComponent<StorageContainer>(out container) ?? false ? container : null;
            }

            public static string GetShortPrefabName(ContainerType containerType)
            {
                switch (containerType)
                {
                    case ContainerType.FuelStorage:
                        return FUEL_STORAGE_PREFAB;
                    case ContainerType.QuarryOutput:
                        return QUARRY_OUTPUT_PREFAB;
                    case ContainerType.PumpJackOutput:
                        return PUMPJACK_OUTPUT_PREFAB;
                }
                return "";
            }

            public static bool IsComplexStorage(ContainerType containerType)
            {
                switch (containerType)
                {
                    case ContainerType.FuelStorage:
                    case ContainerType.PumpJackOutput:
                    case ContainerType.QuarryOutput:
                        return true;
                    default:
                        return false;
                }
            }

            public static bool CanAutoStart(ContainerType containerType)
            {
                switch (containerType)
                {
                    case ContainerType.FuelStorage:
                    case ContainerType.Oven:
                    case ContainerType.Recycler:
                        return true;
                    default:
                        return false;
                }

            }
        }

        /// <summary>
        /// Entity Types
        /// </summary>
        public enum ContainerType
        {
            General,
            Oven,
            Recycler,
            FuelStorage,
            QuarryOutput,
            PumpJackOutput,
            ResourceExtractor
        }
    }
}
