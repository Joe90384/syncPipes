using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    partial class SyncPipesDevelopment
    {
        partial class DataStore
        {
            partial class OnePointOne
            {
                public class PipeConverter : JsonConverter
                {
                    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
                    {
                        var pipe = value as Pipe;
                        if (pipe == null) return;
                        writer.WriteStartObject();
                        writer.WritePropertyName("pid");
                        writer.WriteValue(pipe.Id);
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
                        var pipe = new Pipe();


                        pipe.Id = pipe.GenerateId();
                        var depth = 1;
                        if (reader.TokenType != JsonToken.StartObject)
                        {
                            LogLoadError(pipe, 0, 0, "Json StartObject for pipe is missing...");
                            return pipe;
                        }

                        uint sourceId = 0, destinationId = 0;
                        ContainerType sourceType = ContainerType.General,
                            destinationType = ContainerType.General;
                        while (reader.Read() && depth > 0)
                        {
                            switch (reader.TokenType)
                            {
                                case JsonToken.StartObject:
                                    depth++;
                                    break;
                                case JsonToken.EndObject:
                                    depth--;
                                    break;
                                case JsonToken.PropertyName:
                                    switch (reader.Value.ToString())
                                    {
                                        case "pid":
                                            reader.Read();
                                            uint id;
                                            if (uint.TryParse(reader.Value.ToString(), out id))
                                                pipe.Id = id;
                                            break;
                                        case "enb":
                                            pipe.IsEnabled = reader.ReadAsBoolean() ?? false;
                                            break;
                                        case "grd":
                                            pipe.Grade =
                                                (BuildingGrade.Enum)reader.ReadAsInt32().GetValueOrDefault(0);
                                            break;
                                        case "sid":
                                            reader.Read();
                                            uint.TryParse(reader.Value.ToString(), out sourceId);
                                            break;
                                        case "did":
                                            reader.Read();
                                            uint.TryParse(reader.Value.ToString(), out destinationId);
                                            break;
                                        case "sct":
                                            sourceType =
                                                (ContainerType)reader.ReadAsInt32().GetValueOrDefault(0);
                                            break;
                                        case "dct":
                                            destinationType =
                                                (ContainerType)reader.ReadAsInt32().GetValueOrDefault(0);
                                            break;
                                        case "hth":
                                            pipe.InitialHealth =
                                                (float)reader.ReadAsDecimal().GetValueOrDefault(0);
                                            break;
                                        case "mst":
                                            pipe.IsMultiStack = reader.ReadAsBoolean() ?? false;
                                            break;
                                        case "ast":
                                            pipe.IsAutoStart = reader.ReadAsBoolean() ?? false;
                                            break;
                                        case "fso":
                                            pipe.IsFurnaceSplitterEnabled = reader.ReadAsBoolean() ?? false;
                                            break;
                                        case "fss":
                                            pipe.FurnaceSplitterStacks = reader.ReadAsInt32() ?? 1;
                                            break;
                                        case "prt":
                                            pipe.Priority =
                                                (Pipe.PipePriority)reader.ReadAsInt32()
                                                    .GetValueOrDefault(0);
                                            break;
                                        case "oid":
                                            reader.Read();
                                            ulong ownerId;
                                            if (ulong.TryParse(reader.Value.ToString(), out ownerId))
                                                pipe.OwnerId = ownerId;
                                            break;
                                        case "onm":
                                            pipe.OwnerName = reader.ReadAsString();
                                            break;
                                        case "nme":
                                            pipe.DisplayName = reader.ReadAsString();
                                            break;
                                        case "flr":
                                            var filterIds = new List<int>();
                                            while (reader.Read() && reader.TokenType != JsonToken.EndArray)
                                            {
                                                int value;
                                                if (reader.Value != null &&
                                                    int.TryParse(reader.Value?.ToString(), out value))
                                                    filterIds.Add(value);
                                            }

                                            pipe.InitialFilterItems = filterIds;
                                            break;
                                    }

                                    break;
                            }
                        }

                        var source = ContainerHelper.Find(sourceId, sourceType);
                        var destination = ContainerHelper.Find(destinationId, destinationType);
                        if(source != null)
                            pipe.Source = new PipeEndContainer(source, sourceType, pipe);
                        if(destination != null)
                            pipe.Destination = new PipeEndContainer(destination, destinationType, pipe);
                        pipe.Validate();
                        if (pipe.Validity != Pipe.Status.Success)
                            LogLoadError(pipe, sourceId, destinationId);
                        return pipe;
                    }

                    public override bool CanConvert(Type objectType)
                    {
                        return objectType == typeof(Pipe);
                    }
                }
            }
        }
    }
}
