// C#, Source/AssetRipper.Android/AndroidEngineService.cs, .NET 10 Android, AssetRipper engine adapter
using AssetRipper.Export.Configuration;
using AssetRipper.Export.PrimaryContent;
using AssetRipper.Export.UnityProjects;
using AssetRipper.Import.Logging;
using AssetRipper.IO.Files;
using AssetRipper.Processing;

namespace AssetRipper.Android;

public sealed class AndroidEngineService : ILogger, IDisposable
{
    private readonly FullConfiguration settings;
    private readonly ExportHandler exportHandler;
    private GameData? gameData;
    private bool disposed;

    public event Action<LogType, LogCategory, string>? LogReceived;

    public bool IsLoaded => gameData is not null;

    public AndroidEngineService(string privateDataDirectory)
    {
        LocalFileSystem.SetExecutingDirectory(privateDataDirectory);
        settings = new FullConfiguration();
        settings.LoadFromDefaultPath();
        exportHandler = new ExportHandler(settings);
        Logger.Add(this);
    }

    public Task LoadAndProcessAsync(IReadOnlyList<string> inputPaths)
    {
        ArgumentNullException.ThrowIfNull(inputPaths);
        if (inputPaths.Count == 0)
            throw new ArgumentException("At least one input path is required.", nameof(inputPaths));

        string[] stablePaths = inputPaths.ToArray();
        return Task.Run(() =>
        {
            gameData = exportHandler.LoadAndProcess(stablePaths, LocalFileSystem.Instance);
        });
    }

    public Task ExportUnityProjectAsync(string outputPath)
    {
        GameData loaded = gameData ?? throw new InvalidOperationException("Load and process a Unity source before exporting.");
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        return Task.Run(() =>
        {
            Directory.CreateDirectory(outputPath);
            exportHandler.Export(loaded, outputPath, LocalFileSystem.Instance);
        });
    }

    public Task ExportPrimaryContentAsync(string outputPath)
    {
        GameData loaded = gameData ?? throw new InvalidOperationException("Load and process a Unity source before exporting.");
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        return Task.Run(() =>
        {
            Directory.CreateDirectory(outputPath);
            settings.ExportRootPath = outputPath;
            Logger.Info(LogCategory.Export, "Starting primary content export.");
            PrimaryContentExporter.CreateDefault(loaded, settings)
                .Export(loaded.GameBundle, settings, LocalFileSystem.Instance);
            Logger.Info(LogCategory.Export, "Finished primary content export.");
        });
    }

    public void Reset()
    {
        gameData = null;
        GC.Collect();
        Logger.Info(LogCategory.General, "Android session reset.");
    }

    public void Log(LogType type, LogCategory category, string message)
    {
        LogReceived?.Invoke(type, category, message);
    }

    public void BlankLine(int numLines)
    {
        LogReceived?.Invoke(LogType.Info, LogCategory.None, new string('\n', Math.Clamp(numLines, 1, 8)));
    }

    public void Dispose()
    {
        if (disposed)
            return;

        Logger.Remove(this);
        disposed = true;
    }
}
