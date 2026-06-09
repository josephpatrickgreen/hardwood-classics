using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChainNet.Data
{
    [Serializable]
    public class UnlockCondition
    {
        public UnlockConditionType conditionType;
        public string targetId;
        public int requiredAmount = 1;
    }

    [Serializable]
    public class TeamPassiveData
    {
        public string passiveId;
        public string displayName;
        [TextArea] public string description;
    }

    [Serializable]
    public class TrinketModifier
    {
        public string stat;
        public float amount;
        public bool additive = true;
    }

    [CreateAssetMenu(menuName = "ChainNet/Data/Special")]
    public class SpecialData : ScriptableObject
    {
        public string specialId;
        public string displayName;
        [TextArea] public string description;
        public float cooldownSeconds;
        public int cooldownPossessions;
        public SpecialType specialType;
        public GameObject vfxPrefab;
    }

    [CreateAssetMenu(menuName = "ChainNet/Data/Court")]
    public class CourtData : ScriptableObject
    {
        public string courtId;
        public string displayName;
        [TextArea] public string description;
        public GameObject courtPrefab;
        public CourtRule courtRule;
        public float foulStrictness;
        public float dirtyPlayTolerance;
        public float fightChanceMultiplier = 1f;
        public float hypeMultiplier = 1f;
        public float heatMultiplier = 1f;
    }

    [CreateAssetMenu(menuName = "ChainNet/Data/Character")]
    public class CharacterData : ScriptableObject
    {
        public string characterId;
        public string displayName;
        public string nickname;
        public CharacterArchetype archetype;
        public Sprite portrait;
        public GameObject playerPrefab;
        public Stats baseStats;
        public SpecialData special;
        public List<string> tags = new();
        public UnlockCondition unlockCondition;
    }

    [CreateAssetMenu(menuName = "ChainNet/Data/Team")]
    public class TeamData : ScriptableObject
    {
        public string teamId;
        public string displayName;
        [TextArea] public string description;
        public List<CharacterData> roster = new();
        public CourtData homeCourt;
        public TeamPassiveData passive;
        public List<string> tags = new();
    }

    [CreateAssetMenu(menuName = "ChainNet/Data/Trinket")]
    public class TrinketData : ScriptableObject
    {
        public string trinketId;
        public string displayName;
        [TextArea] public string description;
        public TrinketRarity rarity;
        public TrinketScope scope;
        public List<string> tags = new();
        public List<TrinketModifier> modifiers = new();
    }

    [CreateAssetMenu(menuName = "ChainNet/Data/Node")]
    public class MapNodeData : ScriptableObject
    {
        public string nodeId;
        public MapNodeType nodeType;
        public float weight = 1f;
    }

    [CreateAssetMenu(menuName = "ChainNet/Data/Event")]
    public class EventData : ScriptableObject
    {
        public string eventId;
        public string displayName;
        [TextArea] public string description;
    }
}
