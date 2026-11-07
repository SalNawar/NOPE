using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

/// <summary>
/// The checks a player must pass after Unity calls its build "Succeeded"
/// (audit 2026-10-28, BLD-01 and BLD-02: on a near-full disk Unity wrote
/// resources.assets with a 3.7 GiB run of zero bytes, logged one
/// "success &amp;&amp; actual == size" assert for each 255 MiB chunk it could not
/// write, and still reported Succeeded; that player crashed at boot). Before
/// the build: enough free space on the output's drive and the project's drive
/// (<see cref="SpaceProblem"/>). After it: no write-failure assert in the
/// editor log of the build (<see cref="WriteFailures"/>), no long zero run in
/// any data file (<see cref="ZeroRuns"/>), and a boot: the player starts
/// hidden (batch mode, no window) with its own save folder, reaches the title
/// and stays up (<see cref="Boot"/>). The rules are pure, so they are tested
/// headless (BuildTests); DemoBuild applies them.
/// </summary>
public static class BuildVerify
{
    /// <summary>The free space each drive needs before a build: the player (about 3.6 GB with the character art as BC7, D11; 14 GB before) plus the player-data cache, with room to spare.</summary>
    public const long MinFreeBytes = 40L << 30;

    /// <summary>A zero run this long or longer in a data file is a hole the build failed to write (one failed chunk is 255 MiB; no real asset holds 32 MiB of zeros).</summary>
    public const long MinZeroRun = 32L << 20;

    /// <summary>The editor log's assert for a chunk of a data file that was not written whole.</summary>
    public const string WriteFailureText = "success && actual == size";

    /// <summary>Player log lines that mean the player's data is broken or it crashed.</summary>
    public static readonly string[] CrashTexts =
    {
        "Crash!!!",
        "The referenced script (Unknown) on this Behaviour is missing!",
        "Mismatched serialization",
        "is corrupted",
    };

    /// <summary>The longest wait for the player to reach the title.</summary>
    public const int BootTimeoutSeconds = 180;

    /// <summary>How long the player must stay up after it reached the title.</summary>
    public const int AliveAfterReadySeconds = 10;

    /// <summary>One run of zero bytes in a file.</summary>
    public readonly struct ZeroRun
    {
        /// <summary>The offset of the run's first zero byte.</summary>
        public readonly long Start;

        /// <summary>The run's length in bytes.</summary>
        public readonly long Length;

        /// <summary>A run from its parts.</summary>
        public ZeroRun(long start, long length)
        {
            Start = start;
            Length = length;
        }
    }

    /// <summary>Why the build must not start on <paramref name="drive"/> (null when it may): less than <paramref name="needBytes"/> free.</summary>
    public static string SpaceProblem(string drive, long freeBytes, long needBytes) =>
        freeBytes >= needBytes ? null
            : $"Drive {drive} has {Gigabytes(freeBytes)} free; a build needs {Gigabytes(needBytes)} (a full disk writes a broken player that Unity still calls Succeeded: BLD-01).";

    /// <summary>The number of write-failure asserts (<see cref="WriteFailureText"/>) in <paramref name="logText"/>.</summary>
    public static int WriteFailures(string logText)
    {
        if (string.IsNullOrEmpty(logText))
            return 0;
        int count = 0;
        for (int i = logText.IndexOf(WriteFailureText, StringComparison.Ordinal); i >= 0; i = logText.IndexOf(WriteFailureText, i + WriteFailureText.Length, StringComparison.Ordinal))
            count++;
        return count;
    }

    /// <summary>Every run of zero bytes at least <paramref name="minRun"/> long in <paramref name="data"/>, read from its current position to its end.</summary>
    public static List<ZeroRun> ZeroRuns(Stream data, long minRun)
    {
        var runs = new List<ZeroRun>();
        var buffer = new byte[4 << 20];
        long offset = 0, runStart = -1;
        int read;
        while ((read = data.Read(buffer, 0, buffer.Length)) > 0)
        {
            for (int i = 0; i < read; i++)
            {
                if (buffer[i] == 0)
                {
                    if (runStart < 0)
                        runStart = offset + i;
                }
                else if (runStart >= 0)
                {
                    long length = offset + i - runStart;
                    if (length >= minRun)
                        runs.Add(new ZeroRun(runStart, length));
                    runStart = -1;
                }
            }
            offset += read;
        }
        if (runStart >= 0 && offset - runStart >= minRun)
            runs.Add(new ZeroRun(runStart, offset - runStart));
        return runs;
    }

    /// <summary>
    /// Why the boot failed (null when it passed), from the player's log, whether
    /// the title's ready line was seen and whether the player was still up
    /// <see cref="AliveAfterReadySeconds"/> after it.
    /// </summary>
    public static string BootProblem(string playerLog, bool readySeen, bool aliveAfterReady)
    {
        string crash = CrashTexts.FirstOrDefault(t => (playerLog ?? "").Contains(t));
        if (crash != null)
            return $"The player's log says \"{crash}\": its data is broken or it crashed.";
        if (!readySeen)
            return $"The player did not reach the title in {BootTimeoutSeconds} s (no \"{TitleSceneController.ReadyLine}\" in its log).";
        if (!aliveAfterReady)
            return $"The player reached the title but did not stay up {AliveAfterReadySeconds} s.";
        return null;
    }

    /// <summary>A problem line for each long zero run in each file under <paramref name="dataFolder"/> (none: the data is whole).</summary>
    public static List<string> ScanData(string dataFolder)
    {
        var problems = new List<string>();
        if (!Directory.Exists(dataFolder))
        {
            problems.Add($"The player's data folder {dataFolder} is missing.");
            return problems;
        }
        foreach (string file in Directory.EnumerateFiles(dataFolder, "*", SearchOption.AllDirectories))
        {
            using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 16))
                foreach (ZeroRun run in ZeroRuns(stream, MinZeroRun))
                    problems.Add($"{file} has {Megabytes(run.Length)} of zero bytes from byte {run.Start}: a chunk the build did not write.");
        }
        return problems;
    }

    /// <summary>
    /// Starts <paramref name="exePath"/> hidden (batch mode, no graphics, no
    /// window) with its log and its save in <paramref name="workFolder"/>,
    /// waits for the title, then stops it. Returns the problem, or null when it
    /// booted.
    /// </summary>
    public static string Boot(string exePath, string workFolder)
    {
        Directory.CreateDirectory(workFolder);
        string saves = Path.Combine(workFolder, "saves");
        Directory.CreateDirectory(saves);
        string log = Path.Combine(workFolder, "boot_player.log");
        if (File.Exists(log))
            File.Delete(log);

        var start = new ProcessStartInfo(exePath,
            $"-batchmode -nographics -logFile \"{log}\" {SaveLocation.FolderSwitch} \"{saves}\"")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(exePath),
        };

        Process player;
        try { player = Process.Start(start); }
        catch (Exception e) { return $"The player could not be started: {e.Message}"; }
        if (player == null)
            return "The player could not be started.";

        try
        {
            bool ready = false;
            var clock = Stopwatch.StartNew();
            while (!ready && !player.HasExited && clock.Elapsed.TotalSeconds < BootTimeoutSeconds)
            {
                Thread.Sleep(500);
                string text = ReadShared(log);
                ready = text.Contains(TitleSceneController.ReadyLine);
                if (CrashTexts.Any(text.Contains))
                    break;
            }
            bool alive = ready && !player.WaitForExit(AliveAfterReadySeconds * 1000);
            return BootProblem(ReadShared(log), ready, alive);
        }
        finally
        {
            try
            {
                if (!player.HasExited)
                {
                    player.Kill();
                    player.WaitForExit(10000);
                }
            }
            catch (InvalidOperationException) { }
            player.Dispose();
        }
    }

    /// <summary>The text of <paramref name="path"/> while another process may still write it ("" when it does not exist yet).</summary>
    public static string ReadShared(string path) => ReadSharedFrom(path, 0);

    /// <summary>The text appended to <paramref name="path"/> after byte <paramref name="from"/> ("" when the file does not exist; all of it when the file is shorter, so a log that was started again is read whole).</summary>
    public static string ReadSharedFrom(string path, long from)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return "";
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
        {
            stream.Seek(from > stream.Length ? 0 : from, SeekOrigin.Begin);
            using (var reader = new StreamReader(stream))
                return reader.ReadToEnd();
        }
    }

    /// <summary>The length of <paramref name="path"/> (0 when it does not exist).</summary>
    public static long LengthOf(string path) => !string.IsNullOrEmpty(path) && File.Exists(path) ? new FileInfo(path).Length : 0L;

    /// <summary>The free bytes on the drive that holds <paramref name="path"/>.</summary>
    public static long FreeBytes(string path) => new DriveInfo(Path.GetPathRoot(Path.GetFullPath(path))).AvailableFreeSpace;

    private static string Gigabytes(long bytes) => $"{bytes / (double)(1L << 30):0.0} GB";

    private static string Megabytes(long bytes) => $"{bytes / (double)(1L << 20):0.0} MiB";
}
