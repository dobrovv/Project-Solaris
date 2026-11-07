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

    public override string ToString()
    {
        if (id == 0) return "Neutral";
        if (id == 1) return "Player";
        return "AI_" + (id - 1);
    }
}

public class Factions
{
    private List<Faction> factions;
    public static Faction Neutral = new Faction(0, new Color( 138f/255f, 160f/255f, 189f/255f));
    public static Faction Player = new Faction(1, new Color(79f / 255f, 209f / 255f, 255f / 255f));
    private int ids = 2; // Start from 2 since 0 is reserved for Neutral and 1 for the player's faction

    public static readonly Color AI_1_COLOR = new Color(255f / 255f, 93f / 255f, 143f / 255f);

    public Factions()
    {
        factions = new List<Faction>();
        factions.Add(Neutral);
        factions.Add(Player);
    }

    public Faction CreateFaction()
    {
        Faction faction = new Faction(ids, CreateFactionColor(ids));
        ids++;
        factions.Add(faction);
        return faction;
    }

    public Faction GetFaction(int id) { return factions[id]; }

    public int GetFactionCount() { return factions.Count; }



    private Color CreateFactionColor(int id)
    {
        if (id == 0) // Neutral
            return new Color(138f / 255f, 160f / 255f, 189f / 255f);
        if (id == 1) // Player
            return new Color(79f / 255f, 209f / 255f, 255f / 255f);
        if (id == 2) // AI_1
            return new Color(255f / 255f, 93f / 255f, 143f / 255f);
        if (id == 3) // AI_2
            return new Color(255f / 255f, 179f / 255f, 71 / 255f);
        if (id == 4) // AI_3
            return new Color(94f / 255f, 242f / 255f, 160f / 255f);

        return Random.ColorHSV();
    }

}
