using System.Diagnostics;

namespace Vein.Ue4ss.DumpLog;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        if (args.Any(arg => arg.Equals("--self-test", StringComparison.OrdinalIgnoreCase)))
        {
            return RunSelfTest();
        }

        if (args.Any(arg => arg.Equals("--render-screenshots", StringComparison.OrdinalIgnoreCase)))
        {
            return RenderScreenshots();
        }

        if (args.Any(arg => arg.Equals("--export-readable", StringComparison.OrdinalIgnoreCase)))
        {
            return ExportReadableDump();
        }

        if (args.Any(arg => arg.Equals("--export-full-game", StringComparison.OrdinalIgnoreCase)))
        {
            return ExportFullGameDump(args.Any(arg => arg.Equals("--copy-files", StringComparison.OrdinalIgnoreCase)));
        }

        if (args.Any(arg => arg.Equals("--print-live-view", StringComparison.OrdinalIgnoreCase)))
        {
            return PrintLiveView();
        }

        Application.Run(new MainForm(new Ue4ssService(AppPaths.Default)));
        return 0;
    }

    private static int RunSelfTest()
    {
        var service = new Ue4ssService(AppPaths.Default);
        var status = service.GetStatus();

        Console.WriteLine($"Win64:       {service.Paths.Win64Directory}");
        Console.WriteLine($"UE4SS:       {service.Paths.Ue4ssDirectory}");
        Console.WriteLine($"UE4SS.dll:   {status.Ue4ssDllFound}");
        Console.WriteLine($"mods.txt:    {status.ModsTxtFound}");
        Console.WriteLine($"Keybinds:    {status.KeybindsFound}");
        Console.WriteLine($"UE4SS.log:   {status.LogFound}");
        Console.WriteLine($"VEIN running:{status.GameRunning}");
        Console.WriteLine($"CE toolkit:  {service.CheatEngineWorkflowDirectory ?? "missing"}");
        Console.WriteLine($"Cheat Engine:{service.CheatEngineExecutablePath ?? "missing"}");

        return status.Ue4ssDllFound && status.ModsTxtFound && status.KeybindsFound ? 0 : 2;
    }

    private static int RenderScreenshots()
    {
        try
        {
            var outputDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "outputs", "vein-ue4ss-control-panel-screens"));
            Directory.CreateDirectory(outputDir);

            using var form = new MainForm(new Ue4ssService(AppPaths.Default));
            form.StartPosition = FormStartPosition.Manual;
            form.ShowInTaskbar = false;
            form.Opacity = 0;
            form.Show();

            RenderForm(form, new Size(1072, 682), Path.Combine(outputDir, "dump-log-min-1072x682.png"));
            RenderForm(form, new Size(1072, 682), Path.Combine(outputDir, "dumper-1072x682.png"));
            RenderForm(form, new Size(1280, 760), Path.Combine(outputDir, "dump-log-1280x760.png"));
            RenderForm(form, new Size(1600, 900), Path.Combine(outputDir, "dump-log-1600x900.png"));

            foreach (var page in new[] { "LiveView", "Watches", "Dumpers", "BpMods", "LuaDebugger", "Files", "Settings" })
            {
                form.SelectPageForTesting(page);
                RenderForm(form, new Size(1072, 682), Path.Combine(outputDir, $"{page.ToLowerInvariant()}-1072x682.png"));
            }

            form.SelectSettingsSectionForTesting("Credits");
            RenderForm(form, new Size(1072, 682), Path.Combine(outputDir, "settings-credits-1072x682.png"));

            Console.WriteLine(outputDir);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static int ExportReadableDump()
    {
        var result = new Ue4ssService(AppPaths.Default).ExportReadableObjectDump();
        Console.WriteLine(result.Message);
        return result.Success ? 0 : 1;
    }

    private static int ExportFullGameDump(bool copyFiles)
    {
        var result = new Ue4ssService(AppPaths.Default).ExportFullGameDump(copyFiles);
        Console.WriteLine(result.Message);
        return result.Success ? 0 : 1;
    }

    private static int PrintLiveView()
    {
        var text = new Ue4ssService(AppPaths.Default).BuildLiveObjectDumpView(null, 80);
        Console.WriteLine(text);
        return string.IsNullOrWhiteSpace(text) ? 1 : 0;
    }

    private static void RenderForm(Form form, Size size, string path)
    {
        form.ClientSize = size;
        form.PerformLayout();
        Application.DoEvents();

        using var bitmap = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
        bitmap.Save(path);
    }
}
