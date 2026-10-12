using System.Diagnostics;
using System.IO;
using PhysicsTasks.Labs;

namespace PhysicsTasks.Terminal.Labs;

/// <summary>
/// Запускает WPFPhisics1.1 с <c>--setup &lt;id&gt;</c> (та же логика, что <c>WpfBridgeLauncher</c> в игре).
/// </summary>
public sealed class WpfPhyzixLabNavigator : ILabNavigator
{
    public bool TryOpenLab(string? labWpfSetupId)
    {
        var id = labWpfSetupId?.Trim();
        if (string.IsNullOrEmpty(id) || string.Equals(id, "none", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var psi = ResolveLaunchTarget(id);
        if (psi is null)
        {
            return false;
        }

        try
        {
            using var p = Process.Start(psi);
            return p is not null;
        }
        catch
        {
            return false;
        }
    }

    private static ProcessStartInfo? ResolveLaunchTarget(string setupId)
    {
        // Рядом с PhysicsTasksTerminal.exe (копия из сборки игры / общий output).
        var sideBySide = Path.Combine(AppContext.BaseDirectory, "WPFPhisics1.1.exe");
        if (File.Exists(sideBySide))
        {
            var psi = new ProcessStartInfo
            {
                FileName = sideBySide,
                WorkingDirectory = AppContext.BaseDirectory,
                UseShellExecute = false,
            };
            psi.ArgumentList.Add("--setup");
            psi.ArgumentList.Add(setupId);
            return psi;
        }

        if (!TryFindWpfProjectDir(out var wpfProjectDir))
        {
            return null;
        }

        var outputCandidates = new[]
        {
            Path.Combine(wpfProjectDir, "bin", "Debug", "net9.0-windows"),
            Path.Combine(wpfProjectDir, "bin", "Release", "net9.0-windows"),
        };

        foreach (var dir in outputCandidates)
        {
            var exePath = Path.Combine(dir, "WPFPhisics1.1.exe");
            if (File.Exists(exePath))
            {
                var psi = new ProcessStartInfo
                {
                    FileName = exePath,
                    WorkingDirectory = dir,
                    UseShellExecute = false,
                };
                psi.ArgumentList.Add("--setup");
                psi.ArgumentList.Add(setupId);
                return psi;
            }
        }

        foreach (var dir in outputCandidates)
        {
            var dllPath = Path.Combine(dir, "WPFPhisics1.1.dll");
            if (!File.Exists(dllPath))
            {
                continue;
            }

            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                WorkingDirectory = dir,
                UseShellExecute = false,
            };
            psi.ArgumentList.Add(dllPath);
            psi.ArgumentList.Add("--setup");
            psi.ArgumentList.Add(setupId);
            return psi;
        }

        return null;
    }

    private static bool TryFindWpfProjectDir(out string wpfProjectDir)
    {
        wpfProjectDir = string.Empty;
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "WPFPhisics1.1", "WPFPhisics1.1");
            if (Directory.Exists(candidate))
            {
                wpfProjectDir = candidate;
                return true;
            }

            current = current.Parent;
        }

        return false;
    }
}
