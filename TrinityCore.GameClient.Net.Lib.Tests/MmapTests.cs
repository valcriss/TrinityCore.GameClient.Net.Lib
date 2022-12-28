using System.Diagnostics;
using TrinityCore.GameClient.Net.Lib.Map;
using TrinityCore.GameClient.Net.Lib.Tests.Models;

namespace TrinityCore.GameClient.Net.Lib.Tests
{
    [TestClass]
    public class MmapTests
    {
        #region Public Methods

        [TestMethod]
        public void CalculateCorrectPathBetweenTiles()
        {
            MmapFilesCollection collection = MmapFilesCollection.Load(@"C:\Users\silve\Documents\wowData\3.3.5\mmaps");
            PathFindingFindPathTestCases testCases = PathFindingFindPathTestCases.Load();
            Assert.AreNotEqual(0, testCases.TestCases.Count);

            foreach (var testCase in testCases.TestCases)
            {
                Map.Path path = collection.PathFinding.FindPath(testCase.MapId, testCase.Source, testCase.Destination, 7f);
                Assert.IsNotNull(path);
            }
        }

        [TestMethod]
        public void CalculateFloorPosition()
        {
            MmapFilesCollection collection = MmapFilesCollection.Load(@"C:\Users\silve\Documents\wowData\3.3.5\mmaps");
            GetHeightAtPositionTestCases testCases = GetHeightAtPositionTestCases.Load();
            Assert.AreNotEqual(0, testCases.TestCases.Count);

            foreach (var testCase in testCases.TestCases)
            {
                MmapFile mmap = collection.GetMap(testCase.MapId);
                Assert.IsNotNull(mmap);
                MmapTileFile tile = mmap.GetMmapTileFileFromVector3(testCase.Correct);
                Assert.IsNotNull(tile);
                float? check = tile.GetHeightAtPosition(testCase.Correct);
                Assert.IsNotNull(check);
                float value = (float)check;
                Trace.WriteLine(testCase.Correct.Z - value);
            }
        }

        #endregion Public Methods
    }
}