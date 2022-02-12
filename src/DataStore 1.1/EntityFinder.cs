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
                public class EntityFinder
                {
                    public Dictionary<uint, EntityPositionData> Positions { get; } = new Dictionary<uint, EntityPositionData>();

                    public Dictionary<uint, uint> Adjustments { get; } = new Dictionary<uint, uint>();

                    public uint AdjustedIds(uint savedId)
                    {
                        if (Adjustments.ContainsKey(savedId))
                            return Adjustments[savedId];
                        return savedId;
                    }

                    public BaseEntity Find(uint savedId, ContainerType containerType = ContainerType.General)
                    {
                        savedId = AdjustedIds(savedId);
                        var entity = (BaseEntity)BaseNetworkable.serverEntities.Find(savedId);
                        if (entity != null) return ContainerHelper.Find(entity, containerType);
                        if (!Positions.ContainsKey(savedId)) return null;
                        var positionData = Positions[savedId];
                        var quarries = new List<MiningQuarry>();
                        Vis.Entities(positionData.Vector, 0f, quarries);
                        Instance.PrintWarning($"Failed to find {positionData.ShortPrefabName}({positionData.Id}) at {positionData.Vector}");
                        for (int i = 0; i < quarries.Count; i++)
                        {
                            var quarry = quarries[i];
                            if (quarry == null) continue;
                            if (quarry.ShortPrefabName == positionData.ShortPrefabName)
                            {
                                Instance.PrintWarning($"Found alternate {quarry.ShortPrefabName}({quarry.net.ID}) at {quarry.transform.position}");
                                Adjustments.Add(savedId, quarry.net.ID);
                                return ContainerHelper.Find(quarry, containerType);
                            }
                        }
                        return null;
                    }
                }
            }
        }
    }
}