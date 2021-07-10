using UnityEngine;

namespace Oxide.Plugins
{
    public partial class SyncPipesDevelopment
    {
        /// <summary>
        /// This class holds all the parameters to do with the containers at each end of the pipe.
        /// This makes swapping direction easier as you just swap these objects
        /// </summary>
        public class PipeEndContainer
        {
            // The pipe this goes with
            private readonly Pipe _pipe;

            /// <summary>
            /// Creates a pipe end container and fetches all the required variables that may be needed later
            /// </summary>
            /// <param name="container">The container this pipe end is connected to</param>
            /// <param name="containerType">The type of the entity this pipe end is connected to</param>
            /// <param name="pipe">The pipe this is an end for</param>
            public PipeEndContainer(BaseEntity container, ContainerType containerType, Pipe pipe)
            {
                _pipe = pipe;
                Container = container;
                Id = container?.net.ID ?? 0;
                Storage = ContainerHelper.Find(container);
                ContainerType = containerType;
                IconUrl = StorageHelper.GetImageUrl(Container);// ItemIcons.GetIcon(Entity);
                CanAutoStart = ContainerType != ContainerType.General && ContainerType != ContainerType.ResourceExtractor;
            }

            /// <summary>
            /// Attach the pipe end to the container manager for the container
            /// This is done separately to allow the pipe to validate the container before attaching it to the container manager
            /// </summary>
            public void Attach()
            {
                ContainerManager = ContainerManager.Attach(Container, Storage, _pipe);
            }

            /// <summary>
            /// The container Id
            /// </summary>
            public uint Id {get;}

            /// <summary>
            /// The container Entity
            /// </summary>
            public BaseEntity Container { get; }

            /// <summary>
            /// The container's Container Manager
            /// </summary>
            public ContainerManager ContainerManager { get; private set; }

            /// <summary>
            /// The container's storage
            /// </summary>
            public StorageContainer Storage { get; }

            /// <summary>
            /// Whether this is an Oven, Recycler, Quarry or General Storage Entity
            /// </summary>
            public ContainerType ContainerType { get; }

            /// <summary>
            /// The connection position of this container
            /// </summary>
            public Vector3 Position { get; set; }

            /// <summary>
            /// The url of this container to display in the pipe menu
            /// </summary>
            public string IconUrl { get; }

            /// <summary>
            /// Can this container be auto started
            /// </summary>
            public bool CanAutoStart { get; }

            /// <summary>
            /// Attempt to start the container
            /// </summary>
            public void Start()
            {
                switch (ContainerType)
                {
                    case ContainerType.Oven:
                        if (Instance.QuickSmelt != null && !((BaseOven)Container).IsOn()) 
                            Instance.QuickSmelt?.Call("OnOvenToggle", Container,
                                BasePlayer.Find(_pipe.OwnerId.ToString()));
                        if(!((BaseOven)Container).IsOn())
                            ((BaseOven) Container)?.StartCooking();
                        break;
                    case ContainerType.Recycler:
                        if(!(Container as Recycler)?.IsOn() ?? false)
                            (Container as Recycler)?.StartRecycling();
                        break;
                    case ContainerType.FuelStorage:
                        if(!Container.GetComponentInParent<MiningQuarry>().IsEngineOn())
                            Container.GetComponentInParent<MiningQuarry>().EngineSwitch(true);
                        break;
                }
            }

            /// <summary>
            /// Attempt to stop the container
            /// Not yet used...
            /// </summary>
            public void Stop()
            {
                switch (ContainerType)
                {
                    case ContainerType.Oven:
                        (Container as BaseOven)?.StopCooking();
                        break;
                    case ContainerType.Recycler:
                        (Container as Recycler)?.StopRecycling();
                        break;
                    case ContainerType.FuelStorage:
                        Container.GetComponentInParent<MiningQuarry>().EngineSwitch(false);
                        break;
                }
            }

            /// <summary>
            /// Verifies that the container has the fuel it needs to start
            /// </summary>
            /// <returns>True if it has fuel</returns>
            public bool HasFuel()
            {
                switch (ContainerType)
                {
                    case ContainerType.Oven:
                        return (Container as BaseOven)?.FindBurnable() != null;
                    case ContainerType.FuelStorage:
                        var items = Storage.inventory.itemList;
                        for (var i = 0; i < items.Count; i++)
                        {
                            if (items[i].info.name == "fuel.lowgrade.item")
                                return true;
                        }
                        return false;
                    case ContainerType.Recycler:
                        return true;
                    default:
                        return false;
                }
            }
        }
    }
}
