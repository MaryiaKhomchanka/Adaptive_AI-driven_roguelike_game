/// <summary>
/// What the player has picked up during the current dungeon run. It only reaches SaveData when the run is won (portal);
/// dying, abandoning to the hub or the menu, or quitting throws it away. Anything else dropped inside a dungeon
/// (items, temporary upgrades) belongs here too, never directly in SaveData.
/// </summary>
public static class RunState
{
    public static int Coins { get; private set; }

    public static void AddCoins(int amount) => Coins += amount;

    /// <summary>Moves the run's loot into the save. Returns what was banked.</summary>
    public static int Bank()
    {
        var banked = Coins;
        SaveData.Current.coins += banked;
        Coins = 0;
        SaveData.Save();
        return banked;
    }

    /// <summary>Drops the run's loot. Returns what was lost.</summary>
    public static int Discard()
    {
        var lost = Coins;
        Coins = 0;
        return lost;
    }
}
