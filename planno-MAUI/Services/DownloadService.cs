using CommunityToolkit.Maui.Storage;
#if ANDROID
using Android.Content;
using Android.Provider;
#endif

namespace planno_MAUI.Services
{
    public class DownloadService(IFileSaver fileSaver)
    {
        private readonly IFileSaver _fileSaver = fileSaver;

        // Gibt den Speicherort zurück oder null, wenn der Nutzer den Speichern-Dialog abgebrochen hat
        public async Task<string?> SaveAsync(string fileName, byte[] content, string mimeType)
        {
#if ANDROID
            // Android: direkt in den öffentlichen „Downloads“-Ordner, wie ein Browser-Download
            if (OperatingSystem.IsAndroidVersionAtLeast(29))
            {
                return await SaveToMediaStoreAsync(fileName, content, mimeType);
            }

            if (await Permissions.RequestAsync<Permissions.StorageWrite>() != PermissionStatus.Granted)
            {
                throw new UnauthorizedAccessException("Keine Berechtigung zum Speichern.");
            }

            string directory = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads)!.AbsolutePath;
            string path = Path.Combine(directory, fileName);
            await File.WriteAllBytesAsync(path, content);
            return path;
#else
            using var stream = new MemoryStream(content);
            var result = await _fileSaver.SaveAsync(fileName, stream, CancellationToken.None);

            if (result.IsSuccessful) return result.FilePath;
            if (result.Exception is FileSaveException) return null;   // Dialog abgebrochen

            throw result.Exception ?? new IOException("Speichern fehlgeschlagen.");
#endif
        }

#if ANDROID
        [System.Runtime.Versioning.SupportedOSPlatform("android29.0")]
        private static async Task<string> SaveToMediaStoreAsync(string fileName, byte[] content, string mimeType)
        {
            ContentResolver resolver = Android.App.Application.Context.ContentResolver!;

            var values = new ContentValues();
            values.Put(MediaStore.IMediaColumns.DisplayName, fileName);
            values.Put(MediaStore.IMediaColumns.MimeType, mimeType);
            values.Put(MediaStore.IMediaColumns.RelativePath, Android.OS.Environment.DirectoryDownloads);
            values.Put(MediaStore.IMediaColumns.IsPending, 1);

            Android.Net.Uri uri = resolver.Insert(MediaStore.Downloads.ExternalContentUri, values)
                ?? throw new IOException("Datei konnte nicht angelegt werden.");

            try
            {
                using (Stream output = resolver.OpenOutputStream(uri) ?? throw new IOException("Datei konnte nicht geöffnet werden."))
                {
                    await output.WriteAsync(content);
                }

                // Erst jetzt für andere Apps (Dateien, PDF-Viewer) sichtbar machen
                values.Clear();
                values.Put(MediaStore.IMediaColumns.IsPending, 0);
                resolver.Update(uri, values, null, null);
            }
            catch
            {
                resolver.Delete(uri, null, null);
                throw;
            }

            return $"Downloads/{fileName}";
        }
#endif
    }
}
