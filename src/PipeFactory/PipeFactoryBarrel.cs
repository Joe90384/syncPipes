using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    partial class SyncPipesDevelopment
    {
        private class PipeFactoryBarrel : PipeFactoryBase<StorageContainer>
        {
            protected override string Prefab => "assets/bundled/prefabs/radtown/loot_barrel_1.prefab";

            public PipeFactoryBarrel(Plugins.SyncPipesDevelopment.Pipe pipe) : base(pipe) { }

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
                return CreateSegment(GetOffsetPosition(segmentIndex), Quaternion.Euler(0f, segmentIndex * 80f, 0f));
            }
        }
    }
}
