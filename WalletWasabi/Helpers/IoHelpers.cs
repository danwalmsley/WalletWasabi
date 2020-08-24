using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using WalletWasabi.Crypto;
using WalletWasabi.Helpers;
using WalletWasabi.Logging;

namespace System.IO
{
	public static class IoHelpers
	{
		/// <summary>
		/// Attempts to delete <paramref name="directory"/> with retry feature to allow File Explorer or any other
		/// application that holds the directory handle of the <paramref name="directory"/> to release it.
		/// </summary>
		/// <see href="https://stackoverflow.com/questions/329355/cannot-delete-directory-with-directory-deletepath-true/14933880#44324346"/>
		public static async Task<bool> TryDeleteDirectoryAsync(string directory, int maxRetries = 10, int millisecondsDelay = 100)
		{
			if (directory is null)
			{
				throw new ArgumentNullException(directory);
			}

			if (maxRetries < 1)
			{
				throw new ArgumentOutOfRangeException(nameof(maxRetries));
			}

			if (millisecondsDelay < 1)
			{
				throw new ArgumentOutOfRangeException(nameof(millisecondsDelay));
			}

			for (int i = 0; i < maxRetries; ++i)
			{
				try
				{
					if (Directory.Exists(directory))
					{
						Directory.Delete(directory, recursive: true);
					}

					return true;
				}
				catch (DirectoryNotFoundException e)
				{
					Logger.LogDebug($"Directory was not found: {e}");

					// Directory does not exist. So the operation is trivially done.
					return true;
				}
				catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
				{
					await Task.Delay(millisecondsDelay).ConfigureAwait(false);
				}
			}

			return false;
		}

		public static async Task BetterExtractZipToDirectoryAsync(string src, string dest)
		{
			try
			{
				ZipFile.ExtractToDirectory(src, dest);
			}
			catch (UnauthorizedAccessException)
			{
				await Task.Delay(100);
				ZipFile.ExtractToDirectory(src, dest);
			}
		}

		public static void EnsureContainingDirectoryExists(string fileNameOrPath)
		{
			string fullPath = Path.GetFullPath(fileNameOrPath); // No matter if relative or absolute path is given to this.
			string? dir = Path.GetDirectoryName(fullPath);
			EnsureDirectoryExists(dir);
		}

		/// <summary>
		/// It's like Directory.CreateDirectory, but does not fail when root is given.
		/// </summary>
		public static void EnsureDirectoryExists(string? dir)
		{
			if (!string.IsNullOrWhiteSpace(dir)) // If root is given, then do not worry.
			{
				Directory.CreateDirectory(dir); // It does not fail if it exists.
			}
		}

		public static void EnsureFileExists(string filePath)
		{
			if (!File.Exists(filePath))
			{
				EnsureContainingDirectoryExists(filePath);

				File.Create(filePath)?.Dispose();
			}
		}

		public static byte[] GetHashFile(string filePath)
		{
			var bytes = File.ReadAllBytes(filePath);
			return HashHelpers.GenerateSha256Hash(bytes);
		}

		public static bool CheckExpectedHash(string filePath, string sourceFolderPath)
		{
			var fileHash = GetHashFile(filePath);
			try
			{
				var digests = File.ReadAllLines(Path.Combine(sourceFolderPath, "digests.txt"));
				foreach (var digest in digests)
				{
					var expectedHash = ByteHelpers.FromHex(digest);
					if (ByteHelpers.CompareFastUnsafe(fileHash, expectedHash))
					{
						return true;
					}
				}
				return false;
			}
			catch
			{
				return false;
			}
		}

		public static void OpenFolderInFileExplorer(string dirPath)
		{
			if (Directory.Exists(dirPath))
			{
				using Process process = Process.Start(new ProcessStartInfo
				{
					FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
						? "explorer.exe"
						: (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
							? "open"
							: "xdg-open"),
					Arguments = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? $"\"{dirPath}\"" : dirPath,
					CreateNoWindow = true
				});
			}
		}

		public static async Task OpenBrowserAsync(string url)
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				// If no associated application/json MimeType is found xdg-open opens retrun error
				// but it tries to open it anyway using the console editor (nano, vim, other..)
				await EnvironmentHelpers.ShellExecAsync($"xdg-open {url}", waitForExit: false).ConfigureAwait(false);
			}
			else
			{
				using Process process = Process.Start(new ProcessStartInfo
				{
					FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? url : "open",
					Arguments = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? $"-e {url}" : "",
					CreateNoWindow = true,
					UseShellExecute = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
				});
			}
		}

		public static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
		{
			foreach (DirectoryInfo dir in source.GetDirectories())
			{
				CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
			}

			foreach (FileInfo file in source.GetFiles())
			{
				file.CopyTo(Path.Combine(target.FullName, file.Name));
			}
		}
	}
}
