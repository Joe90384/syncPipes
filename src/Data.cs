using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;
using Oxide.Core;
using UnityEngine;

namespace Oxide.Plugins
{
    public partial class SyncPipesDevelopment
    {
        /// <summary>
        /// The data handler for loading and saving data to disk
        /// </summary>
        //[JsonConverter(typeof(DataConverter))]
        class Data
        {
            /// <summary>
            /// The data for all the pipes
            /// </summary>
            public PipeData[] PipeData { get; set; }

            /// <summary>
            /// The data for all the container managers
            /// </summary>
            public List<ContainerManager.Data> ContainerData { get; set; }
            /// <summary>
            /// Load syncPipes data from disk
            /// </summary>
            public static void Load()
            {
                var data = Interface.Oxide.DataFileSystem.ReadObject<Data>(Instance.Name);
                if (data != null)
                {
                    Pipe.Load(data.PipeData);
                    ContainerManager.Load(data.ContainerData);
                }
            }
        }

        class DataStore1_0: MonoBehaviour
        {
            private static GameObject _saverGameObject;
            private static DataStore1_0 _dataStore;

            private static DataStore1_0 DataStore
            {
                get
                {
                    if (_dataStore == null)
                    {
                        _saverGameObject =
                            new GameObject($"{Instance.Name.ToLower()}-datastore-1-0");
                        _dataStore = _saverGameObject.AddComponent<DataStore1_0>();
                    }
                    return _dataStore;
                }
            }
            private static string _filename;
            private static string Filename => _filename ?? (_filename = $"{Instance.Name} v1-0");

            public static bool Save(bool backgroundSave = true)
            {
                if (backgroundSave && _running)
                {
                    if(DataStore._coroutine != null)
                        DataStore.StopCoroutine(DataStore._coroutine);
                    _running = false;
                }
                if (_running)
                    return false;
                _running = true;
                if (backgroundSave)
                    DataStore._coroutine = DataStore.StartCoroutine(DataStore.BufferedSave());
                else
                {
                    var enumerator = DataStore.BufferedSave();
                    while (enumerator.MoveNext()) { }
                }
                return true;
            }

            public static bool Load()
            {
                if (!Interface.Oxide.DataFileSystem.ExistsDatafile(Filename) || _running) 
                    return false;
                _running = true;
                DataStore._coroutine = DataStore.StartCoroutine(DataStore.BufferedLoad());
                return true;
            }

            private UnityEngine.Coroutine _coroutine;
            private static bool _running;

            class Converter : JsonConverter
            {
                public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
                {
                    var buffer = value as Buffer;
                    if (buffer == null) return;

                    writer.WriteStartObject();
                    writer.WritePropertyName("pipes");
                    writer.WriteStartArray();
                    for(int i = 0; i < buffer.Pipes.Count; i++)
                        writer.WriteRawValue(buffer.Pipes[i]);
                    writer.WriteEndArray();
                    writer.WritePropertyName("containers");
                    writer.WriteStartArray();
                    for(int i = 0; i < buffer.Containers.Count; i++)
                        writer.WriteRawValue(buffer.Containers[i]);
                    writer.WriteEndArray();
                    writer.WriteEndObject();
                }

                public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
                    JsonSerializer serializer)
                {
                    var buffer = new Loader();
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonToken.PropertyName)
                        {
                            switch ((string) reader.Value)
                            {
                                case "pipes":
                                    reader.Read();
                                    reader.Read();
                                    while (reader.TokenType != JsonToken.EndArray)
                                    {
                                        var pipe = serializer.Deserialize<Pipe>(reader);
                                        if (pipe.Validity == Pipe.Status.Success)
                                            buffer.Pipes.Add(pipe);
                                        else
                                            Instance.Puts("Failed to read pipe {0}({1})", pipe.DisplayName ?? pipe.Id.ToString(), pipe.OwnerId);
                                    }
                                    break;
                                case "containers":
                                    reader.Read();
                                    while (reader.Read() && reader.TokenType != JsonToken.EndArray)
                                    {
                                        var data = new ContainerManager.Data();
                                        while (reader.Read() && reader.TokenType != JsonToken.EndObject)
                                        {
                                            if (reader.TokenType == JsonToken.PropertyName)
                                            {
                                                switch (reader.Value.ToString())
                                                {
                                                    case "ci":
                                                        reader.Read();
                                                        uint.TryParse(reader.Value.ToString(), out data.ContainerId);
                                                        break;
                                                    case "cs":
                                                        data.CombineStacks = reader.ReadAsBoolean() ?? true;
                                                        break;
                                                    case "dn":
                                                        data.DisplayName = reader.ReadAsString();
                                                        break;
                                                    case "ct":
                                                        data.ContainerType = (ContainerType)reader.ReadAsInt32().GetValueOrDefault();
                                                        break;
                                                }
                                            }
                                        }
                                        buffer.Containers.Add(data);
                                    }
                                    break;
                            }
                        }
                    }

                    return buffer;
                }

                public override bool CanConvert(Type objectType)
                {
                    return true;
                }
            }

            [JsonConverter(typeof(Converter))]
            class Buffer
            {
                public List<string> Pipes { get; } = new List<string>();
                public List<string> Containers { get; } = new List<string>();
            }

            [JsonConverter(typeof(Converter))]
            class Loader
            {
                public List<Pipe> Pipes { get; } = new List<Pipe>();
                public List<ContainerManager.Data> Containers { get; } = new List<ContainerManager.Data>();
            }
            
            IEnumerator BufferedSave()
            {
                var sw = Stopwatch.StartNew();
                yield return null;
                Instance.Puts("Save v1.0 starting");
                var buffer = new Buffer();
                var pipeSnapshot = new List<Pipe>(Pipe.Pipes);
                var containerSnapshot = new List<ContainerManager>(ContainerManager.ManagedContainers);
                for (int i = 0; i < pipeSnapshot.Count; i++)
                {
                    buffer.Pipes.Add(JsonConvert.SerializeObject(pipeSnapshot[i], Formatting.None));
                    yield return null;
                }
                Instance.Puts("Saved {0} pipes", buffer.Pipes.Count);
                for(int i = 0; i < containerSnapshot.Count; i++)
                {
                    if (!containerSnapshot[i].HasAnyPipes) continue;
                    buffer.Containers.Add(JsonConvert.SerializeObject(containerSnapshot[i], Formatting.None));
                    yield return null;
                }
                Instance.Puts("Saved {0} managers", buffer.Containers.Count);
                Interface.Oxide.DataFileSystem.WriteObject(Filename, buffer);
                Interface.Oxide.DataFileSystem.GetDatafile($"{Instance.Name}").Clear();
                Instance.Puts("Save v1.0 complete ({0}.{1:00}s)", sw.Elapsed.Seconds, sw.Elapsed.Milliseconds);
                sw.Stop();
                _running = false;
                yield return null;
            }

            IEnumerator BufferedLoad()
            {
                yield return null;
                Instance.Puts("Load v1.0 starting");
                var loader = Interface.Oxide.DataFileSystem.ReadObject<Loader>(Filename);
                for (int i = 0; i < loader.Pipes.Count; i++)
                {
                    loader.Pipes[i].Create();
                    yield return null;
                }
                Instance.Puts("Successfully loaded {0} pipes", loader.Pipes.Count);
                ContainerManager.Load(loader.Containers);
                Instance.Puts("Load v1.0 complete");
                _running = false;
                yield return null;
            }
        }
    }
}
