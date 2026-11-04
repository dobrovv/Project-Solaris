using UnityEngine;

public enum StarType
{
    Normal,
    Production,
    Economic,
    Defensive,
    WhiteDwarf,
    GasGiant
}


public class StarTypes
{
    public static float GetStarTypeRadius(StarType st)
    {
        switch (st)
        {
            case StarType.Normal:
                return 0.5f;
            case StarType.Production:
                return 0.5f;
            case StarType.Economic:
                return 0.5f;
            case StarType.Defensive:
                return 0.5f;
            case StarType.WhiteDwarf:
                return 0.38f;
            case StarType.GasGiant:
                return 0.75f;
            default:
                return 0.5f;
        }
    }

    public static int GetStarTypeMaxEnergyRate(StarType st)
    {
        switch (st)
        {
            case StarType.Normal:
                return 100;
            case StarType.Production:
                return 150;
            case StarType.Economic:
                return 100;
            case StarType.Defensive:
                return 100;
            case StarType.WhiteDwarf:
                return 35;
            case StarType.GasGiant:
                return 250;
            default:
                return 100;
        }
    }

    public static float GetStarTypeProductionRate(StarType st)
    {
        switch (st)
        {
            case StarType.Normal:
                return 2f;
            case StarType.Production:
                return 2.8f;
            case StarType.Economic:
                return 2f;
            case StarType.Defensive:
                return 1f;
            case StarType.WhiteDwarf:
                return 0.85f;
            case StarType.GasGiant:
                return 2.8f;
            default:
                return 2f;
        }
    }

    public static float GetStarTypeDampingRate(StarType st)
    {
        switch (st)
        {
            case StarType.Normal:
                return 0.1f;
            case StarType.Production:
                return 0.1f;
            case StarType.Economic:
                return 0.1f;
            case StarType.Defensive:
                return 0.1f;
            case StarType.WhiteDwarf:
                return 0.1f;
            case StarType.GasGiant:
                return 0.1f;
            default:
                return 0.1f;
        }
    }
}
