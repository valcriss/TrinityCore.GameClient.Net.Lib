using System.Numerics;

namespace TrinityCore.GameClient.Net.Lib.Tests.Models
{
    internal class GetHeightAtPositionTestCase
    {
        #region Public Properties

        public float Calculated { get; set; }
        public Vector3 Correct { get; set; }
        public int MapId { get; set; }

        #endregion Public Properties
    }

    internal class GetHeightAtPositionTestCases
    {
        #region Public Properties

        public List<GetHeightAtPositionTestCase> TestCases { get; set; }

        #endregion Public Properties

        #region Public Constructors

        public GetHeightAtPositionTestCases()
        {
            TestCases = new List<GetHeightAtPositionTestCase>();
        }

        #endregion Public Constructors

        #region Public Methods

        public static GetHeightAtPositionTestCases Load()
        {
            GetHeightAtPositionTestCases tmp = new GetHeightAtPositionTestCases();
            tmp.TestCases = new List<GetHeightAtPositionTestCase>();
            string[] lines = File.ReadAllLines("Resources/GetHeightAtPositionTestCases.csv");

            foreach (string line in lines)
            {
                string[] parts = line.Split(';');
                GetHeightAtPositionTestCase testCase = new GetHeightAtPositionTestCase()
                {
                    MapId = int.Parse(parts[0]),
                    Correct = new Vector3(float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3])),
                    Calculated = float.Parse(parts[4])
                };
                tmp.TestCases.Add(testCase);
            }
            return tmp;
        }

        #endregion Public Methods
    }
}