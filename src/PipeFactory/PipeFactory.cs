using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    partial class SyncPipesDevelopment
    {
        internal abstract class PipeFactoryBase
        {
            protected Pipe _pipe;
            protected int _segmentCount;
            public BaseEntity PrimarySegment => Segments.FirstOrDefault();
            protected float _segmentOffset;
            protected Vector3 _rotationOffset;
            public List<BaseEntity> Segments = new List<BaseEntity>();
            public List<BaseEntity> Lights = new List<BaseEntity>();

            protected abstract float PipeLength { get; }

            protected abstract string Prefab { get; }
            protected static readonly Vector3 OverlappingPipeOffset = OverlappingPipeOffset = new Vector3(0.0001f, 0.0001f, 0);
            //protected 
            protected PipeFactoryBase(Pipe pipe)
            {
                _pipe = pipe;
                Init();
            }

            private void Init()
            {
                if (_pipe.Validity != Pipe.Status.Success) return;
                _segmentCount = (int)Mathf.Ceil(_pipe.Distance / PipeLength);
                _segmentOffset = _segmentCount * PipeLength - _pipe.Distance;
                _rotationOffset = (_pipe.Source.Position.y - _pipe.Destination.Position.y) * Vector3.down * 0.0002f;
            }

            protected virtual BaseEntity CreateSegment(Vector3 position, Quaternion rotation = default(Quaternion))
            {
                return GameManager.server.CreateEntity(Prefab, position, rotation);
            }

            public abstract void Create();
            
            public virtual void AttachPipeSegment(BaseEntity pipeSegmentEntity)
            {
                if (_pipe.Validity == Pipe.Status.Success) 
                    PipeSegment.Attach(pipeSegmentEntity, _pipe);
                Segments.Add(pipeSegmentEntity);
            }

            public virtual void AttachLights(BaseEntity pipeLightsEntity)
            {
                if (_pipe.Validity == Pipe.Status.Success)
                    PipeSegmentLights.Attach(pipeLightsEntity, _pipe);
                Lights.Add(pipeLightsEntity);
            }

            public virtual void Reverse() { }

            public virtual void Upgrade(BuildingGrade.Enum grade) { }

            public abstract void SetHealth(float health);

            protected abstract Vector3 SourcePosition { get; }

            protected abstract Quaternion Rotation { get; }

            protected abstract Vector3 GetOffsetPosition(int segmentIndex);

            protected virtual BaseEntity CreatePrimarySegment() => CreateSegment(SourcePosition, Rotation);

            protected virtual BaseEntity CreateSecondarySegment(int segmentIndex) => CreateSegment(GetOffsetPosition(segmentIndex));
        }

        private abstract class PipeFactoryBase<TEntity> : PipeFactoryBase
        where TEntity : BaseEntity
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
                pipeSegmentEntity.enableSaving = InstanceConfig.Experimental.PermanentEntities;
                pipeSegmentEntity.Spawn();

                AttachPipeSegment(pipeSegmentEntity);

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

            protected PipeFactoryBase(Pipe pipe) : base(pipe) { }
        }
    }
}
