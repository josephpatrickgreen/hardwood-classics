using System.Collections.Generic;
using ChainNet.Data;
using UnityEngine;

namespace ChainNet.Teams
{
    [CreateAssetMenu(menuName = "ChainNet/Data/Sample Library")]
    public class SampleDataLibrary : ScriptableObject
    {
        public List<CharacterData> characters = new();
        public List<TeamData> teams = new();
        public List<TrinketData> trinkets = new();
        public List<SpecialData> specials = new();
        public List<CourtData> courts = new();

        public bool HasMinimumPhaseOneData()
        {
            return characters.Count >= 6 && teams.Count >= 3 && trinkets.Count >= 5 && specials.Count >= 3 && courts.Count >= 2;
        }
    }
}
