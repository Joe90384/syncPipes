using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Oxide.Plugins
{
    partial class SyncPipesDevelopment
    {
        partial class DataStore
        {
            partial class OnePointOne
            {
                public class DataConverter : JsonConverter
                {
                    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
                    {
                        try
                        {
                            var buffer = value as WriteDataBuffer;
                            if (buffer == null) return;
                            writer.WriteStartObject();
                            writer.WritePropertyName("positions");
                            writer.WriteStartArray();
                            for (int i = 0; i < buffer.QuarryPumpJackPositions.Count; i++)
                                writer.WriteRawValue(buffer.QuarryPumpJackPositions[i]);
                            writer.WriteEndArray();
                            writer.WritePropertyName("pipes");
                            writer.WriteStartArray();
                            for (int i = 0; i < buffer.Pipes.Count; i++)
                                writer.WriteRawValue(buffer.Pipes[i]);
                            writer.WriteEndArray();
                            writer.WritePropertyName("factories");
                            writer.WriteStartArray();
                            for (int i = 0; i < buffer.Factories.Count; i++)
                                writer.WriteRawValue(buffer.Factories[i]);
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
                            Logger.Runtime.LogException(e, "DataStore1_1.DataConverter.WriteJson");
                        }
                    }

                    public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
                        JsonSerializer serializer)
                    {
                        var buffer = new ReadDataBuffer();
                        serializer.Converters.Add(new PipeConverter(buffer.EntityFinder));
                        serializer.Converters.Add(new ContainerManagerDataConverter(){IsRead = true});
                        serializer.Converters.Add(new PipeFactoryDataConverter(){IsRead = true});
                        serializer.Converters.Add(new EntityPositionConverter() { IsRead = true });
                        try
                        {
                            while (reader.Read())
                            {
                                if (reader.TokenType == JsonToken.PropertyName)
                                {
                                    Instance.Puts((string)reader.Value);
                                    switch ((string)reader.Value)
                                    {
                                        case "positions":
                                            reader.Read();
                                            reader.Read();
                                            while (reader.TokenType != JsonToken.EndArray)
                                            {
                                                var positionData = serializer.Deserialize<EntityPositionData>(reader);
                                                buffer.EntityFinder.Positions.Add(positionData.Id, positionData);
                                            }
                                            break;
                                        case "pipes":
                                            reader.Read();
                                            reader.Read();
                                            while (reader.TokenType != JsonToken.EndArray)
                                                buffer.Pipes.Add(serializer.Deserialize<Pipe>(reader));
                                            break;
                                        case "factories":
                                            reader.Read();
                                            reader.Read();
                                            while (reader.TokenType != JsonToken.EndArray)
                                                buffer.Factories.Add(serializer.Deserialize<PipeFactoryData>(reader));
                                            break;
                                        case "containers":
                                            reader.Read();
                                            reader.Read();
                                            while (reader.TokenType != JsonToken.EndArray)
                                                buffer.Containers.Add(serializer.Deserialize<ContainerManagerData>(reader));
                                            break;
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.Runtime.LogException(e, "DataStore1_0.DataConverter.ReadJson");
                        }

                        return buffer;
                    }

                    public override bool CanConvert(Type objectType)
                    {
                        return true;
                    }
                }
                
            }
        }
    }
}
