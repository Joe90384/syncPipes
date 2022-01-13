﻿using System;
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
            internal partial class OnePointZero : MonoBehaviour
            {
                private static Coroutine _coroutine;
                private static bool _saving;
                private static bool _loading;
                private static GameObject _saverGameObject;
                private static OnePointZero _dataStore;

                private static OnePointZero DataStore
                {
                    get
                    {
                        if (_dataStore == null)
                        {
                            _saverGameObject =
                                new GameObject($"{Instance.Name.ToLower()}-datastore-1-0");
                            _dataStore = _saverGameObject.AddComponent<OnePointZero>();
                        }

                        return _dataStore;
                    }
                }

                private static string _filename;
                private static string Filename => _filename ?? (_filename = $"{Instance.Name}_v1-0");
                private static string OldFilename => $"{Instance.Name} v1-0";

                public static bool Save(bool backgroundSave = true)
                {
                    try
                    {
                        if (_loading)
                            return false;
                        if (!backgroundSave && _saving)
                        {
                            if (_coroutine != null)
                                DataStore.StopCoroutine(_coroutine);
                            _saving = false;
                        }
                        else if (_saving)
                            return false;

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
                        Logger.Runtime.LogException(e, "OnePointZero.Save");
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
                        if (!Interface.Oxide.DataFileSystem.ExistsDatafile(Filename))
                        {
                            if (!Interface.Oxide.DataFileSystem.ExistsDatafile(OldFilename))
                                return false;
                            filename = OldFilename;
                        }

                        _coroutine = DataStore.StartCoroutine(DataStore.BufferedLoad(filename));
                        return true;
                    }
                    catch (Exception e)
                    {
                        _loading = false;
                        Logger.Runtime.LogException(e, "OnePointZero.Load");
                        return false;
                    }
                }

                IEnumerator BufferedSave(string filename)
                {
                    var sw = Stopwatch.StartNew();
                    yield return null;
                    Instance.Puts("Save v1.0 starting");
                    var buffer = new WriteDataBuffer();
                    var pipeSnapshot = new List<Pipe>(Pipe.Pipes);
                    var containerSnapshot = new List<ContainerManager>(ContainerManager.ManagedContainers);
                    for (int i = 0; i < pipeSnapshot.Count; i++)
                    {
                        try
                        {
                            buffer.Pipes.Add(JsonConvert.SerializeObject(pipeSnapshot[i], Formatting.None,
                                new PipeConverter()));
                        }
                        catch (Exception e)
                        {
                            Logger.Runtime.LogException(e, "OnePointZero.BufferedSave");
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
                            Logger.Runtime.LogException(e, "OnePointZero.BufferedSave");
                        }

                        yield return null;
                    }

                    Instance.Puts("Saved {0} managers", buffer.Containers.Count);
                    Interface.Oxide.DataFileSystem.WriteObject(filename, buffer);
                    Interface.Oxide.DataFileSystem.GetDatafile($"{Instance.Name}").Clear();
                    Instance.Puts("Save v1.0 complete ({0}.{1:00}s)", sw.Elapsed.Seconds, sw.Elapsed.Milliseconds);
                    sw.Stop();
                    _saving = false;
                    yield return null;
                }

                IEnumerator BufferedLoad(string filename)
                {
                    try
                    {
                        yield return null;
                        Instance.Puts("Load v1.0 starting");
                        var readDataBuffer = Interface.Oxide.DataFileSystem.ReadObject<ReadDataBuffer>(filename);
                        var validPipes = 0;
                        for (int i = 0; i < readDataBuffer.Pipes.Count; i++)
                        {
                            var pipe = readDataBuffer.Pipes[i];
                            if (pipe.Validity == Pipe.Status.Success)
                            {
                                readDataBuffer.Pipes[i].Create();
                                validPipes++;
                            }
                            else
                                Instance.Puts("Failed to read pipe {0}({1})", pipe.DisplayName ?? pipe.Id.ToString(), pipe.OwnerId);
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

                            Instance.Puts("Successfully loaded {0} of {1} managers", validContainers, readDataBuffer.Containers.Count);
                        }

                        Instance.Puts("Load v1.0 complete");
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