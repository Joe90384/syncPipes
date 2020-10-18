using System.Linq;

namespace Oxide.Plugins
{
	partial class SyncPipes
	{
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
            public static bool IsBlacklisted(BaseEntity container) =>
                container is BaseFuelLightSource || container is Locker || container is ShopFront || container is RepairBench;

            /// <summary>
            /// Get a storage container from its Id
            /// </summary>
            /// <param name="id">The Id to search for</param>
            /// <returns>The container that matches the id</returns>
            public static StorageContainer Find(uint id) => Find((BaseEntity) BaseNetworkable.serverEntities.Find(id));

            /// <summary>
            /// Get the container id and the startable type from a container
            /// </summary>
            /// <param name="container">The container to get the data for</param>
            public static ContainerType GetEntityType(BaseEntity container)
            {
                if (container is BaseOven)
                    return ContainerType.Oven;
                else if(container is Recycler)
                    return ContainerType.Recycler;
                else if (container is ResourceExtractorFuelStorage && container.parentEntity.Get(false) is MiningQuarry)
                {
                    switch (((ResourceExtractorFuelStorage) container).panelName)
                    {
                        case "fuelstorage":
                            return ContainerType.FuelStorage;
                        case "generic":
                            return ContainerType.ResourceExtractor;
                    }
                }

                return ContainerType.General;
            }

            public static BaseEntity Find(uint parentId, ContainerType containerType)
            {
                var entity = (BaseEntity)BaseNetworkable.serverEntities.Find(parentId);
                if (containerType != ContainerType.ResourceExtractor && containerType != ContainerType.FuelStorage)
                    return entity;
                return entity?.GetComponent<BaseResourceExtractor>()?.children
                    .OfType<ResourceExtractorFuelStorage>().FirstOrDefault(a =>a.panelName == (containerType == ContainerType.FuelStorage ? "fuelstorage" : "generic"));
            }

            public static StorageContainer Find(BaseEntity parent) => parent?.GetComponent<StorageContainer>();
        }

        /// <summary>
        /// Entity Types
        /// </summary>
        enum ContainerType
        {
            General,
            Oven,
            Recycler,
            FuelStorage,
            ResourceExtractor
        }
	}
}
