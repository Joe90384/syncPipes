using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    partial class SyncPipesDevelopment
    {
        partial class DataStore
        {
            partial class OnePointOne
            {
                public class PipeFactoryDataConverter : JsonConverter
                {
                    public bool IsRead { get; set; }
                    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
                    {
                        var pipe = value as Pipe;
                        if (pipe == null) return;
                        writer.WriteStartObject();
                        writer.WritePropertyName("pid");
                        writer.WriteValue(pipe.Id);
                        writer.WritePropertyName("brl");
                        writer.WriteValue(pipe.Factory is PipeFactoryBarrel);
                        writer.WritePropertyName("sgs");
                        writer.WriteStartArray();
                        for(int i = 0; i < pipe.Factory.Segments.Count; i++)
                            writer.WriteValue(pipe.Factory.Segments[i].net.ID);
                        writer.WriteEndArray();
                        writer.WriteEndObject();
                    }

                    public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
                        JsonSerializer serializer)
                    {
                        var pipeFactoryData = new PipeFactoryData();

                        var depth = 1;
                        if (reader.TokenType != JsonToken.StartObject)
                        {
                            LogLoadError(pipeFactoryData,
                                "Json StartObject for pipe factory data is missing...");
                            return pipeFactoryData;
                        }

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
                                            uint pipeId;
                                            if (uint.TryParse(reader.Value.ToString(), out pipeId))
                                                pipeFactoryData.PipeId = pipeId;
                                            break;
                                        case "brl":
                                            pipeFactoryData.IsBarrel =
                                                reader.ReadAsBoolean() ?? false;
                                            break;
                                        case "sgs":
                                            var entityIds = new List<uint>();
                                            while (reader.Read() && reader.TokenType != JsonToken.EndArray)
                                            {
                                                uint value;
                                                if (reader.Value != null &&
                                                    uint.TryParse(reader.Value?.ToString(), out value))
                                                    entityIds.Add(value);
                                            }
                                            pipeFactoryData.EntityIds = entityIds.ToArray();
                                            break;
                                    }
                                    break;
                            }
                        }
                        return pipeFactoryData;
                    }

                    public override bool CanConvert(Type objectType) => IsRead ? objectType == typeof(PipeFactoryData) : objectType == typeof(Pipe);
                }

            }
        }
    }
}
