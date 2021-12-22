using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    partial class SyncPipesDevelopment
    {
        private class PipeFactoryLowWall : PipeFactoryBase<BuildingBlock>
        {
            protected override float PipeLength => 3f;

            private const string _prefab = "assets/prefabs/building core/wall.low/wall.low.prefab";
            protected override string Prefab => _prefab;

            /// <summary>
            /// Initialize the properties of a pipe segment entity.
            /// Adds lights if enabled in the config
            /// </summary>
            /// <param name="pipeSegment">The pipe segment entity to prepare</param>
            /// <param name="pipeIndex">The index of this pipe segment</param>
            protected override BuildingBlock PreparePipeSegmentEntity(int pipeIndex, BaseEntity pipeSegment)
            {
                var pipeSegmentEntity = base.PreparePipeSegmentEntity(pipeIndex, pipeSegment);
                if (pipeSegmentEntity == null) return null;
                pipeSegmentEntity.grounded = true;
                pipeSegmentEntity.grade = _pipe.Grade;
                pipeSegmentEntity.enableSaving = InstanceConfig.Experimental.PermanentEntities;
                pipeSegmentEntity.SetHealthToMax();
                if (InstanceConfig.AttachXmasLights)
                {
                    var lights = GameManager.server.CreateEntity(
                        "assets/prefabs/misc/xmas/christmas_lights/xmas.lightstring.deployed.prefab",
                        Vector3.up * 1.025f +
                        Vector3.forward * 0.13f +
                        (pipeIndex % 2 == 0
                            ? Vector3.zero
                            : OverlappingPipeOffset),
                        Quaternion.Euler(180, 90, 0));
                    lights.enableSaving = false;
                    lights.Spawn();
                    lights.SetParent(pipeSegment);
                    PipeSegmentLights.Attach(lights, _pipe);
                }
                return pipeSegmentEntity;
            }

            public PipeFactoryLowWall(Pipe pipe) : base(pipe) { }

            private BuildingBlock[] _segmentBuildingBlocks;

            private IEnumerable<BuildingBlock> SegmentBuildingBlocks
            {
                get
                {
                    if (_segmentBuildingBlocks == null)
                    {
                        _segmentBuildingBlocks = new BuildingBlock[Segments.Count];
                        for (int i = 0; i < Segments.Count; i++)
                            _segmentBuildingBlocks[i] = Segments[i].GetComponent<BuildingBlock>();
                    }

                    return _segmentBuildingBlocks;
                }
            }

            public override void Upgrade(BuildingGrade.Enum grade)
            {
                foreach (var buildingBlock in SegmentBuildingBlocks)
                {
                    buildingBlock.SetGrade(grade);
                    buildingBlock.SetHealthToMax();
                    buildingBlock.SendNetworkUpdate(BasePlayer.NetworkQueue.UpdateDistance);
                }
            }

            public override void SetHealth(float health)
            {
                foreach (var buildingBlock in SegmentBuildingBlocks)
                {
                    buildingBlock.health = health;
                    buildingBlock.SendNetworkUpdate(BasePlayer.NetworkQueue.UpdateDistance);
                }
            }

            protected override Vector3 SourcePosition =>
                (_segmentCount == 1
                           ? (_pipe.Source.Position + _pipe.Destination.Position) / 2
                           : _pipe.Source.Position + _pipe.Rotation * Vector3.forward * (PipeLength / 2))
                       + _rotationOffset + Vector3.down * 0.8f;

            protected override Quaternion Rotation => _pipe.Rotation;

            protected override Vector3 GetOffsetPosition(int segmentIndex) =>
                Vector3.forward * (PipeLength * segmentIndex - _segmentOffset) + (segmentIndex % 2 == 0
                    ? Vector3.zero
                    : OverlappingPipeOffset);
        }
    }
}
