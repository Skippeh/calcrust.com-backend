namespace RustCalc
{
    internal static class Utility
    {
        public static readonly string[] ItemExcludeList =
        {
            "ammo.rocket.smoke", // WIP Smoke Rocket
            "generator.wind.scrap", // Wind Turbine
        };

        public static readonly string[] EndsWithBlacklist =
        {
            ".twig.prefab",
            ".wood.prefab",
            ".stone.prefab",
            ".metal.prefab",
            ".toptier.prefab",
            ".item.prefab",
            ".close-end.prefab",
            "open-end.prefab",
            "close-start.prefab",
            "open-start.prefab",
            "close-end.asset",
            "open-end.asset",
            "close-start.asset",
            "open-start.asset",
            "impact.prefab",
            "knock.prefab",
            "ladder_prop.prefab",
            "-deploy.asset",
            ".skinnable.asset"
        };
    }
}