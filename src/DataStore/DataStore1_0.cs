using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Oxide.Core;
using UnityEngine;

namespace Oxide.Plugins
{
    partial class SyncPipesDevelopment
    {
        class DataStore1_0 : MonoBehaviour
        {
            private static Coroutine _coroutine;
            private static bool _saving;
            private static bool _loading;
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
                            while (enumerator.MoveNext()) { }
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
                    SyncPipesDevelopment.Logger.Runtime.LogException(e, "DataStore1_0.Save");
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
                    SyncPipesDevelopment.Logger.Runtime.LogException(e, "DataStore1_0.Load");
                    return false;
                }
            }


            class Converter : JsonConverter
            {
                public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
                {
                    try
                    {
                        var buffer = value as Buffer;
                        if (buffer == null) return;

                        writer.WriteStartObject();
                        writer.WritePropertyName("pipes");
                        writer.WriteStartArray();
                        for (int i = 0; i < buffer.Pipes.Count; i++)
                            writer.WriteRawValue(buffer.Pipes[i]);
                        writer.WriteEndArray();
                        writer.WritePropertyName("containers");
                        writer.WriteStartArray();
                        for (int i = 0; i < buffer.Containers.Count; i++)
                            writer.WriteRawValue(buffer.Containers[i]);
                        writer.WriteEndArray();
                        writer.WriteEndObject();
                    }
                    catch (Exception e)
                    {
                        SyncPipesDevelopment.Logger.Runtime.LogException(e, "DataStore1_0.Converter.WriteJson");
                    }
                }

                public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
                    JsonSerializer serializer)
                {
                    var buffer = new Loader();
                    try
                    {
                        while (reader.Read())
                        {
                            if (reader.TokenType == JsonToken.PropertyName)
                            {
                                switch ((string)reader.Value)
                                {
                                    case "pipes":
                                        reader.Read();
                                        reader.Read();
                                        while (reader.TokenType != JsonToken.EndArray)
                                        {
                                            var pipe = serializer.Deserialize<SyncPipesDevelopment.Pipe>(reader);
                                            if (pipe.Validity == SyncPipesDevelopment.Pipe.Status.Success)
                                                buffer.Pipes.Add(pipe);
                                            else
                                                Instance.Puts("Failed to read pipe {0}({1})",
                                                    pipe.DisplayName ?? pipe.Id.ToString(), pipe.OwnerId);
                                        }

                                        break;
                                    case "containers":
                                        reader.Read();
                                        while (reader.Read() && reader.TokenType != JsonToken.EndArray)
                                        {
                                            var data = new SyncPipesDevelopment.ContainerManager.Data();
                                            while (reader.Read() && reader.TokenType != JsonToken.EndObject)
                                            {
                                                if (reader.TokenType == JsonToken.PropertyName)
                                                {
                                                    switch (reader.Value.ToString())
                                                    {
                                                        case "ci":
                                                            reader.Read();
                                                            uint.TryParse(reader.Value.ToString(),
                                                                out data.ContainerId);
                                                            break;
                                                        case "cs":
                                                            data.CombineStacks = reader.ReadAsBoolean() ?? true;
                                                            break;
                                                        case "dn":
                                                            data.DisplayName = reader.ReadAsString();
                                                            break;
                                                        case "ct":
                                                            data.ContainerType =
                                                                (SyncPipesDevelopment.ContainerType)reader.ReadAsInt32()
                                                                    .GetValueOrDefault();
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
                    }
                    catch (Exception e)
                    {
                        SyncPipesDevelopment.Logger.Runtime.LogException(e, "DataStore1_0.Converter.ReadJson");
                    }
                    return buffer;
                }

                public override bool CanConvert(Type objectType)
                {
                    return true;
                }
            }

            public class PipeConverter : JsonConverter
            {
                public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
                {
                    var pipe = value as Pipe;
                    if (pipe == null) return;
                    writer.WriteStartObject();
                    writer.WritePropertyName("enb");
                    writer.WriteValue(pipe.IsEnabled);
                    writer.WritePropertyName("grd");
                    writer.WriteValue(pipe.Grade);
                    writer.WritePropertyName("sid");
                    writer.WriteValue(ContainerHelper.IsComplexStorage(pipe.Source.ContainerType)
                        ? pipe.Source.Container.parentEntity.uid
                        : pipe.Source.Id);
                    writer.WritePropertyName("did");
                    writer.WriteValue(ContainerHelper.IsComplexStorage(pipe.Destination.ContainerType)
                        ? pipe.Destination.Container.parentEntity.uid
                        : pipe.Destination.Id);
                    writer.WritePropertyName("sct");
                    writer.WriteValue(pipe.Source.ContainerType);
                    writer.WritePropertyName("dct");
                    writer.WriteValue(pipe.Destination.ContainerType);
                    writer.WritePropertyName("hth");
                    writer.WriteValue(pipe.Health);
                    writer.WritePropertyName("mst");
                    writer.WriteValue(pipe.IsMultiStack);
                    writer.WritePropertyName("ast");
                    writer.WriteValue(pipe.IsAutoStart);
                    writer.WritePropertyName("fso");
                    writer.WriteValue(pipe.IsFurnaceSplitterEnabled);
                    writer.WritePropertyName("fss");
                    writer.WriteValue(pipe.FurnaceSplitterStacks);
                    writer.WritePropertyName("prt");
                    writer.WriteValue(pipe.Priority);
                    writer.WritePropertyName("oid");
                    writer.WriteValue(pipe.OwnerId);
                    writer.WritePropertyName("onm");
                    writer.WriteValue(pipe.OwnerName);
                    writer.WritePropertyName("nme");
                    writer.WriteValue(pipe.DisplayName);
                    writer.WritePropertyName("flr");
                    writer.WriteStartArray();
                    for (var i = 0; i < pipe.PipeFilter.Items.Count; i++)
                        writer.WriteValue(pipe.PipeFilter.Items[i].info.itemid);
                    writer.WriteEndArray();
                    writer.WriteEndObject();
                }

                public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
                    JsonSerializer serializer)
                {
                    return new Pipe(reader, serializer);
                }

                public override bool CanConvert(Type objectType)
                {
                    return objectType == typeof(Pipe);
                }
            }

            public class ContainerManagerConverter : JsonConverter
            {
                public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
                {
                    var container = value as ContainerManager;
                    if (container == null) return;
                    writer.WriteStartObject();
                    writer.WritePropertyName("ci");
                    if (container.Container is ResourceExtractorFuelStorage)
                        writer.WriteValue(container.Container.parentEntity.uid);
                    else
                        writer.WriteValue(container.ContainerId);
                    writer.WritePropertyName("cs");
                    writer.WriteValue(container.CombineStacks);
                    writer.WritePropertyName("dn");
                    writer.WriteValue(container.DisplayName);
                    writer.WritePropertyName("ct");
                    writer.WriteValue(ContainerHelper.GetEntityType(container.Container));
                    writer.WriteEndObject();
                }

                public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
                {
                    return null;
                }

                public override bool CanConvert(Type objectType)
                {
                    return objectType == typeof(ContainerManager);
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

            IEnumerator BufferedSave(string filename)
            {
                var sw = Stopwatch.StartNew();
                yield return null;
                Instance.Puts("Save v1.0 starting");
                var buffer = new Buffer();
                var pipeSnapshot = new List<Pipe>(SyncPipesDevelopment.Pipe.Pipes);
                var containerSnapshot = new List<ContainerManager>(SyncPipesDevelopment.ContainerManager.ManagedContainers);
                for (int i = 0; i < pipeSnapshot.Count; i++)
                {
                    try
                    {
                        buffer.Pipes.Add(JsonConvert.SerializeObject(pipeSnapshot[i], Formatting.None, new PipeConverter()));
                    }
                    catch (Exception e)
                    {
                        SyncPipesDevelopment.Logger.Runtime.LogException(e, "DataStore1_0.BufferedSave");
                    }

                    yield return null;
                }
                Instance.Puts("Saved {0} pipes", buffer.Pipes.Count);
                for (int i = 0; i < containerSnapshot.Count; i++)
                {
                    try
                    {
                        if (!containerSnapshot[i].HasAnyPipes) continue;
                        buffer.Containers.Add(JsonConvert.SerializeObject(containerSnapshot[i], Formatting.None, new ContainerManagerConverter()));
                    }
                    catch (Exception e)
                    {
                        SyncPipesDevelopment.Logger.Runtime.LogException(e, "DataStore1_0.BufferedSave");
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
                    var loader = Interface.Oxide.DataFileSystem.ReadObject<Loader>(filename);
                    for (int i = 0; i < loader.Pipes.Count; i++)
                    {
                        loader.Pipes[i].Create();
                        yield return null;
                    }

                    Instance.Puts("Successfully loaded {0} pipes", loader.Pipes.Count);
                    ContainerManager.Load(loader.Containers);
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
