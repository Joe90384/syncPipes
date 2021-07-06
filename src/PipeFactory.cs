using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    partial class SyncPipesDevelopment
    {
        private abstract class PipeFactory
        {
            protected Pipe _pipe;
            protected int _segmentCount;
            public BaseEntity PrimarySegment => Segments.FirstOrDefault();
            protected float _segmentOffset;
            protected Vector3 _rotationOffset;
            public List<BaseEntity> Segments = new List<BaseEntity>();

            protected abstract float PipeLength { get; }

            protected abstract string Prefab { get; }
            protected static readonly Vector3 OverlappingPipeOffset = OverlappingPipeOffset = new Vector3(0.0001f, 0.0001f, 0);
            //protected 
            protected PipeFactory(Pipe pipe)
            {
                _pipe = pipe;
                Init();
            }

            private void Init()
            {
                _segmentCount = (int)Mathf.Ceil(_pipe.Distance / PipeLength);
                _segmentOffset = _segmentCount * PipeLength - _pipe.Distance;
                _rotationOffset = (_pipe.Source.Position.y - _pipe.Destination.Position.y) * Vector3.down * 0.0002f;
            }

            protected virtual BaseEntity CreateSegment(Vector3 position, Quaternion rotation = default(Quaternion))
            {
                return GameManager.server.CreateEntity(Prefab, position, rotation);
            }

            public abstract void Create();
            public virtual void Reverse() { }

            public virtual void Upgrade(BuildingGrade.Enum grade) { }

            public abstract void SetHealth(float health);

            protected abstract Vector3 SourcePosition { get; }

            protected abstract Quaternion Rotation { get; }

            protected abstract Vector3 GetOffsetPosition(int segmentIndex);

            protected virtual BaseEntity CreatePrimarySegment() => CreateSegment(SourcePosition, Rotation);

            protected virtual BaseEntity CreateSecondarySegment(int segmentIndex) => CreateSegment(GetOffsetPosition(segmentIndex));
        }

        private abstract class PipeFactory<TEntity> : PipeFactory
        where TEntity: BaseEntity
        {
            /// <summary>
            /// Initialize the properties of a pipe segment entity.
            /// Adds lights if enabled in the config
            /// </summary>
            /// <param name="pipeSegment">The pipe segment entity to prepare</param>
            /// <param name="pipeIndex">The index of this pipe segment</param>
            protected virtual TEntity PreparePipeSegmentEntity(int pipeIndex, BaseEntity pipeSegment)
            {
                var pipeSegmentEntity = pipeSegment as TEntity;
                if (pipeSegmentEntity == null) return null;
                pipeSegmentEntity.enableSaving = false;
                pipeSegmentEntity.Spawn();

                PipeSegment.Attach(pipeSegmentEntity, _pipe);

                if (PrimarySegment != pipeSegmentEntity)
                    pipeSegmentEntity.SetParent(PrimarySegment);
                return pipeSegmentEntity;
            }

            public override void Create()
            {
                Segments.Add(PreparePipeSegmentEntity(0, CreatePrimarySegment()));
                for (var i = 1; i < _segmentCount; i++)
                {
                    Segments.Add(PreparePipeSegmentEntity(i, CreateSecondarySegment(i)));
                }
            }

            protected PipeFactory(Pipe pipe) : base(pipe) { }
        }

        private class PipeFactoryLowWall : PipeFactory<BuildingBlock>
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
                pipeSegmentEntity.enableSaving = false;
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

            public override void Upgrade(BuildingGrade.Enum grade)
            {
                foreach (var buildingBlock in Segments.Select(segment => segment.GetComponent<BuildingBlock>()))
                {
                    buildingBlock.SetGrade(grade);
                    buildingBlock.SetHealthToMax();
                    buildingBlock.SendNetworkUpdate(BasePlayer.NetworkQueue.UpdateDistance);
                }
            }

            public override void SetHealth(float health)
            {
                foreach (var buildingBlock in Segments.Select(segment => segment.GetComponent<BuildingBlock>()))
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

        private class PipeFactoryBarrel : PipeFactory<StorageContainer>
        {
            protected override string Prefab => "assets/bundled/prefabs/radtown/loot_barrel_1.prefab";

            public PipeFactoryBarrel(Pipe pipe) : base(pipe) { }

            protected override float PipeLength => 1.14f;

            public override void SetHealth(float health)
            {
                foreach (var segment in Segments.OfType<LootContainer>())
                {
                    segment.health = health;
                    segment.SendNetworkUpdate(BasePlayer.NetworkQueue.UpdateDistance);
                }
            }

            protected override Vector3 SourcePosition =>
                (_segmentCount == 1
                           ? (_pipe.Source.Position + _pipe.Destination.Position) / 2
                           : _pipe.Source.Position + _pipe.Rotation * Vector3.back * (PipeLength / 2 - 0.5f)) +
                       _rotationOffset + Vector3.down * 0.05f;

            protected override Quaternion Rotation =>
                _pipe.Rotation * Quaternion.AngleAxis(90f, Vector3.forward) * Quaternion.AngleAxis(-90f, Vector3.left);

            protected override Vector3 GetOffsetPosition(int segmentIndex) =>
                Vector3.up * (PipeLength * segmentIndex - _segmentOffset) + (segmentIndex % 2 == 0
                    ? Vector3.zero
                    : OverlappingPipeOffset);

            public override void Reverse()
            {
                PrimarySegment.transform.SetPositionAndRotation(SourcePosition, Rotation);
                PrimarySegment.SendNetworkUpdate(BasePlayer.NetworkQueue.UpdateDistance);
            }

            protected override BaseEntity CreateSecondarySegment(int segmentIndex)
            {
                return CreateSegment(GetOffsetPosition(segmentIndex), Quaternion.Euler(0f, segmentIndex *80f, 0f));
            }
        }
    }
}
