// C#, Source/AssetRipper.Android/MainActivity.cs, .NET 10 Android, native Android ARM64
using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Database;
using Android.Net;
using Android.OS;
using Android.Provider;
using Android.Views;
using Android.Widget;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Util;

namespace AssetRipper.Android;

[Activity(
    Label = "AssetRipper",
    MainLauncher = true,
    Exported = true,
    Theme = "@android:style/Theme.Material.NoActionBar",
    ConfigurationChanges = global::Android.Content.PM.ConfigChanges.Orientation
        | global::Android.Content.PM.ConfigChanges.ScreenSize
        | global::Android.Content.PM.ConfigChanges.KeyboardHidden)]
public sealed class MainActivity : Activity
{
    private const int ImportRequestCode = 7401;
    private const int FolderRequestCode = 7402;
    private const int ExportRequestCode = 7403;

    private readonly List<Uri> selectedSources = [];
    private AndroidEngineService? engine;
    private LinearLayout? sourceList;
    private TextView? sourceSummary;
    private TextView? statusText;
    private Button? processButton;
    private Button? exportButton;
    private bool operationRunning;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        Window?.SetFlags(WindowManagerFlags.Fullscreen, WindowManagerFlags.Fullscreen);
        Window?.SetNavigationBarColor(Color.ParseColor("#101827"));
        Window?.SetStatusBarColor(Color.ParseColor("#101827"));
        Window?.DecorView?.SetSystemUiVisibility(
            StatusBarVisibility.Fullscreen
            | StatusBarVisibility.HideNavigation
            | StatusBarVisibility.ImmersiveSticky
            | StatusBarVisibility.LayoutStable);

        engine = new AndroidEngineService(FilesDir!.AbsolutePath);
        engine.LogReceived += OnEngineLog;
        BuildNativeInterface();
    }

    private void BuildNativeInterface()
    {
        ScrollView scroll = new(this) { FillViewport = true };
        scroll.SetBackgroundColor(Color.ParseColor("#0B1220"));
        LinearLayout page = new(this) { Orientation = Orientation.Vertical };
        page.SetPadding(Dp(22), Dp(30), Dp(22), Dp(28));

        page.AddView(Text("ASSET RIPPER", 13, "#8EA7CC", true));
        TextView title = Text("Unity asset extraction", 29, "#F4F7FC", true);
        title.SetPadding(0, Dp(10), 0, Dp(4));
        page.AddView(title);

        TextView subtitle = Text(
            "Import Unity game files, process their assets, and export a recovered Unity project.",
            15,
            "#A9B7CC");
        subtitle.SetPadding(0, 0, 0, Dp(24));
        page.AddView(subtitle);

        LinearLayout importCard = Card();
        importCard.AddView(Text("SOURCE FILES", 12, "#8EA7CC", true));
        sourceSummary = Text("No source selected", 15, "#F4F7FC");
        sourceSummary.SetPadding(0, Dp(9), 0, Dp(12));
        importCard.AddView(sourceSummary);

        Button filesButton = ActionButton("Choose Unity files", "#477FE8");
        filesButton.Click += (_, _) => OpenFilePicker();
        importCard.AddView(filesButton);

        Button folderButton = SecondaryButton("Choose a folder");
        LinearLayout.LayoutParams folderParams = new(
            ViewGroup.LayoutParams.MatchParent, Dp(50)) { TopMargin = Dp(10) };
        folderButton.Click += (_, _) => OpenFolderPicker();
        importCard.AddView(folderButton, folderParams);
        page.AddView(importCard);

        TextView selectedTitle = Text("SELECTED SOURCES", 12, "#8EA7CC", true);
        selectedTitle.SetPadding(2, Dp(25), 0, Dp(10));
        page.AddView(selectedTitle);

        sourceList = new LinearLayout(this) { Orientation = Orientation.Vertical };
        page.AddView(sourceList);

        processButton = ActionButton("Analyze and process", "#477FE8");
        processButton.Enabled = false;
        processButton.Click += async (_, _) => await ProcessSelectedSourcesAsync();
        LinearLayout.LayoutParams processParams = new(
            ViewGroup.LayoutParams.MatchParent, Dp(54)) { TopMargin = Dp(12) };
        page.AddView(processButton, processParams);

        exportButton = SecondaryButton("Export Unity project");
        exportButton.Enabled = false;
        exportButton.Click += (_, _) => OpenExportPicker();
        LinearLayout.LayoutParams exportParams = new(
            ViewGroup.LayoutParams.MatchParent, Dp(52)) { TopMargin = Dp(10) };
        page.AddView(exportButton, exportParams);

        LinearLayout statusCard = Card();
        statusCard.SetPadding(Dp(16), Dp(16), Dp(16), Dp(16));
        statusCard.AddView(Text("JOB STATUS", 12, "#75D6B0", true));
        statusText = Text(
            "Native interface ready. Select Unity files or a folder to begin.",
            14,
            "#D4DEEC");
        statusText.SetPadding(0, Dp(8), 0, 0);
        statusCard.AddView(statusText);
        LinearLayout.LayoutParams statusParams = new(
            ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        { TopMargin = Dp(20) };
        page.AddView(statusCard, statusParams);

        TextView footer = Text(
            "Android ARM64  •  Native UI  •  Direct engine integration",
            12,
            "#71819A");
        footer.SetPadding(2, Dp(22), 2, 0);
        page.AddView(footer);

        scroll.AddView(page);
        SetContentView(scroll);
    }

    private void OpenFilePicker()
    {
        Intent intent = new(Intent.ActionOpenDocument);
        intent.AddCategory(Intent.CategoryOpenable);
        intent.SetType("*/*");
        intent.PutExtra(Intent.ExtraAllowMultiple, true);
        intent.AddFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantPersistableUriPermission);
        StartActivityForResult(intent, ImportRequestCode);
    }

    private void OpenFolderPicker()
    {
        Intent intent = new(Intent.ActionOpenDocumentTree);
        intent.AddFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantPersistableUriPermission);
        StartActivityForResult(intent, FolderRequestCode);
    }

    private void OpenExportPicker()
    {
        if (engine?.IsLoaded != true || operationRunning)
            return;

        Intent intent = new(Intent.ActionOpenDocumentTree);
        intent.AddFlags(
            ActivityFlags.GrantReadUriPermission
            | ActivityFlags.GrantWriteUriPermission
            | ActivityFlags.GrantPersistableUriPermission);
        StartActivityForResult(intent, ExportRequestCode);
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        if (resultCode != Result.Ok || data?.Data is null && data?.ClipData is null)
            return;

        if (requestCode == ImportRequestCode)
        {
            if (data.ClipData is { ItemCount: > 0 } clip)
            {
                for (int index = 0; index < clip.ItemCount; index++)
                {
                    Uri? uri = clip.GetItemAt(index)?.Uri;
                    if (uri is not null)
                        AddSource(uri, data.Flags);
                }
            }
            else if (data.Data is not null)
            {
                AddSource(data.Data, data.Flags);
            }
            RefreshSourceList();
        }
        else if (requestCode == FolderRequestCode && data.Data is not null)
        {
            AddSource(data.Data, data.Flags);
            RefreshSourceList();
        }
        else if (requestCode == ExportRequestCode && data.Data is not null)
        {
            PersistUriPermission(data.Data, data.Flags);
            _ = ExportToSelectedFolderAsync(data.Data);
        }
    }

    private void AddSource(Uri uri, ActivityFlags resultFlags)
    {
        if (selectedSources.Any(existing => existing.ToString() == uri.ToString()))
            return;

        PersistUriPermission(uri, resultFlags);
        selectedSources.Add(uri);
    }

    private void PersistUriPermission(Uri uri, ActivityFlags resultFlags)
    {
        ActivityFlags granted = resultFlags
            & (ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission);
        if (granted == 0)
            return;

        try
        {
            ContentResolver?.TakePersistableUriPermission(uri, granted);
        }
        catch (Java.Lang.SecurityException exception)
        {
            global::Android.Util.Log.Warn("AssetRipper", $"Persistable URI permission unavailable: {exception.Message}");
        }
    }

    private void RefreshSourceList()
    {
        if (sourceList is null || sourceSummary is null)
            return;

        sourceList.RemoveAllViews();
        sourceSummary.Text = selectedSources.Count == 0
            ? "No source selected"
            : $"{selectedSources.Count} source(s) selected";

        foreach (Uri uri in selectedSources)
        {
            TextView item = Text(GetDisplayName(uri), 14, "#DCE6F5");
            item.SetPadding(Dp(13), Dp(12), Dp(13), Dp(12));
            GradientDrawable background = new();
            background.SetColor(Color.ParseColor("#172338"));
            background.SetCornerRadius(Dp(10));
            item.Background = background;
            LinearLayout.LayoutParams parameters = new(
                ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
            { BottomMargin = Dp(8) };
            sourceList.AddView(item, parameters);
        }

        if (processButton is not null)
            processButton.Enabled = selectedSources.Count > 0 && !operationRunning;
    }

    private async Task ProcessSelectedSourcesAsync()
    {
        if (engine is null || selectedSources.Count == 0 || operationRunning)
            return;

        SetOperationState(true, "Copying selected sources into private app storage...");
        try
        {
            string batchDirectory = Path.Combine(
                FilesDir!.AbsolutePath,
                "imports",
                DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff"));
            Directory.CreateDirectory(batchDirectory);

            List<string> stagedPaths = [];
            foreach (Uri uri in selectedSources)
            {
                if (DocumentsContract.IsTreeUri(uri))
                {
                    string folderName = SafeFileName(GetDisplayName(uri));
                    string folderPath = GetAvailablePath(batchDirectory, folderName);
                    Directory.CreateDirectory(folderPath);
                    await CopyTreeToDirectoryAsync(uri, folderPath);
                    stagedPaths.Add(folderPath);
                }
                else
                {
                    stagedPaths.Add(await CopyDocumentToDirectoryAsync(uri, batchDirectory));
                }
            }

            SetStatus("Loading and processing Unity assets...");
            await engine.LoadAndProcessAsync(stagedPaths);
            SetStatus("Processing completed. Choose an export destination.");
            if (exportButton is not null)
                exportButton.Enabled = engine.IsLoaded;
        }
        catch (Exception exception)
        {
            global::Android.Util.Log.Error("AssetRipper", exception.ToString());
            SetStatus($"Import failed: {exception.Message}");
            ShowError("Import failed", exception);
        }
        finally
        {
            SetOperationState(false, statusText?.Text ?? "Ready");
        }
    }

    private async Task ExportToSelectedFolderAsync(Uri treeUri)
    {
        if (engine is null || !engine.IsLoaded || operationRunning)
            return;

        SetOperationState(true, "Exporting Unity project...");
        string? stagingPath = null;
        try
        {
            stagingPath = Path.Combine(
                FilesDir!.AbsolutePath,
                "exports",
                DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff"));
            await engine.ExportUnityProjectAsync(stagingPath);

            Uri rootDocumentUri = DocumentsContract.BuildDocumentUriUsingTree(
                treeUri,
                DocumentsContract.GetTreeDocumentId(treeUri))
                ?? throw new IOException("The selected Android folder is not accessible.");

            await CopyDirectoryToTreeAsync(stagingPath, rootDocumentUri);
            SetStatus("Unity project export completed.");
        }
        catch (Exception exception)
        {
            global::Android.Util.Log.Error("AssetRipper", exception.ToString());
            SetStatus($"Export failed: {exception.Message}");
            ShowError("Export failed", exception);
        }
        finally
        {
            if (stagingPath is not null && Directory.Exists(stagingPath))
            {
                try { Directory.Delete(stagingPath, recursive: true); }
                catch (IOException exception) { global::Android.Util.Log.Warn("AssetRipper", exception.Message); }
            }
            SetOperationState(false, statusText?.Text ?? "Ready");
        }
    }

    private async Task<string> CopyDocumentToDirectoryAsync(Uri uri, string destination)
    {
        string displayName = SafeFileName(GetDisplayName(uri));
        string target = GetAvailablePath(destination, displayName);
        using Stream input = ContentResolver!.OpenInputStream(uri)
            ?? throw new IOException($"Unable to open selected document: {displayName}");
        await using FileStream output = File.Create(target);
        await input.CopyToAsync(output);
        return target;
    }

    private async Task CopyTreeToDirectoryAsync(Uri treeUri, string destination)
    {
        string rootDocumentId = DocumentsContract.GetTreeDocumentId(treeUri)
            ?? throw new IOException("Android storage provider returned no root document identifier.");
        await CopyDocumentChildrenAsync(treeUri, rootDocumentId, destination);
    }

    private async Task CopyDocumentChildrenAsync(Uri treeUri, string parentDocumentId, string destination)
    {
        Uri childrenUri = DocumentsContract.BuildChildDocumentsUriUsingTree(treeUri, parentDocumentId)
            ?? throw new IOException("Android storage provider could not enumerate the selected folder.");
        string[] projection =
        [
            DocumentsContract.Document.ColumnDocumentId,
            DocumentsContract.Document.ColumnDisplayName,
            DocumentsContract.Document.ColumnMimeType
        ];

        using ICursor? cursor = ContentResolver!.Query(childrenUri, projection, null, null, null);
        if (cursor is null)
            throw new IOException("Android storage provider did not return folder contents.");

        while (cursor.MoveToNext())
        {
            string documentId = cursor.GetString(0) ?? throw new IOException("Missing document identifier.");
            string name = SafeFileName(cursor.GetString(1) ?? "unnamed");
            string mimeType = cursor.GetString(2) ?? "application/octet-stream";
            string localPath = GetAvailablePath(destination, name);
            Uri documentUri = DocumentsContract.BuildDocumentUriUsingTree(treeUri, documentId)
                ?? throw new IOException("Android storage provider returned an invalid document URI.");

            if (mimeType == DocumentsContract.Document.MimeTypeDir)
            {
                Directory.CreateDirectory(localPath);
                await CopyDocumentChildrenAsync(treeUri, documentId, localPath);
            }
            else
            {
                using Stream input = ContentResolver!.OpenInputStream(documentUri)
                    ?? throw new IOException($"Unable to read {name} from Android storage.");
                await using FileStream output = File.Create(localPath);
                await input.CopyToAsync(output);
            }
        }
    }

    private async Task CopyDirectoryToTreeAsync(string sourceDirectory, Uri targetDirectory)
    {
        foreach (string directory in Directory.EnumerateDirectories(sourceDirectory))
        {
            string name = Path.GetFileName(directory);
            Uri created = DocumentsContract.CreateDocument(
                ContentResolver!,
                targetDirectory,
                DocumentsContract.Document.MimeTypeDir,
                name) ?? throw new IOException($"Android storage provider could not create folder '{name}'.");
            await CopyDirectoryToTreeAsync(directory, created);
        }

        foreach (string file in Directory.EnumerateFiles(sourceDirectory))
        {
            string name = Path.GetFileName(file);
            string extension = Path.GetExtension(name).TrimStart('.');
            string mimeType = MimeTypeMap.Singleton?.GetMimeTypeFromExtension(extension)
                ?? "application/octet-stream";
            Uri created = DocumentsContract.CreateDocument(ContentResolver!, targetDirectory, mimeType, name)
                ?? throw new IOException($"Android storage provider could not create file '{name}'.");
            await using FileStream input = File.OpenRead(file);
            using Stream output = ContentResolver!.OpenOutputStream(created, "w")
                ?? throw new IOException($"Android storage provider could not open '{name}' for writing.");
            await input.CopyToAsync(output);
        }
    }

    private string GetDisplayName(Uri uri)
    {
        using ICursor? cursor = ContentResolver!.Query(
            uri,
            [global::Android.Provider.IOpenableColumns.DisplayName],
            null,
            null,
            null);
        if (cursor is not null && cursor.MoveToFirst())
        {
            int index = cursor.GetColumnIndex(global::Android.Provider.IOpenableColumns.DisplayName);
            if (index >= 0 && !cursor.IsNull(index))
                return cursor.GetString(index) ?? "selected-file";
        }
        return uri.LastPathSegment ?? "selected-file";
    }

    private static string SafeFileName(string value)
    {
        string name = Path.GetFileName(value);
        return string.IsNullOrWhiteSpace(name) || name is "." or ".."
            ? $"source-{Guid.NewGuid():N}"
            : name;
    }

    private static string GetAvailablePath(string directory, string fileName)
    {
        string candidate = Path.Combine(directory, fileName);
        if (!File.Exists(candidate) && !Directory.Exists(candidate))
            return candidate;

        string name = Path.GetFileNameWithoutExtension(fileName);
        string extension = Path.GetExtension(fileName);
        for (int suffix = 2; ; suffix++)
        {
            candidate = Path.Combine(directory, $"{name} ({suffix}){extension}");
            if (!File.Exists(candidate) && !Directory.Exists(candidate))
                return candidate;
        }
    }

    private void OnEngineLog(global::AssetRipper.Import.Logging.LogType type,
        global::AssetRipper.Import.Logging.LogCategory category,
        string message)
    {
        RunOnUiThread(() =>
        {
            if (statusText is not null && !string.IsNullOrWhiteSpace(message))
                statusText.Text = message.Trim();
        });
    }

    private void SetOperationState(bool running, string message)
    {
        operationRunning = running;
        if (processButton is not null)
            processButton.Enabled = !running && selectedSources.Count > 0;
        if (exportButton is not null)
            exportButton.Enabled = !running && engine?.IsLoaded == true;
        SetStatus(message);
    }

    private void SetStatus(string message)
    {
        if (statusText is not null)
            statusText.Text = message;
    }

    private void ShowError(string title, Exception exception)
    {
        if (IsFinishing || IsDestroyed)
            return;

        new AlertDialog.Builder(this)
            .SetTitle(title)
            .SetMessage(exception.Message)
            .SetPositiveButton("OK", (_, _) => { })
            .Show();
    }

    private LinearLayout Card()
    {
        LinearLayout card = new(this) { Orientation = Orientation.Vertical };
        card.SetPadding(Dp(16), Dp(16), Dp(16), Dp(16));
        GradientDrawable background = new();
        background.SetColor(Color.ParseColor("#121E30"));
        background.SetCornerRadius(Dp(16));
        card.Background = background;
        return card;
    }

    private Button ActionButton(string label, string color)
    {
        Button button = new(this) { Text = label };
        button.SetTextColor(Color.White);
        button.SetAllCaps(false);
        button.TextSize = 15;
        button.SetBackgroundTintList(ColorStateList.ValueOf(Color.ParseColor(color)));
        button.LayoutParameters = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, Dp(52));
        return button;
    }

    private Button SecondaryButton(string label)
    {
        Button button = new(this) { Text = label };
        button.SetTextColor(Color.ParseColor("#E7EDF7"));
        button.SetAllCaps(false);
        button.TextSize = 15;
        button.SetBackgroundTintList(ColorStateList.ValueOf(Color.ParseColor("#263750")));
        return button;
    }

    private TextView Text(string value, float size, string color, bool bold = false)
    {
        TextView view = new(this)
        {
            Text = value,
            TextSize = size,
            Typeface = bold ? Typeface.DefaultBold : Typeface.Default
        };
        view.SetTextColor(Color.ParseColor(color));
        view.SetIncludeFontPadding(true);
        return view;
    }

    private int Dp(int value) =>
        (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, value, Resources!.DisplayMetrics);

    protected override void OnDestroy()
    {
        if (engine is not null)
        {
            engine.LogReceived -= OnEngineLog;
            engine.Dispose();
            engine = null;
        }
        base.OnDestroy();
    }
}
