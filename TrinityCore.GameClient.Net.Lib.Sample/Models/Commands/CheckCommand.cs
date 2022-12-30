using System;
using System.Numerics;
using TrinityCore.GameClient.Net.Lib.Components.Entities;
using TrinityCore.GameClient.Net.Lib.Components.Entities.Models;
using TrinityCore.GameClient.Net.Lib.Components.Social;
using TrinityCore.GameClient.Net.Lib.Components.Zone;
using TrinityCore.GameClient.Net.Lib.Map;
using TrinityCore.GameClient.Net.Lib.Map.Tools;
using TrinityCore.GameClient.Net.Lib.Network.World.Models;

namespace TrinityCore.GameClient.Net.Lib.Sample.Models.Commands
{
    internal class CheckCommand : Command
    {
        #region Private Properties

        private string TestCaseFile { get; set; }

        #endregion Private Properties

        #region Public Constructors

        public CheckCommand()
        {
            TestCaseFile = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "GetHeightAtPositionTestCases.csv");
        }

        #endregion Public Constructors

        #region Public Methods

        public override bool Handle(string command)
        {
            if (command.ToLower() == "check")
            {
                Player other = GameClient.Get<EntitiesComponent>().FindPlayerByName("Daniel");

                if (other == null)
                {
                    return false;
                }

                int mapId = GameClient.Get<ZoneComponent>().WorldState.MapId;
                Position current = other.GetPosition();
                MmapFilesCollection collection = MmapFilesCollection.Load(System.IO.Path.Combine(GameClient.Get().GetDataPath(), "mmaps"));
                MmapFile mmap = collection.GetMap(mapId);
                MmapTileFile tile = mmap.GetMmapTileFileFromVector3(current);
                Vector3 check = tile.ClosestPointAtPosition(current.ToVector3());
                GameClient.Get<SocialComponent>().Shout("Position : " + current.ToString());
                GameClient.Get<SocialComponent>().Shout("GetHeightAtPosition : " + check.ToString());

                System.IO.File.AppendAllText(TestCaseFile, $"{mapId};{current.X};{current.Y};{current.Z};{check}\n");

                return true;
            }
            return false;
        }

        #endregion Public Methods
    }
}