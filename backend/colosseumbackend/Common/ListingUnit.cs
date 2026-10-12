namespace colosseumbackend.Common;

/// <summary>
/// What one part of a listing's price is paid for ("3 matches"). Stored and sent by name, so members
/// can be added in any order but never renamed. A listing without one shows its parts as "parts".
/// </summary>
public enum ListingUnit
{
    Match,
    Game,
    Round,
    Win,
    Run,
    Level,
    Rank,
    Hour,
    Day,
    Session,
    Item,
}
