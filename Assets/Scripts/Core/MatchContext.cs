using ChainNet.Data;
using ChainNet.Gameplay;
using ChainNet.RoguelikeMap;
using UnityEngine;

namespace ChainNet.Core
{
    /// <summary>
    /// Persists across scene loads to ferry match setup data from the map to the match scene.
    /// </summary>
    public class MatchContext : MonoBehaviour
    {
        public static MatchContext Instance { get; private set; }

        public TeamRuntime PlayerTeam { get; private set; }
        public TeamRuntime EnemyTeam { get; private set; }
        public CourtData Court { get; private set; }
        public MapNodeRuntime SourceNode { get; private set; }
        public bool IsBossMatch { get; private set; }

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

        public void PrepareMatch(TeamRuntime player, TeamRuntime enemy, CourtData court,
            MapNodeRuntime sourceNode, bool isBoss = false)
        {
            PlayerTeam = player;
            EnemyTeam = enemy;
            Court = court;
            SourceNode = sourceNode;
            IsBossMatch = isBoss;
        }

        public void Clear()
        {
            PlayerTeam = null;
            EnemyTeam = null;
            Court = null;
            SourceNode = null;
            IsBossMatch = false;
        }
    }
}
