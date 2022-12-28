using System.Numerics;

namespace TrinityCore.GameClient.Net.Lib.Tests.Models
{
    internal class PathFindingFindPathTestCase
    {
        #region Public Properties

        public Vector3 Destination { get; set; }
        public int MapId { get; set; }
        public Vector3 Source { get; set; }

        #endregion Public Properties
    }

    internal class PathFindingFindPathTestCases
    {
        #region Public Properties

        public List<PathFindingFindPathTestCase> TestCases { get; set; }

        #endregion Public Properties

        #region Public Constructors

        public PathFindingFindPathTestCases()
        {
            TestCases = new List<PathFindingFindPathTestCase>();
        }

        #endregion Public Constructors

        #region Public Methods

        public static PathFindingFindPathTestCases Load()
        {
            PathFindingFindPathTestCases tmp = new PathFindingFindPathTestCases();
            tmp.TestCases = new List<PathFindingFindPathTestCase>();
            string[] lines = File.ReadAllLines("Resources/PathFindingFindPathTestCases.csv");

            foreach (string line in lines)
            {
                string[] parts = line.Split(';');
                PathFindingFindPathTestCase testCase = new PathFindingFindPathTestCase()
                {
                    MapId = int.Parse(parts[0]),
                    Source = new Vector3(float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3])),
                    Destination = new Vector3(float.Parse(parts[4]), float.Parse(parts[5]), float.Parse(parts[6])),
                };
                tmp.TestCases.Add(testCase);
            }
            return tmp;
        }

        #endregion Public Methods
    }
}