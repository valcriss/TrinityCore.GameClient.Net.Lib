using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using TrinityCore.GameClient.Net.Navigation.MmapEngine.MmapTile;
using TrinityCore.GameClient.Net.Navigation.MmapEngine.Tools;

namespace TrinityCore.GameClient.Net.Navigation.MmapEngine
{
    public static class PathFinding
    {
        #region Public Properties

        public static MmapFilesCollection Collection { get; set; }

        #endregion Public Properties

        #region Public Methods

        public static Path FindPath(int mapId, Vector3 start, Vector3 end, float speed)
        {
            DateTime durationStart = DateTime.Now;
            float distance = (start - end).Length();
            NavTrace.WriteLine(
                $"NAV ENGINE FIND_PATH map={mapId} " +
                $"startW=({start.X:F3},{start.Y:F3},{start.Z:F3}) endW=({end.X:F3},{end.Y:F3},{end.Z:F3}) " +
                $"startF=({start.ToFileFormat().X:F3},{start.ToFileFormat().Y:F3},{start.ToFileFormat().Z:F3}) " +
                $"endF=({end.ToFileFormat().X:F3},{end.ToFileFormat().Y:F3},{end.ToFileFormat().Z:F3})");
            MmapTileFileCollection tileCollection = MmapTileFileCollection.Factory(Collection, mapId, start, end);
            if (tileCollection == null)
            {
                NavTrace.WriteLine($"NAV ENGINE FIND_PATH map={mapId} tile collection unavailable");
                return null;
            }

            MmapTileFile startTile = tileCollection.StartTile;
            MmapTileFile endTile = tileCollection.EndTile;
            NavTrace.WriteLine($"NAV ENGINE TILES map={mapId} startTile={startTile.Key} endTile={endTile.Key} loaded={tileCollection.MmapTileFiles.Count}");

            if (startTile.Key == endTile.Key)
            {
                NavTrace.WriteLine("NAV ENGINE SAME_TILE navmesh path");
            }

            MmapMeshPoly startPoly = startTile.GetNearestPoly(start);
            if (startPoly == null) return null;
            MmapMeshPoly endPoly = endTile.GetNearestPoly(end);
            if (endPoly == null) return null;
            NavTrace.WriteLine($"NAV ENGINE POLYS start={startPoly.Key} end={endPoly.Key}");

            Queue<PathHypothesis> queue = new Queue<PathHypothesis>();
            queue.Enqueue(new PathHypothesis(startPoly, start));
            var explored = 0;

            while (queue.Count > 0)
            {
                PathHypothesis hypothesis = queue.Dequeue();
                List<MmapMeshPoly> linked = tileCollection.GetLinkedPolys(hypothesis.LastMeshPoly);
                foreach (MmapMeshPoly poly in linked.Where(c => !hypothesis.IsDone(c)).OrderBy(c => (c.Center() - end.ToFileFormat()).Length()))
                {
                    explored++;
                    var branch = hypothesis.Clone();
                    if (poly.Key == endPoly.Key)
                    {
                        // travel done
                        branch.Append(poly, end.ToFileFormat());
                        NavTrace.WriteLine(
                            $"NAV ENGINE PATH OK map={mapId} explored={explored} " +
                            $"durationMs={DateTime.Now.Subtract(durationStart).TotalMilliseconds:F1}");
                        return new Path(branch.GetPoints(), speed, mapId);
                    }
                    branch.Append(poly);
                    if (branch.Length < (distance * 5))
                        queue.Enqueue(branch);
                }
            }

            NavTrace.WriteLine(
                $"NAV ENGINE PATH FAIL map={mapId} explored={explored} " +
                $"durationMs={DateTime.Now.Subtract(durationStart).TotalMilliseconds:F1}");
            return null;
        }

        public static void Initialize(MmapFilesCollection collection)
        {
            Collection = collection;
        }

        #endregion Public Methods
    }

    public class Path
    {
        #region Public Properties

        public float CurrentOrientation
        {
            get
            {
                if (NextPointIndex < Points.Length)
                    return (Points[NextPointIndex] - CurrentPosition).DirectionOrientation;
                else
                    return (Points[NextPointIndex - 1] - Points[NextPointIndex - 2]).DirectionOrientation;
            }
        }

        public Point CurrentPosition
        {
            get => _currentPosition;
            private set => _currentPosition = value;
        }

        public Point Destination
        {
            get
            {
                return Points.Last();
            }
        }

        public int MapId
        {
            get;
            private set;
        }

        public Point[] Points { get; set; }

        public float Speed
        {
            get;
            set;
        }

        #endregion Public Properties

        #region Private Properties

        private int NextPointIndex { get; set; }

        #endregion Private Properties

        #region Private Fields

        private static readonly int MaxClosePositionCounter = 4;
        private int _closePositionCounter;
        private Point _currentPosition;
        private Point _previousPosition;

        #endregion Private Fields

        #region Public Constructors

        public Path(List<Point> points, float speed, int mapId)
        {
            if (points == null || points.Count < 2)
                throw new ArgumentException("Argument cannot be null or a list with just 1 point", nameof(points));
            Points = points.ToArray();

            for (var index = 0; index < Points.Length; index++)
            {
                Points[index] = new Point(Points[index].X, Points[index].Y, Points[index].Z);
            }

            if (speed <= 0.0f)
                throw new ArgumentException("Argument must be a positive number", nameof(speed));
            Speed = speed;

            CurrentPosition = Points[0];
            NextPointIndex = 1;
            MapId = mapId;
            _previousPosition = CurrentPosition;
            _closePositionCounter = 0;
        }

        #endregion Public Constructors

        #region Public Methods

        public static float GetOrientation(float x1, float y1, float z1, float x2, float y2, float z2)
        {
            return (new Point(x2, y2, z2) - new Point(x1, y1, z1)).DirectionOrientation;
        }

        public Point MoveAlongPath(float deltaTime)
        {
            float totalDistance = deltaTime * Speed;

            if (Points.Length <= NextPointIndex) return Points[^1];

            float distanceToNextPoint = (Points[NextPointIndex] - _currentPosition).Length;
            if (totalDistance < distanceToNextPoint)
            {
                Point result = _currentPosition + (Points[NextPointIndex] - _currentPosition).Direction * totalDistance;
                _currentPosition = result;
            }
            else
            {
                NextPoint(totalDistance, distanceToNextPoint);
            }

            if ((_currentPosition - _previousPosition).Length < 1f)
            {
                _closePositionCounter++;
                if (_closePositionCounter >= MaxClosePositionCounter)
                {
                    _closePositionCounter = 0;
                    NextPoint(totalDistance, distanceToNextPoint);
                }
            }
            else
            {
                _previousPosition = _currentPosition;
                _closePositionCounter = 0;
            }

            return _currentPosition;
        }

        #endregion Public Methods

        #region Private Methods

        private void NextPoint(float totalDistance, float distanceToNextPoint)
        {
            NextPointIndex++;
            if (NextPointIndex >= Points.Length - 1)
                _currentPosition = Points.Last();
            else
            {
                float remainingTime = (totalDistance - distanceToNextPoint) / Speed;
                _currentPosition = MoveAlongPath(remainingTime);
            }
        }

        #endregion Private Methods
    }

    public sealed class PathHypothesis
    {
        #region Public Properties

        public List<string> Done { get; set; }
        public MmapMeshPoly LastMeshPoly { get; set; }
        public float Length { get; set; }
        public List<Vector3> Points { get; set; }

        #endregion Public Properties

        #region Public Constructors

        public PathHypothesis(MmapMeshPoly lastMeshPoly, Vector3 start)
        {
            Points = new List<Vector3>() { start.ToFileFormat() };
            Length = 0;
            Done = new List<string>() { lastMeshPoly.Key };
            LastMeshPoly = lastMeshPoly;
        }

        private PathHypothesis(MmapMeshPoly lastMeshPoly, float length, List<string> done, List<Vector3> points)
        {
            LastMeshPoly = lastMeshPoly;
            Length = length;
            Done = done;
            Points = points;
        }

        #endregion Public Constructors

        #region Public Methods

        public void Append(MmapMeshPoly lastMeshPoly, Vector3? final = null)
        {
            if (Points.Count > 0)
            {
                Length += (lastMeshPoly.Center() - Points[^1]).Length();
            }
            LastMeshPoly = lastMeshPoly;
            Done.Add(lastMeshPoly.Key);
            Points.Add(lastMeshPoly.Center());
            if (final != null)
            {
                Length += (final.Value - Points[^1]).Length();
                Points.Add(final.Value);
            }
        }

        public PathHypothesis Clone()
        {
            return new PathHypothesis(
                LastMeshPoly,
                Length,
                new List<string>(Done),
                new List<Vector3>(Points));
        }

        public List<Point> GetPoints()
        {
            List<Point> tmp = new List<Point>();
            foreach (Vector3 vector in Points)
            {
                Vector3 v = vector.ToWorldFormat();
                Vector3 h = PathFinding.Collection.ClosestPointAtPosition(LastMeshPoly.MmapMesh.MmapTileFile.MapId, v);
                tmp.Add(new Point(h.X, h.Y, h.Z));
            }
            return tmp;
        }

        public bool IsDone(MmapMeshPoly poly)
        {
            return Done.Contains(poly.Key);
        }

        #endregion Public Methods
    }
}


