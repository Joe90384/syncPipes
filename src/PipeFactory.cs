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
            private static readonly Vector3 OverlappingPipeOffset = OverlappingPipeOffset = new Vector3(0.0001f, 0.0001f, 0);
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

            public virtual void Create()
            {
                Segments.Add(PreparePipeSegmentEntity(0, CreatePrimarySegment()));
                for (var i = 1; i < _segmentCount; i++)
                {
                    Segments.Add(PreparePipeSegmentEntity(i, CreateSecondarySegment(i)));
                }
            }
            public virtual void Reverse() { }

            public virtual void Upgrade(BuildingGrade.Enum grade) { }

            public abstract void SetHelath(float health);

            protected virtual Vector3 GetSourcePosition()
            {
                return (_segmentCount == 1
                        ? (_pipe.Source.Position + _pipe.Destination.Position) / 2
                        : _pipe.Source.Position + _pipe.Rotation * Vector3.forward * (PipeLength / 2))
                    + _rotationOffset + Vector3.down * 0.8f;
            }

            protected virtual Vector3 GetOffsetPosition(int segmentIndex) =>
                Vector3.forward * (PipeLength * segmentIndex - _segmentOffset) + (segmentIndex % 2 == 0
                    ? Vector3.zero
                    : OverlappingPipeOffset);

            protected virtual BaseEntity CreatePrimarySegment() => CreateSegment(GetSourcePosition(), _pipe.Rotation);

            protected virtual BaseEntity CreateSecondarySegment(int segmentIndex) => CreateSegment(GetOffsetPosition(segmentIndex));


            /// <summary>
            /// Initialize the properties of a pipe segment entity.
            /// Adds lights if enabled in the config
            /// </summary>
            /// <param name="pipeSegment">The pipe segment entity to prepare</param>
            /// <param name="pipeIndex">The index of this pipe segment</param>
            protected virtual BaseEntity PreparePipeSegmentEntity(int pipeIndex, BaseEntity pipeSegment)
            {
                pipeSegment.enableSaving = false;

                PipeSegment.Attach(pipeSegment, _pipe);

                if (PrimarySegment != pipeSegment)
                    pipeSegment.SetParent(PrimarySegment);

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
                pipeSegment.enableSaving = false;
                return pipeSegment;
            }

        }

        class PipeFactoryLowWall : PipeFactory
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
            protected override BaseEntity PreparePipeSegmentEntity(int pipeIndex, BaseEntity pipeSegment)
            {
                var block = pipeSegment.GetComponent<BuildingBlock>();
                if (block != null)
                {
                    block.grounded = true;
                    block.grade = _pipe.Grade;
                    block.enableSaving = false;
                    block.Spawn();
                    block.SetHealthToMax();
                }
                return base.PreparePipeSegmentEntity(pipeIndex, pipeSegment);
            }

            public PipeFactoryLowWall(Pipe pipe) : base(pipe)
            {
            }

            public override void Upgrade(BuildingGrade.Enum grade)
            {
                foreach (var buildingBlock in Segments.Select(segment => segment.GetComponent<BuildingBlock>()))
                {
                    buildingBlock.SetGrade(grade);
                    buildingBlock.SetHealthToMax();
                    buildingBlock.SendNetworkUpdate(BasePlayer.NetworkQueue.UpdateDistance);
                }
            }

            public override void SetHelath(float health)
            {
                foreach (var buildingBlock in Segments.Select(segment => segment.GetComponent<BuildingBlock>()))
                {
                    buildingBlock.health = health;
                    buildingBlock.SendNetworkUpdate(BasePlayer.NetworkQueue.UpdateDistance);
                }
            }
        }
    }
}
