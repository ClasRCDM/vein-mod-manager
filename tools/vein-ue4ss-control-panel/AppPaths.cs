namespace Vein.Ue4ss.DumpLog;

internal sealed record AppPaths(
    string VeinRoot,
    string Win64Directory,
    string Ue4ssDirectory,
    string ModsDirectory,
    string ModsTxt,
    string Ue4ssLog,
    string KeybindsScript)
{
    public static AppPaths Default
    {
        get
        {
            var veinRoot = @"C:\Program Files (x86)\Steam\steamapps\common\Vein\Vein";
            var win64 = Path.Combine(veinRoot, "Binaries", "Win64");
            var ue4ss = Path.Combine(win64, "ue4ss");
            var mods = Path.Combine(ue4ss, "Mods");
            return new AppPaths(
                veinRoot,
                win64,
                ue4ss,
                mods,
                Path.Combine(mods, "mods.txt"),
                Path.Combine(ue4ss, "UE4SS.log"),
                Path.Combine(mods, "Keybinds", "Scripts", "main.lua"));
        }
    }
}
