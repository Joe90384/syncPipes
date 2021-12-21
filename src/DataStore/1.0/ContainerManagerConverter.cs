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
                public class ContainerManagerDataConverter : JsonConverter
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

                    public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
                        JsonSerializer serializer)
                    {
                        var containerManagerData = new ContainerManagerData();

                        var depth = 1;
                        if (reader.TokenType != JsonToken.StartObject)
                        {
                            LogLoadError(containerManagerData,
                                "Json StartObject for container manager is missing...");
                            return containerManagerData;
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
                                        case "ci":
                                            reader.Read();
                                            uint containerId;
                                            if (uint.TryParse(reader.Value.ToString(), out containerId))
                                                containerManagerData.ContainerId = containerId;
                                            break;
                                        case "cs":
                                            containerManagerData.CombineStacks =
                                                reader.ReadAsBoolean() ?? false;
                                            break;
                                        case "dn":
                                            containerManagerData.DisplayName = reader.ReadAsString();
                                            break;
                                        case "ct":
                                            containerManagerData.ContainerType =
                                                (ContainerType)(reader.ReadAsInt32() ?? 0);
                                            break;
                                    }

                                    break;
                            }
                        }

                        return containerManagerData;
                    }

                    public override bool CanConvert(Type objectType)
                    {
                        _canRead = objectType == typeof(ContainerManagerData);
                        _canWrite = objectType == typeof(ContainerManager);
                        return _canRead || _canWrite;
                    }

                    private bool _canWrite;
                    private bool _canRead;

                    public override bool CanWrite => _canWrite;
                    public override bool CanRead => _canRead;
                }

            }
        }
    }
}
