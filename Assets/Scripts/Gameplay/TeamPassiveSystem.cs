using ChainNet.Data;
using ChainNet.Gameplay;
using UnityEngine;

namespace ChainNet.Gameplay
{
    /// <summary>
    /// Applies <see cref="TeamPassiveData"/> effects to every player on a team at match start.
    /// Add the passiveId defined here to the corresponding TeamData ScriptableObject in the editor.
    /// </summary>
    public static class TeamPassiveSystem
    {
        // ── Known passive IDs ─────────────────────────────────────────────────
        // Wire these strings into TeamData.passive.passiveId in the Unity editor.

        /// <summary>+3 Motor for every player. Deep bench endures.</summary>
        public const string BenchDepth = "bench-depth";

        /// <summary>+4 Swagger, +2 Cool for every player. Play in front of the crowd.</summary>
        public const string HomeCourtRules = "home-court-rules";

        /// <summary>+3 Edge for every player. They play mean.</summary>
        public const string ChainGang = "chain-gang";

        /// <summary>+3 Jumper for every player. Outside shooters always ready.</summary>
        public const string CornerPocket = "corner-pocket";

        /// <summary>+3 Clamps for every player. Suffocating defense unit.</summary>
        public const string LockdownUnit = "lockdown-unit";

        /// <summary>+3 Handle for every player. Ball movement crew.</summary>
        public const string PoundTheRock = "pound-the-rock";

        /// <summary>+3 Finish for every player. Live at the rim.</summary>
        public const string HighFlyers = "high-flyers";

        /// <summary>+2 Handle, Jumper, Finish, Clamps. Well-rounded tape crew.</summary>
        public const string TapeGang = "tape-gang";

        /// <summary>+3 Bounce, +3 Boards. Control the glass.</summary>
        public const string GlassEaters = "glass-eaters";

        /// <summary>+3 Nerve, +2 Swagger. Clutch performers.</summary>
        public const string NightOwls = "night-owls";

        // ─────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Apply passive stat bonuses to all players on <paramref name="team"/>.
        /// Safe to call with a null team or a team with no passive set.
        /// </summary>
        public static void Apply(TeamRuntime team)
        {
            if (team?.data?.passive == null) return;

            var passiveId = team.data.passive.passiveId;
            if (string.IsNullOrEmpty(passiveId)) return;

            foreach (var player in team.players)
                ApplyToPlayer(player, passiveId);

            Debug.Log($"[Passive] Applied '{passiveId}' ({team.data.passive.displayName}) " +
                      $"to team '{team.data.displayName}'.");
        }

        // ── Per-player application ─────────────────────────────────────────────
        private static void ApplyToPlayer(PlayerRuntime player, string passiveId)
        {
            switch (passiveId)
            {
                case BenchDepth:
                    player.currentStats.motor += 3;
                    break;

                case HomeCourtRules:
                    player.currentStats.swagger += 4;
                    player.currentStats.cool += 2;
                    break;

                case ChainGang:
                    player.currentStats.edge += 3;
                    break;

                case CornerPocket:
                    player.currentStats.jumper += 3;
                    break;

                case LockdownUnit:
                    player.currentStats.clamps += 3;
                    break;

                case PoundTheRock:
                    player.currentStats.handle += 3;
                    break;

                case HighFlyers:
                    player.currentStats.finish += 3;
                    break;

                case TapeGang:
                    player.currentStats.handle += 2;
                    player.currentStats.jumper += 2;
                    player.currentStats.finish += 2;
                    player.currentStats.clamps += 2;
                    break;

                case GlassEaters:
                    player.currentStats.bounce += 3;
                    player.currentStats.boards += 3;
                    break;

                case NightOwls:
                    player.currentStats.nerve += 3;
                    player.currentStats.swagger += 2;
                    break;
            }
        }
    }
}
