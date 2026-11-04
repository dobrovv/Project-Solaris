using UnityEngine;
using System.Collections.Generic;

public class Faction
{
    public int id = 0;
    public Color color = Color.grey;

    public Faction(int id, Color color)
    {
        this.id = id;
        this.color = color;
    }
}

public class Factions
{
    private List<Faction> factions;
    public static Faction Neutral = new Faction(0, new Color( 138f/255f, 160f/255f, 189f/255f));
    public static Faction Player = new Faction(1, new Color(79f / 255f, 209f / 255f, 255f / 255f));
    public static int ids = 2; // Start from 2 since 0 is reserved for Neutral and 1 for the player's faction

    public static readonly Color AI_1_COLOR = new Color(255f / 255f, 93f / 255f, 143f / 255f);

    public Factions()
    {
        factions = new List<Faction>();
        factions.Add(Neutral);
        factions.Add(Player);
    }

    public Faction CreateFaction(Color color)
    {
        Faction faction = new Faction(ids++, color);
        factions.Add(faction);
        return faction;
    }

}
