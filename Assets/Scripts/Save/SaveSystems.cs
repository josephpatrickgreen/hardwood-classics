using System;
using System.Collections.Generic;
using System.IO;
using ChainNet.Data;
using ChainNet.Gameplay;
using UnityEngine;

namespace ChainNet.Save
{
    [Serializable]
    public class UnlockState
    {
        public List<string> unlockedCharacters = new();
        public List<string> unlockedTrinkets = new();
        public List<string> unlockedCourts = new();
        public List<string> unlockedTeams = new();
    }

    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }
        private string SavePath => Path.Combine(Application.persistentDataPath, "chain-net-run.json");

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SaveRun(RunState runState)
        {
            if (runState == null)
            {
                return;
            }

            var payload = JsonUtility.ToJson(new SerializableRunSummary
            {
                runId = runState.runId,
                cash = runState.cash,
                nodeCount = runState.mapNodes.Count
            });
            File.WriteAllText(SavePath, payload);
        }

        [Serializable]
        private class SerializableRunSummary
        {
            public string runId;
            public int cash;
            public int nodeCount;
        }
    }

    public class UnlockManager : MonoBehaviour
    {
        private const string UnlockKey = "chain-net-unlocks";
        public UnlockState State { get; private set; } = new();

        public void Load()
        {
            if (!PlayerPrefs.HasKey(UnlockKey))
            {
                return;
            }

            State = JsonUtility.FromJson<UnlockState>(PlayerPrefs.GetString(UnlockKey)) ?? new UnlockState();
        }

        public void Save()
        {
            PlayerPrefs.SetString(UnlockKey, JsonUtility.ToJson(State));
            PlayerPrefs.Save();
        }

        public bool IsUnlocked(UnlockCondition condition)
        {
            if (condition == null)
            {
                return true;
            }

            return condition.conditionType switch
            {
                UnlockConditionType.BeatTeam => State.unlockedTeams.Contains(condition.targetId),
                UnlockConditionType.BeatBoss => State.unlockedTeams.Contains(condition.targetId),
                _ => false
            };
        }
    }
}
