using System.Collections.Generic;
using UnityEngine;

namespace ChainNet.Data
{
    [CreateAssetMenu(menuName = "ChainNet/Data/Prototype Catalog")]
    public class PrototypeCatalog : ScriptableObject
    {
        [Header("Phase 1 Minimums")]
        public List<string> characterIds = new()
        {
            "jalen-jaywalk-price",
            "marcus-milkcrate-bell",
            "tony-tape-deck-rivera",
            "reggie-receipt-knox",
            "big-rome",
            "percy-yoyo-valentine"
        };

        public List<string> teamIds = new()
        {
            "bench-mob",
            "corner-kings",
            "chain-lords"
        };

        public List<string> trinketIds = new()
        {
            "chain-net",
            "taped-fingers",
            "brass-whistle",
            "scuffed-high-tops",
            "pump-sneakers",
            "gold-chain",
            "taped-knuckles",
            "vhs-highlight-tape",
            "lucky-tube-socks",
            "blacktop-lawyer",
            "cracked-backboard-shard",
            "loose-elbows",
            "streetlight-bulb",
            "payphone-token",
            "corner-store-soda",
            "bent-rim-charm",
            "clean-jersey",
            "no-blood-poster",
            "boom-box-battery",
            "mystery-tape"
        };

        public List<string> specialIds = new()
        {
            "yoyo-pass",
            "poster-child",
            "chain-net-sniper"
        };

        public List<string> courtIds = new()
        {
            "schoolyard",
            "the-cage"
        };
    }
}
