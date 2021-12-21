namespace Oxide.Plugins
{
    partial class SyncPipesDevelopment
    {
        partial class Data
        {
            partial class OnePointZero
            {
                /// <summary>
                /// This is the serializable data format fro loading or saving container manager data
                /// </summary>
                public class ContainerManagerData
                {
                    public uint ContainerId;
                    public bool CombineStacks;
                    public string DisplayName;
                    public ContainerType ContainerType;

                    /// <summary>
                    /// This is required to deserialize from json
                    /// </summary>
                    public ContainerManagerData()
                    {
                    }

                    /// <summary>
                    /// Create data from a container manager for saving
                    /// </summary>
                    /// <param name="containerManager">Container manager to extract settings from</param>
                    public ContainerManagerData(ContainerManager containerManager)
                    {
                        ContainerId = containerManager.ContainerId;
                        CombineStacks = containerManager.CombineStacks;
                        DisplayName = containerManager.DisplayName;
                        ContainerType = ContainerHelper.GetEntityType(containerManager.Container);
                    }
                }
            }
        }
    }
}
