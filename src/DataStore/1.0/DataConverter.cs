using System;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    partial class SyncPipesDevelopment
    {
        partial class Data
        {
            partial class OnePointZero
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
                            Logger.Runtime.LogException(e, "DataStore1_0.DataConverter.WriteJson");
                        }
                    }

                    public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
                        JsonSerializer serializer)
                    {
                        serializer.Converters.Add(new PipeConverter());
                        serializer.Converters.Add(new ContainerManagerDataConverter());
                        var buffer = new ReadDataBuffer();
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
                                                buffer.Pipes.Add(serializer.Deserialize<Pipe>(reader));
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
