using UnityEngine;

namespace ChainNet.Data
{
    public enum MapNodeType
    {
        PickupGame,
        RivalGame,
        DirtyCourt,
        MoneyGame,
        Trainer,
        SneakerShop,
        TapeDealer,
        OpenGym,
        BackAlley,
        BarberShop,
        CornerStore,
        MiniBossCourt,
        BossCourt
    }

    public enum CharacterArchetype
    {
        HandleGod,
        RimWrecker,
        PlaygroundBig,
        MidrangeKiller,
        NoLookProphet,
        Lockdown,
        Enforcer,
        OldHead,
        Trickster
    }

    public enum TrinketRarity
    {
        Common,
        Uncommon,
        Rare,
        Legendary,
        Cursed
    }

    public enum TrinketScope
    {
        Team,
        Player
    }

    public enum SpecialType
    {
        YoYoPass,
        PosterChild,
        ChainNetSniper,
        NoBloodNoFoul,
        PocketCheck,
        MilkCrateFloater,
        ElbowRoom,
        BoomBoxBounce,
        FenceRattler,
        HometownWhistle,
        NeonStepback,
        OldManPivot
    }

    public enum CourtRule
    {
        Standard,
        WinnersBall,
        NoBloodNoFoul,
        HouseRef,
        DoubleRim,
        ChainFence,
        LowLight,
        RooftopWind,
        CrackedAsphalt,
        ExhibitionRules
    }

    public enum BallState
    {
        Held,
        Passing,
        Shooting,
        Loose,
        Rebounding,
        Scored
    }

    public enum HypeLevel
    {
        Quiet,
        Buzzing,
        Hot,
        CrowdIn,
        Legendary,
        TapeMoment
    }

    public enum UnlockConditionType
    {
        BeatTeam,
        BeatBoss,
        WinFightAgainstTeam,
        MakeJumpersAgainstTeam,
        UseSpecialsInMatch,
        WinWithoutCallingFoul,
        WinWithNoInjuries,
        BlockSpecificCharacter,
        ReachMaxHype
    }
}
