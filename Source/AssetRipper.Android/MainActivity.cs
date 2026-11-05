// C#, Source/AssetRipper.Android/MainActivity.cs, .NET 10 Android, native Android ARM64
using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Net;
using Android.OS;
using Android.Views;
using Android.Widget;
using Android.Graphics;
using Android.Graphics.Drawables;

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

    private readonly List<Uri> selectedSources = [];
    private LinearLayout? sourceList;
    private TextView? sourceSummary;
    private TextView? statusText;

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

        BuildNativeInterface();
    }

    private void BuildNativeInterface()
    {
        ScrollView scroll = new(this)
        {
            FillViewport = true
        };
        scroll.SetBackgroundColor(Color.ParseColor("#0B1220"));

        LinearLayout page = new(this)
        {
            Orientation = Orientation.Vertical
        };
        page.SetPadding(Dp(22), Dp(30), Dp(22), Dp(28));

        TextView brand = Text("ASSET RIPPER", 13, "#8EA7CC", true);
        page.AddView(brand);

        TextView title = Text("Unity asset extraction", 29, "#F4F7FC", true);
        title.SetPadding(0, Dp(10), 0, Dp(4));
        page.AddView(title);

        TextView subtitle = Text(
            "Import Unity game files, inspect the job, and export the recovered project.",
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
        folderButton.Click += (_, _) => OpenFolderPicker();
        LinearLayout.LayoutParams folderParams = new(
            ViewGroup.LayoutParams.MatchParent,
            Dp(50));
        folderParams.TopMargin = Dp(10);
        importCard.AddView(folderButton, folderParams);
        page.AddView(importCard);

        TextView selectedTitle = Text("SELECTED SOURCES", 12, "#8EA7CC", true);
        selectedTitle.SetPadding(2, Dp(25), 0, Dp(10));
        page.AddView(selectedTitle);

        sourceList = new LinearLayout(this)
        {
            Orientation = Orientation.Vertical
        };
        page.AddView(sourceList);

        LinearLayout statusCard = Card();
        statusCard.SetPadding(Dp(16), Dp(16), Dp(16), Dp(16));
        statusCard.AddView(Text("NATIVE ANDROID BUILD", 12, "#75D6B0", true));
        statusText = Text(
            "Native interface is active. The AssetRipper engine adapter and extraction workflow are the next implementation stage.",
            14,
            "#D4DEEC");
        statusText.SetPadding(0, Dp(8), 0, 0);
        statusCard.AddView(statusText);
        LinearLayout.LayoutParams statusParams = new(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent);
        statusParams.TopMargin = Dp(20);
        page.AddView(statusCard, statusParams);

        TextView footer = Text(
            "Android ARM64  •  Native UI  •  Engine integration in progress",
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

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        if (resultCode != Result.Ok || data is null)
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
        }
        else if (requestCode == FolderRequestCode && data.Data is not null)
        {
            AddSource(data.Data, data.Flags);
        }

        RefreshSourceList();
    }

    private void AddSource(Uri uri, ActivityFlags resultFlags)
    {
        if (selectedSources.Any(existing => existing.ToString() == uri.ToString()))
            return;

        ActivityFlags granted = resultFlags
            & (ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission);
        if (granted != 0)
        {
            try
            {
                ContentResolver?.TakePersistableUriPermission(uri, granted);
            }
            catch (Java.Lang.SecurityException exception)
            {
                global::Android.Util.Log.Warn("AssetRipper", $"Persistable URI permission unavailable: {exception.Message}");
            }
        }

        selectedSources.Add(uri);
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
            TextView item = Text(uri.LastPathSegment ?? uri.ToString(), 14, "#DCE6F5");
            item.SetPadding(Dp(13), Dp(12), Dp(13), Dp(12));
            GradientDrawable background = new();
            background.SetColor(Color.ParseColor("#172338"));
            background.SetCornerRadius(Dp(10));
            item.Background = background;

            LinearLayout.LayoutParams parameters = new(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent);
            parameters.BottomMargin = Dp(8);
            sourceList.AddView(item, parameters);
        }
    }

    private LinearLayout Card()
    {
        LinearLayout card = new(this)
        {
            Orientation = Orientation.Vertical
        };
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
            ViewGroup.LayoutParams.MatchParent,
            Dp(52));
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
        (int)TypedValue.ApplyDimension(
            ComplexUnitType.Dip,
            value,
            Resources!.DisplayMetrics);
}
