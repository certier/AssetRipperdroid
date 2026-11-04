// C#, Source/AssetRipper.GUI.Web/AndroidDialogBridge.cs, .NET 10, Android platform bridge
namespace AssetRipper.GUI.Web;

public enum AndroidPickerKind
{
    Files,
    Folders,
}

public static class AndroidDialogBridge
{
    public static Func<AndroidPickerKind, Task<string[]>>? Picker { get; set; }
    public static Func<Task<string>>? ExportFolderPicker { get; set; }
    public static Func<string, Task>? ExportCompleted { get; set; }

    public static bool Supported => Picker is not null;

    public static Task<string[]> PickFilesAsync()
    {
        return Picker is { } picker
            ? picker(AndroidPickerKind.Files)
            : Task.FromResult(Array.Empty<string>());
    }

    public static Task<string[]> PickFoldersAsync()
    {
        return Picker is { } picker
            ? picker(AndroidPickerKind.Folders)
            : Task.FromResult(Array.Empty<string>());
    }

    public static Task<string> PickExportFolderAsync()
    {
        return ExportFolderPicker is { } picker
            ? picker()
            : Task.FromResult(string.Empty);
    }

    public static Task CompleteExportAsync(string path)
    {
        return ExportCompleted is { } completed
            ? completed(path)
            : Task.CompletedTask;
    }
}
