using System;
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
                public class EntityPositionConverter : JsonConverter
                {
                    public bool IsRead { get; set; }

                    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
                    {
                        var container = value as ContainerManager;
                        if (container == null) return;
                        writer.WriteStartObject();
                        if (container.Container is ResourceExtractorFuelStorage)
                        {
                            var parent = container.Container.GetParentEntity();
                            writer.WritePropertyName("uid");
                            writer.WriteValue(parent.net.ID);
                            writer.WritePropertyName("x");
                            writer.WriteValue(parent.transform.position.x);
                            writer.WritePropertyName("y");
                            writer.WriteValue(parent.transform.position.y);
                            writer.WritePropertyName("z");
                            writer.WriteValue(parent.transform.position.z);
                            writer.WritePropertyName("spn");
                            writer.WriteValue(parent.ShortPrefabName);
                        }
                        else
                        {
                            writer.WritePropertyName("uid");
                            writer.WriteValue(container.Container.net.ID);
                            writer.WritePropertyName("x");
                            writer.WriteValue(container.Container.transform.position.x);
                            writer.WritePropertyName("y");
                            writer.WriteValue(container.Container.transform.position.y);
                            writer.WritePropertyName("z");
                            writer.WriteValue(container.Container.transform.position.z);
                            writer.WritePropertyName("spn");
                            writer.WriteValue(container.Container.ShortPrefabName);
                        }
                        writer.WriteEndObject();
                    }

                    public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
                        JsonSerializer serializer)
                    {
                        var quarryPumpJackData = new EntityPositionData();

                        var depth = 1;
                        if (reader.TokenType != JsonToken.StartObject)
                        {
                            LogLoadError(quarryPumpJackData,
                                "Json StartObject for container manager is missing...");
                            return quarryPumpJackData;
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
                                        case "uid":
                                            reader.Read();
                                            uint id;
                                            if (uint.TryParse(reader.Value.ToString(), out id))
                                                quarryPumpJackData.Id = id;
                                            break;
                                        case "x":
                                            reader.Read();
                                            float x;
                                            if (float.TryParse(reader.Value.ToString(), out x))
                                                quarryPumpJackData.X = x;
                                            break;
                                        case "y":
                                            reader.Read();
                                            float y;
                                            if (float.TryParse(reader.Value.ToString(), out y))
                                                quarryPumpJackData.Y = y;
                                            break;
                                        case "z":
                                            reader.Read();
                                            float z;
                                            if (float.TryParse(reader.Value.ToString(), out z))
                                                quarryPumpJackData.Z = z;
                                            break;
                                        case "spn":
                                            quarryPumpJackData.ShortPrefabName = reader.ReadAsString();
                                            break;
                                    }

                                    break;
                            }
                        }

                        return quarryPumpJackData;
                    }

                    public override bool CanConvert(Type objectType) => IsRead
                        ? objectType == typeof(EntityPositionData)
                        : objectType == typeof(ContainerManager);
                }


                public class EntityPositionData
                {
                    public uint Id { get; set; }
                    public float X { get; set; }
                    public float Y { get; set; }
                    public float Z { get; set; }
                    public string ShortPrefabName { get; set; }

                    public Vector3 Vector => new Vector3(X, Y, Z);
                }
            }
        }
    }
}
