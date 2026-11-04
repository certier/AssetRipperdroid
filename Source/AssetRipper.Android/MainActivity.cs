// C#, Source/AssetRipper.Android/MainActivity.cs, .NET 10 Android, Android ARM64
using Android.App;
using Android.Content;
using Android.Database;
using Android.Net;
using Android.OS;
using Android.Provider;
using Android.Views;
using Android.Webkit;
using Uri = Android.Net.Uri;
using AssetRipper.GUI.Web;
using AssetRipper.IO.Files;

namespace AssetRipper.Android;

[Activity(
    Label = "AssetRipper",
    MainLauncher = true,
    Exported = true,
    ConfigurationChanges = global::Android.Content.PM.ConfigChanges.Orientation
        | global::Android.Content.PM.ConfigChanges.ScreenSize
        | global::Android.Content.PM.ConfigChanges.KeyboardHidden)]
public sealed class MainActivity : Activity
{
    private const int ServerPort = 18555;
    private const int ImportRequestCode = 18556;
    private const int ExportRequestCode = 18557;
    private const string LocalAddress = "http://127.0.0.1:18555/";

    private WebView? webView;
    private bool serverStarted;
    private TaskCompletionSource<string[]>? pendingImport;
    private TaskCompletionSource<string>? pendingExportFolder;
    private Uri? exportTreeUri;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        LocalFileSystem.SetExecutingDirectory(FilesDir!.AbsolutePath);
        Window?.SetSoftInputMode(SoftInput.AdjustResize);
        webView = new WebView(this)
        {
            LayoutParameters = new ViewGroup.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent)
        };

        webView.Settings.JavaScriptEnabled = true;
        webView.Settings.DomStorageEnabled = true;
        webView.Settings.AllowFileAccess = false;
        webView.Settings.AllowContentAccess = true;
        webView.Settings.MixedContentMode = MixedContentHandling.NeverAllow;
        webView.SetWebViewClient(new WebViewClient());
        SetContentView(webView);

        AndroidDialogBridge.Picker = PickImportAsync;
        AndroidDialogBridge.ExportFolderPicker = PickExportFolderAsync;
        AndroidDialogBridge.ExportCompleted = CopyExportToSafAsync;

        StartAssetRipper();
    }

    private void StartAssetRipper()
    {
        if (serverStarted)
            return;

        serverStarted = true;
        string logPath = Path.Combine(FilesDir!.AbsolutePath, "assetripper-android.log");

        _ = Task.Run(() =>
        {
            try
            {
                WebApplicationLauncher.Launch(
                    port: ServerPort,
                    headless: true,
                    log: true,
                    logPath: logPath);
            }
            catch (Exception exception)
            {
                global::Android.Util.Log.Error("AssetRipper", exception.ToString());
                RunOnUiThread(() => ShowStartupError(exception));
            }
        });

        _ = WaitForServerAndLoadAsync();
    }

    private async Task WaitForServerAndLoadAsync()
    {
        using HttpClient client = new()
        {
            Timeout = TimeSpan.FromSeconds(2)
        };

        Exception? lastError = null;
        for (int attempt = 0; attempt < 40; attempt++)
        {
            try
            {
                using HttpResponseMessage response = await client.GetAsync(LocalAddress);
                if (response.IsSuccessStatusCode)
                {
                    RunOnUiThread(() => webView?.LoadUrl(LocalAddress));
                    return;
                }
            }
            catch (Exception exception)
            {
                lastError = exception;
            }

            await Task.Delay(500);
        }

        RunOnUiThread(() => ShowStartupError(lastError
            ?? new TimeoutException("AssetRipper did not start listening in time.")));
    }

    private Task<string[]> PickImportAsync(AndroidPickerKind kind)
    {
        TaskCompletionSource<string[]> completion = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        RunOnUiThread(() =>
        {
            if (pendingImport is not null)
            {
                completion.TrySetException(new InvalidOperationException("Another Android picker is already open."));
                return;
            }

            pendingImport = completion;
            Intent intent;
            if (kind == AndroidPickerKind.Files)
            {
                intent = new Intent(Intent.ActionOpenDocument);
                intent.AddCategory(Intent.CategoryOpenable);
                intent.SetType("*/*");
                intent.AddFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantPersistableUriPermission);
                intent.PutExtra(Intent.ExtraAllowMultiple, true);
            }
            else
            {
                intent = new Intent(Intent.ActionOpenDocumentTree);
                intent.AddFlags(ActivityFlags.GrantReadUriPermission
                    | ActivityFlags.GrantWriteUriPermission
                    | ActivityFlags.GrantPersistableUriPermission);
            }

            try
            {
                StartActivityForResult(intent, ImportRequestCode);
            }
            catch (Exception exception)
            {
                pendingImport = null;
                completion.TrySetException(exception);
            }
        });

        return completion.Task;
    }

    private Task<string> PickExportFolderAsync()
    {
        TaskCompletionSource<string> completion = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        RunOnUiThread(() =>
        {
            if (pendingExportFolder is not null)
            {
                completion.TrySetException(new InvalidOperationException("Another Android picker is already open."));
                return;
            }

            pendingExportFolder = completion;
            Intent intent = new(Intent.ActionOpenDocumentTree);
            intent.AddFlags(ActivityFlags.GrantReadUriPermission
                | ActivityFlags.GrantWriteUriPermission
                | ActivityFlags.GrantPersistableUriPermission);

            try
            {
                StartActivityForResult(intent, ExportRequestCode);
            }
            catch (Exception exception)
            {
                pendingExportFolder = null;
                completion.TrySetException(exception);
            }
        });

        return completion.Task;
    }

    protected override async void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);

        if (requestCode == ImportRequestCode)
        {
            TaskCompletionSource<string[]>? completion = pendingImport;
            pendingImport = null;
            if (completion is null)
                return;

            if (resultCode != Result.Ok || data is null)
            {
                completion.TrySetResult([]);
                return;
            }

            try
            {
                Uri? tree = data.Data;
                if (tree is not null)
                    PersistUriPermission(tree, data.Flags);

                string staging = Path.Combine(FilesDir!.AbsolutePath, "imports", Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(staging);

                List<Uri> selected = [];
                if (data.ClipData is { ItemCount: > 0 } clip)
                {
                    for (int i = 0; i < clip.ItemCount; i++)
                    {
                        Uri? uri = clip.GetItemAt(i)?.Uri;
                        if (uri is not null)
                            selected.Add(uri);
                    }
                }
                else if (data.Data is not null)
                {
                    selected.Add(data.Data);
                }

                List<string> paths = [];
                foreach (Uri uri in selected)
                {
                    if (DocumentsContract.IsTreeUri(uri))
                    {
                        string folder = Path.Combine(staging, "folder");
                        Directory.CreateDirectory(folder);
                        await CopyTreeToDirectoryAsync(uri, folder);
                        paths.Add(folder);
                    }
                    else
                    {
                        paths.Add(await CopyDocumentToDirectoryAsync(uri, staging));
                    }
                }

                completion.TrySetResult(paths.ToArray());
            }
            catch (Exception exception)
            {
                completion.TrySetException(exception);
            }
            return;
        }

        if (requestCode == ExportRequestCode)
        {
            TaskCompletionSource<string>? completion = pendingExportFolder;
            pendingExportFolder = null;
            if (completion is null)
                return;

            if (resultCode != Result.Ok || data?.Data is not Uri tree)
            {
                completion.TrySetResult(string.Empty);
                return;
            }

            try
            {
                PersistUriPermission(tree, data.Flags);
                exportTreeUri = tree;
                string stagingPath = Path.Combine(FilesDir!.AbsolutePath, "exports", Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(stagingPath);
                completion.TrySetResult(stagingPath);
            }
            catch (Exception exception)
            {
                completion.TrySetException(exception);
            }
        }
    }

    private void PersistUriPermission(Uri uri, ActivityFlags flags)
    {
        ActivityFlags granted = flags & (ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission);
        if (granted == 0)
            return;

        try
        {
            ContentResolver!.TakePersistableUriPermission(uri, granted);
        }
        catch (global::Java.Lang.SecurityException exception)
        {
            global::Android.Util.Log.Warn("AssetRipper", $"Storage provider did not grant persistent access: {exception.Message}");
        }
    }

    private async Task<string> CopyDocumentToDirectoryAsync(Uri uri, string destination)
    {
        string displayName = Path.GetFileName(GetDisplayName(uri));
        string target = GetAvailablePath(destination, displayName);
        using Stream input = ContentResolver!.OpenInputStream(uri)
            ?? throw new IOException($"Unable to open selected document: {displayName}");
        await using FileStream output = File.Create(target);
        await input.CopyToAsync(output);
        return target;
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
        return $"selected-{Guid.NewGuid():N}";
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
            string name = Path.GetFileName(cursor.GetString(1) ?? "unnamed") ?? "unnamed";
            string mimeType = cursor.GetString(2) ?? "application/octet-stream";
            Uri documentUri = DocumentsContract.BuildDocumentUriUsingTree(treeUri, documentId)
                ?? throw new IOException("Android storage provider returned an invalid document URI.");
            string localPath = Path.Combine(destination, name);

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

    private async Task CopyExportToSafAsync(string exportedPath)
    {
        if (exportTreeUri is null || string.IsNullOrWhiteSpace(exportedPath) || !Directory.Exists(exportedPath))
            return;

        Uri selectedTree = exportTreeUri;
        string rootDocumentId = DocumentsContract.GetTreeDocumentId(selectedTree)
            ?? throw new IOException("Android storage provider returned no export folder identifier.");
        Uri rootDocumentUri = DocumentsContract.BuildDocumentUriUsingTree(selectedTree, rootDocumentId)
            ?? throw new IOException("Android storage provider returned an invalid export folder URI.");

        await CopyDirectoryToTreeAsync(exportedPath, rootDocumentUri);
        exportTreeUri = null;

        try
        {
            Directory.Delete(exportedPath, recursive: true);
        }
        catch (IOException exception)
        {
            global::Android.Util.Log.Warn("AssetRipper", $"Could not remove completed export staging data: {exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            global::Android.Util.Log.Warn("AssetRipper", $"Could not remove completed export staging data: {exception.Message}");
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

    private void ShowStartupError(Exception exception)
    {
        if (IsFinishing || IsDestroyed)
            return;

        AlertDialog.Builder builder = new(this);
        builder.SetTitle("Startup failed");
        builder.SetMessage(exception.ToString());
        builder.SetPositiveButton("Retry", (_, _) =>
        {
            serverStarted = false;
            StartAssetRipper();
        });
        builder.SetNegativeButton("Close", (_, _) => Finish());
        builder.Show();
    }

    public override void OnBackPressed()
    {
        if (webView?.CanGoBack() == true)
            webView.GoBack();
        else
            base.OnBackPressed();
    }

    protected override void OnDestroy()
    {
        pendingImport?.TrySetCanceled();
        pendingImport = null;
        pendingExportFolder?.TrySetCanceled();
        pendingExportFolder = null;
        exportTreeUri = null;

        AndroidDialogBridge.Picker = null;
        AndroidDialogBridge.ExportFolderPicker = null;
        AndroidDialogBridge.ExportCompleted = null;
        webView?.StopLoading();
        webView?.Destroy();
        webView = null;
        base.OnDestroy();
    }
}
