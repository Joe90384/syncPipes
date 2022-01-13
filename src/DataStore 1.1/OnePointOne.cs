using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;
using Oxide.Core;
using UnityEngine;

namespace Oxide.Plugins
{
    partial class SyncPipesDevelopment
    {
        internal partial class DataStore
        {
            internal partial class OnePointOne : MonoBehaviour
            {
                private const string Version = "1.1";
                private static string FilenameVersion => Version.Replace('.', '-');
                private static string _filename;
                private static string Filename => _filename ?? (_filename = $"{Instance.Name}_v{FilenameVersion}");

                private static Coroutine _coroutine;
                private static bool _saving;
                private static bool _loading;
                private static GameObject _saverGameObject;
                private static OnePointOne _dataStore;

                private static OnePointOne DataStore
                {
                    get
                    {
                        if (_dataStore == null)
                        {
                            _saverGameObject =
                                new GameObject($"{Instance.Name.ToLower()}-datastore-{FilenameVersion}");
                            _dataStore = _saverGameObject.AddComponent<OnePointOne>();
                        }

                        return _dataStore;
                    }
                }

                public static bool Save(bool backgroundSave = true)
                {
                    try
                    {
                        if (_loading)
                        {
                            Instance.PrintWarning($"V{Version} Save Skipped. Pipes still loading.");
                            return false;
                        }

                        if (!backgroundSave && _saving)
                        {
                            if (_coroutine != null)
                                DataStore.StopCoroutine(_coroutine);
                            _saving = false;
                        }
                        else if (_saving)
                        {
                            Instance.PrintWarning($"V{Version} Save Skipped. Save in progress.");
                            return false;
                        }

                        try
                        {
                            _saving = true;
                            if (backgroundSave)
                                _coroutine = DataStore.StartCoroutine(DataStore.BufferedSave(Filename));
                            else
                            {
                                var enumerator = DataStore.BufferedSave(Filename);
                                while (enumerator.MoveNext())
                                {
                                }
                            }

                            return true;
                        }
                        finally
                        {
                            _saving = false;
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Runtime.LogException(e, "OnePointOne.Save");
                        _saving = false;
                        return false;
                    }
                }

                public static bool Load()
                {
                    try
                    {
                        _loading = true;
                        var filename = Filename;
                        if (!Interface.Oxide.DataFileSystem.ExistsDatafile(filename))
                        {
                            Instance.PrintWarning($"Failed to find V{Version} data file ({Filename}).");
                            _loading = false;
                            return false;
                        }

                        _coroutine = DataStore.StartCoroutine(DataStore.BufferedLoad(filename));
                        return true;
                    }
                    catch (Exception e)
                    {
                        _loading = false;
                        Logger.Runtime.LogException(e, "OnePointOne.Load");
                        Instance.PrintError($"Load error in V{Version} data file. See logs for more details.");
                        return true; // File exists but load failed return true to prevent V1.0 upgrade.
                    }
                }

                IEnumerator BufferedSave(string filename)
                {
                    var sw = Stopwatch.StartNew();
                    yield return null;
                    Instance.Puts($"Save v{Version} starting");
                    var buffer = new WriteDataBuffer();
                    var pipeSnapshot = new List<Pipe>(Pipe.Pipes);
                    var containerSnapshot = new List<ContainerManager>(ContainerManager.ManagedContainers);
                    for (int i = 0; i < pipeSnapshot.Count; i++)
                    {
                        try
                        {
                            buffer.Pipes.Add(JsonConvert.SerializeObject(pipeSnapshot[i], Formatting.None, new PipeConverter()));
                        }
                        catch (Exception e)
                        {
                            Logger.Runtime.LogException(e, "OnePointOne.BufferedSave.Pipe");
                        }
                        yield return null;
                        try
                        {
                            buffer.Factories.Add(JsonConvert.SerializeObject(pipeSnapshot[i], Formatting.None, new PipeFactoryDataConverter()));
                        }
                        catch (Exception e)
                        {
                            Logger.Runtime.LogException(e, "OnePointOne.BufferedSave.Factory");
                        }
                        yield return null;
                    }

                    Instance.Puts("Saved {0} pipes", buffer.Pipes.Count);
                    for (int i = 0; i < containerSnapshot.Count; i++)
                    {
                        try
                        {
                            if (!containerSnapshot[i].HasAnyPipes) continue;
                            buffer.Containers.Add(JsonConvert.SerializeObject(containerSnapshot[i], Formatting.None,
                                new ContainerManagerDataConverter()));
                        }
                        catch (Exception e)
                        {
                            Logger.Runtime.LogException(e, "OnePointOne.BufferedSave.ContainerManager");
                        }

                        yield return null;
                    }

                    Instance.Puts("Saved {0} managers", buffer.Containers.Count);
                    Interface.Oxide.DataFileSystem.WriteObject(filename, buffer);
                    Interface.Oxide.DataFileSystem.GetDatafile($"{Instance.Name}").Clear();
                    Instance.Puts("Save v{2} complete ({0}.{1:00}s)", sw.Elapsed.Seconds, sw.Elapsed.Milliseconds, Version);
                    sw.Stop();
                    _saving = false;
                    yield return null;
                }

                IEnumerator BufferedLoad(string filename)
                {
                    try
                    {
                        yield return null;
                        Instance.Puts($"Load v{Version} starting");
                        var readDataBuffer = Interface.Oxide.DataFileSystem.ReadObject<ReadDataBuffer>(filename);
                        Instance.Puts(
                            $"Read {{0}} pipes, {{1}} pipe factories and {{2}} container managers from {filename}",
                            readDataBuffer.Pipes.Count, readDataBuffer.Factories.Count,
                            readDataBuffer.Containers.Count);
                        var validPipes = 0;
                        for (int i = 0; i < readDataBuffer.Pipes.Count; i++)
                        {
                            var pipe = readDataBuffer.Pipes[i];
                            try
                            {
                                var factoryData = readDataBuffer.Factories[i];
                                PipeFactoryBase factory = null;
                                var segmentError = false;
                                if (factoryData.IsBarrel)
                                    factory = new PipeFactoryBarrel(pipe);
                                else
                                    factory = new PipeFactoryLowWall(pipe);
                                for (int j = 0; j < factoryData.SegmentEntityIds.Length; j++)
                                {
                                    var segment =
                                        (BaseEntity) BaseNetworkable.serverEntities.Find(
                                            factoryData.SegmentEntityIds[j]);
                                    if (segment == null)
                                    {
                                        Instance.Puts(
                                            $"Pipe {pipe.Id}: Segment not found. Pipe will be recreated.");
                                        segmentError = true;
                                        break;
                                    }

                                    factory.AttachPipeSegment(segment);
                                }

                                for (int j = 0; j < factoryData.LightEntityIds.Length; j++)
                                {
                                    var lights =
                                        (BaseEntity) BaseNetworkable.serverEntities.Find(
                                            factoryData.LightEntityIds[j]);
                                    if (lights == null)
                                    {
                                        Instance.Puts($"Pipe {pipe.Id}: Lights not found. Pipe will be recreated.");
                                        segmentError = true;
                                        break;
                                    }

                                    factory.AttachLights(lights);
                                }
                                Instance.Puts("Pipe Validity: {0}", pipe.Validity);

                                //If any segments or lights are missing remove them all and let the factory recreate.
                                if (
                                    !InstanceConfig.Experimental.PermanentEntities || 
                                    segmentError || 
                                    factory.Segments.Count == 0 || 
                                    pipe.Validity != Pipe.Status.Success
                                    )
                                {
                                    if (!factory.PrimarySegment?.IsDestroyed ?? false)
                                        factory.PrimarySegment?.Kill();
                                }
                                else
                                    pipe.Factory = factory;

                                if (pipe.Validity == Pipe.Status.Success)
                                {
                                    readDataBuffer.Pipes[i].Create();
                                    validPipes++;
                                }
                                else
                                {
                                    Instance.Puts("Failed to read pipe {0}({1})", pipe.DisplayName ?? pipe.Id.ToString(),
                                        pipe.OwnerId);
                                }
                            }
                            catch (Exception e)
                            {
                                Logger.PipeLoader.LogException(e, "Pipe Creation");
                            }
                            yield return null;
                        }

                        Instance.Puts("Successfully loaded {0} of {1} pipes", validPipes, readDataBuffer.Pipes.Count);
                        var dataToLoad = readDataBuffer.Containers;
                        if (dataToLoad != null)
                        {
                            var validContainers = 0;
                            for (int i = 0; i < dataToLoad.Count; i++)
                            {
                                ContainerManager manager;
                                if (ContainerHelper.IsComplexStorage(dataToLoad[i].ContainerType))
                                {
                                    var entity = ContainerHelper.Find(dataToLoad[i].ContainerId,
                                        dataToLoad[i].ContainerType);
                                    dataToLoad[i].ContainerId = entity?.net.ID ?? 0;
                                }

                                if (ContainerManager.ManagedContainerLookup.TryGetValue(dataToLoad[i].ContainerId,
                                        out manager))
                                {
                                    validContainers++;
                                    manager.DisplayName = dataToLoad[i].DisplayName;
                                    manager.CombineStacks = dataToLoad[i].CombineStacks;
                                }
                                else
                                {
                                    Instance.PrintWarning(
                                        "Failed to load manager [{0} - {1} - {2}]: Container not found",
                                        dataToLoad[i].ContainerId, dataToLoad[i].ContainerType,
                                        dataToLoad[i].DisplayName);
                                    LogLoadError(dataToLoad[i]);
                                }

                                yield return null;
                            }

                            Instance.Puts("Successfully loaded {0} of {1} managers", validContainers,
                                readDataBuffer.Containers.Count);
                        }

                        Instance.Puts($"Load v{Version} complete");
                        yield return null;
                    }
                    finally
                    {
                        _loading = false;
                    }
                }
            }
        }
    }
}