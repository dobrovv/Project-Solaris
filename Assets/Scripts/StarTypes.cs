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
    public static float GetStarTypeDiameter(StarType st)
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
}
