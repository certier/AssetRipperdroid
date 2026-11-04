using AssetRipper.NativeDialogs;
using AssetRipper.Web.Extensions;
using Microsoft.AspNetCore.Http;

namespace AssetRipper.GUI.Web;

internal static class Dialogs
{
	public static bool Supported => AndroidDialogBridge.Supported || NativeDialog.Supported;

	internal static async Task<string[]?> SelectFilesAsync()
	{
		return AndroidDialogBridge.Supported
			? await AndroidDialogBridge.PickFilesAsync()
			: await OpenFileDialog.OpenFiles();
	}

	internal static async Task<string[]?> SelectFoldersAsync()
	{
		return AndroidDialogBridge.Supported
			? await AndroidDialogBridge.PickFoldersAsync()
			: await OpenFolderDialog.OpenFolders();
	}

	internal static async Task<string?> SelectFolderAsync()
	{
		if (AndroidDialogBridge.Supported)
		{
			string[] paths = await AndroidDialogBridge.PickFoldersAsync();
			return paths.FirstOrDefault();
		}
		return await OpenFolderDialog.OpenFolder();
	}

	public static class OpenFiles
	{
		public static async Task HandleGetRequest(HttpContext context)
		{
			context.Response.DisableCaching();
			string[] paths = await Dialogs.SelectFilesAsync() ?? [];
			await Results.Json(paths, AppJsonSerializerContext.Default.StringArray).ExecuteAsync(context);
		}
	}

	public static class OpenFolders
	{
		public static async Task HandleGetRequest(HttpContext context)
		{
			context.Response.DisableCaching();
			string[] paths = await Dialogs.SelectFoldersAsync() ?? [];
			await Results.Json(paths, AppJsonSerializerContext.Default.StringArray).ExecuteAsync(context);
		}
	}

	public static class OpenFile
	{
		public static async Task HandleGetRequest(HttpContext context)
		{
			context.Response.DisableCaching();
			string path = (await Dialogs.SelectFilesAsync())?.FirstOrDefault() ?? "";
			await Results.Json(path, AppJsonSerializerContext.Default.String).ExecuteAsync(context);
		}
	}

	public static class OpenFolder
	{
		public static async Task HandleGetRequest(HttpContext context)
		{
			context.Response.DisableCaching();
			string path = await Dialogs.SelectFolderAsync() ?? "";
			await Results.Json(path, AppJsonSerializerContext.Default.String).ExecuteAsync(context);
		}
	}

	public static class ExportFolder
	{
		public static async Task HandleGetRequest(HttpContext context)
		{
			context.Response.DisableCaching();
			string path = AndroidDialogBridge.ExportFolderPicker is not null
				? await AndroidDialogBridge.PickExportFolderAsync()
				: await OpenFolderDialog.OpenFolder() ?? "";
			await Results.Json(path, AppJsonSerializerContext.Default.String).ExecuteAsync(context);
		}
	}

	public static class SaveFile
	{
		public static async Task HandleGetRequest(HttpContext context)
		{
			context.Response.DisableCaching();
			string path = await SaveFileDialog.SaveFile() ?? "";
			await Results.Json(path, AppJsonSerializerContext.Default.String).ExecuteAsync(context);
		}
	}
}
