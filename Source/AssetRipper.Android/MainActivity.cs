// C#, Source/AssetRipper.Android/MainActivity.cs, .NET 10 Android, Android ARM64
using Android.App;
using Android.OS;
using Android.Views;
using Android.Webkit;
using AssetRipper.GUI.Web;

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
    private const string LocalAddress = "http://127.0.0.1:18555/";
    private WebView? webView;
    private bool serverStarted;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

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
        webView.Settings.MixedContentMode = MixedContentHandling.Never;
        webView.SetWebViewClient(new WebViewClient());
        SetContentView(webView);

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

    private void ShowStartupError(Exception exception)
    {
        if (IsFinishing || IsDestroyed)
            return;

        string message = exception.ToString();

        new AlertDialog.Builder(this)
            .SetTitle("Startup failed")
            .SetMessage(message)
            .SetPositiveButton("Retry", (_, _) =>
            {
                serverStarted = false;
                StartAssetRipper();
            })
            .SetNegativeButton("Close", (_, _) => Finish())
            .Show();
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
        webView?.StopLoading();
        webView?.Destroy();
        webView = null;
        base.OnDestroy();
    }
}
