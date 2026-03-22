using HarmonyLib;
using Timberborn.ModManagerScene;

namespace FulgurFangs.Code;

public sealed class ModStarter : IModStarter
{
    private static bool _patched;

    public void StartMod(IModEnvironment modEnvironment)
    {
        if (_patched)
        {
            return;
        }

        var harmony = new Harmony("dima2.fulgurfangs");
        harmony.PatchAll(typeof(ModStarter).Assembly);
        _patched = true;
    }
}
