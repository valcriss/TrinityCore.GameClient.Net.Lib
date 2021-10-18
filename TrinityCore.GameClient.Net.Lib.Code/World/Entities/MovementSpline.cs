using System;
using TrinityCore.GameClient.Net.Lib.World.Enums;
using TrinityCore.GameClient.Net.Lib.World.Navigation;

namespace TrinityCore.GameClient.Net.Lib.World.Entities
{
    internal class MovementSpline
    {
        internal int Duration { get; set; }
        internal int EffectStartTime { get; set; }
        internal float? FacingAngle { get; set; }
        internal float? FacingTarget { get; set; }
        internal Position FinalDestination { get; set; }
        internal Position FinalPosition { get; set; }
        internal SplineEvaluationMode SplineEvaluationMode { get; set; }
        internal SplineFlags? SplineFlags { get; set; }
        internal uint SplineId { get; set; }
        internal Position[] SplineNodes { get; set; }
        internal int TimePassed { get; set; }
        internal float VerticalAcceleration { get; set; }
        private DateTime SplineStart { get; set; }

        internal MovementSpline()
        {
            SplineStart = DateTime.Now;
        }
        internal Position CurrentPosition(float speed)
        {
            float totalTimePassed = (float)DateTime.Now.Subtract(SplineStart).TotalMilliseconds + TimePassed;
            float totalDistanceDone = speed * totalTimePassed;
            float loopLength = SplineNodes.GetPathLength();
            int loopsDone = (int)Math.Floor(totalDistanceDone / loopLength);

            float distanceDoneInPath = totalDistanceDone - (loopsDone * loopLength);

            for (int i = 0; i < SplineNodes.Length; i++)
            {
                Position node1 = SplineNodes[i];
                Position nextNode = (i == SplineNodes.Length - 1) ? SplineNodes[0] : SplineNodes[i + 1];
                float nodesDistance = (nextNode - node1).Length;
                if (distanceDoneInPath <= nodesDistance)
                {
                    // I am between this 2 points
                    Position result = node1 + (nextNode - node1).Direction * distanceDoneInPath;
                    return result;
                }
                else
                {
                    distanceDoneInPath -= nodesDistance;
                }
            }

            return FinalDestination;
        }
    }
}
