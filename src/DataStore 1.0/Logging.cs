namespace Oxide.Plugins
{
    partial class SyncPipesDevelopment
    {
        partial class DataStore
        {
            partial class OnePointZero
            {
                private static void LogLoadError(ContainerManagerData containerManagerData,
                    string message = null)
                {
                    Logger.ContainerLoader.Log("------------------- {0} -------------------",
                        containerManagerData.ContainerId);
                    if (!string.IsNullOrEmpty(message))
                        Logger.ContainerLoader.Log(message);
                    Logger.ContainerLoader.Log("Container Type: {0}", containerManagerData.ContainerType);
                    Logger.ContainerLoader.Log("Display Name: {0}", containerManagerData.DisplayName);
                    Logger.ContainerLoader.Log("");
                }
                
                private static void LogLoadError(Pipe pipe, uint sourceId, uint destinationId,
                    string message = null)
                {
                    Logger.PipeLoader.Log("------------------- {0} -------------------", pipe.Id);
                    if (!string.IsNullOrEmpty(message))
                        Logger.PipeLoader.Log("Error: {0}", message);
                    Logger.PipeLoader.Log("Status: {0}", pipe.Validity);
                    Logger.PipeLoader.Log("Source Id: {0}", sourceId);
                    Logger.PipeLoader.Log("Destination Id: {0}", destinationId);
                    Logger.PipeLoader.Log("Source Type: {0}", pipe.Source?.ContainerType);
                    Logger.PipeLoader.Log("Destination Type: {0}", pipe.Destination?.ContainerType);
                    Logger.PipeLoader.Log("Material: {0}", pipe.Grade);
                    Logger.PipeLoader.Log("Enabled: {0}", pipe.IsEnabled);
                    Logger.PipeLoader.Log("Auto-start: {0}", pipe.IsAutoStart);
                    Logger.PipeLoader.Log("Health: {0}", pipe.InitialHealth);
                    Logger.PipeLoader.Log("Priority: {0}", pipe.Priority);
                    Logger.PipeLoader.Log("Splitter Enabled: {0}", pipe.IsFurnaceSplitterEnabled);
                    Logger.PipeLoader.Log("Splitter Count: {0}", pipe.FurnaceSplitterStacks);
                    Logger.PipeLoader.Log("Item Filter: ({0})", pipe.PipeFilter?.Items.Count);
                    for (var i = 0; i < pipe.PipeFilter?.Items.Count; i++)
                        Logger.PipeLoader.Log("    Item[{0}]: {1}", i,
                            pipe.PipeFilter.Items[i]?.info.displayName.english);
                    Logger.PipeLoader.Log("");
                }
            }
        }
    }
}
