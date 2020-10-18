using UnityEngine;

namespace Oxide.Plugins
{
    partial class SyncPipes
    {
        /// <summary>
        /// Base class for handling the pipe segment behaviour
        /// It ensures that the pipe and its segments will be destroyed if a container is destroyed or picked up
        /// It also allows for tracking hammer hits on pipe segments
        /// </summary>
        abstract class PipeSegmentBase : MonoBehaviour
        {
            /// <summary>
            /// Pipe that this segment belongs to
            /// </summary>
            public Pipe Pipe { get; private set; }

            // Useful for debugging
            private BaseEntity _parent;

            /// <summary>
            /// Hook used to check the validity of the segment
            /// </summary>
            void Update()
            {
                if (Pipe?.IsAlive() ?? false) return;
                Instance.NextFrame(() =>
                {
                    var pipe = Pipe;
                    Pipe = null;
                    pipe?.Kill();
                    Destroy(this);
                });
            }

            protected void Init(Pipe pipe, BaseEntity parent)
            {
                Pipe = pipe;
                _parent = parent;
            }
        }

        /// <summary>
        /// Attach a pipe segment to a pipe
        /// </summary>
        class PipeSegment : PipeSegmentBase
        {
            public static void Attach(BaseEntity pipeEntity, Pipe pipe) => pipeEntity.gameObject.AddComponent<PipeSegment>().Init(pipe, pipeEntity);
        }

        /// <summary>
        /// Detach a pipe segment from a pipe
        /// </summary>
        class PipeSegmentLights : PipeSegmentBase
        {
            public static void Attach(BaseEntity pipeEntity, Pipe pipe) => pipeEntity.gameObject.AddComponent<PipeSegmentLights>().Init(pipe, pipeEntity);
        }
    }
}
