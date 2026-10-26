using UnityEngine;

public static class SortingOrderConfig
{
    public const int BaseSortingOrder = 10;
    public const int StepPerRegion = 4;

    public static int GetToyOrder(int regionId)
    {
        return GetCatOrder(regionId) - 1;
    }

    public static int GetCatOrder(int regionId)
    {
        return BaseSortingOrder + (Mathf.Max(0, regionId) * StepPerRegion);
    }

    public static int GetMaskBackOrder(int regionId)
    {
        return GetToyOrder(regionId) - 1;
    }

    public static int GetTextOrder(int regionId)
    {
        return GetCatOrder(regionId) + 1;
    }
}
