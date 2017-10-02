using System.Collections.Generic;

namespace Cod2GSI
{
    internal class StatusObject
    {

        public string MapName { get; set; }

        public List<PlayerScore> PlayerScores = new List<PlayerScore>();

    }

    public class PlayerScore
    {

        public string Name { get; set; }

        public string Guid { get; set; }

        public int Score { get; set; }

        public string Address { get; set; }

    }

}