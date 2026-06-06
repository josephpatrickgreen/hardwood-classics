using System;
using System.Collections.Generic;
using ChainNet.Data;
using ChainNet.RoguelikeMap;
using UnityEngine;

namespace ChainNet.Gameplay
{
    [Serializable]
    public class Injury
    {
        public string injuryId;
        public string displayName;
        public int matchDuration;
    }

    [Serializable]
    public class RewardData
    {
        public string rewardId;
        public string displayName;
        public int xp;
        public int cash;
        public TrinketData trinket;
        public bool healTeam;
    }

    [Serializable]
    public class PlayerRuntime
    {
        public CharacterData data;
        public Stats currentStats;
        public int level = 1;
        public int xp;
        public float stamina = 100f;
        public float specialCooldownRemaining;
        public List<TrinketData> equippedTrinkets = new();
        public bool isInjured;
        public Injury currentInjury;
        public bool injuryPenaltyApplied;
        public float injuryStaminaLost;

        public PlayerRuntime(CharacterData character)
        {
            data = character;
            currentStats = character.baseStats?.Clone() ?? new Stats();
        }
    }

    [Serializable]
    public class TeamRuntime
    {
        public TeamData data;
        public List<PlayerRuntime> players = new();

        public TeamRuntime(TeamData teamData)
        {
            data = teamData;
            foreach (var character in teamData.roster)
            {
                players.Add(new PlayerRuntime(character));
            }
        }
    }

    [Serializable]
    public class FoulOpportunity
    {
        public PlayerRuntime caller;
        public PlayerRuntime defender;
        public float contactIntensity;
        public bool dirtyActionUsed;
        public bool duringShot;
        public float timeRemaining;
    }

    [Serializable]
    public class DirtyPlayResult
    {
        public bool success;
        public float heatGenerated;
        public float injuryChance;
        public float foulChance;
        public float fightChance;
    }

    [Serializable]
    public class RunState
    {
        public string runId;
        public int cash;
        public TeamRuntime playerTeam;
        public List<MapNodeRuntime> mapNodes = new();
    }
}
