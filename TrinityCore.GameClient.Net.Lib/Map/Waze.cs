﻿using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using TrinityCore.Map.Net.IO;
using TrinityCore.GameClient.Net.Lib.World.Navigation;
using TrinityCore.GameClient.Net.Lib.Log;

namespace TrinityCore.GameClient.Net.Lib.Map
{
    public class Waze
    {
        private static MmapFilesCollection Collection { get; set; }
        public static void Initialize(string dataDirectory)
        {
            Collection = MmapFilesCollection.Load(System.IO.Path.Combine(dataDirectory, "mmaps"));
        }

        public static Path CalculatePath(Position start, Position end, uint mapId, float speed)
        {
            return new Path(new List<Point>() { new Point(start.X, start.Y, start.Z), new Point(end.X, end.Y, end.Z) }, speed, (int)mapId);
            //return Collection.PathFinding.FindPath((int)mapId, start.Vector3, end.Vector3, speed);
        }

    }
}