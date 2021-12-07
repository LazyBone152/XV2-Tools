using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AutoUpdater
{
	//Inspired by https://www.codeproject.com/Articles/5165015/AutoUpdate-A-GitHub-Enabled-autoupdater

	public static class Update
    {
        private const string GITHUB_URL = @"https://github.com/";
        public static string GITHUB_ACCOUNT = "LazyBone152";
        public static string GITHUB_REPO = "Dummy";
		public static string APP_TAG = "EEPK";
		private static string FULL_REPO_PATH { get { return $"{GITHUB_ACCOUNT}/{GITHUB_REPO}"; } }
		public static string DEFAULT_APP_NAME = "";

		//Update failed
		public static bool IsDownloadSuccessful = false;
		public static string DownloadFailedText = "";

		private static List<AppVersion> AvailableVersions = new List<AppVersion>();
        

		private static void FetchAllVersions()
		{
			AvailableVersions.Clear();

			string pattern =
					string.Concat(
						Regex.Escape(FULL_REPO_PATH),
						$@"\/releases\/download\/{APP_TAG}.v[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+.*\.zip");

			Regex urlMatcher = new Regex(pattern, RegexOptions.CultureInvariant | RegexOptions.Compiled);
			var result = new Dictionary<Version, Uri>();
			WebRequest wrq = WebRequest.Create(string.Concat(GITHUB_URL, FULL_REPO_PATH, "/releases/latest"));
			WebResponse wrs = null;
			try
			{
				wrs = wrq.GetResponse();
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Error fetching repo: " + ex.Message);
				return;
			}
			using (var sr = new StreamReader(wrs.GetResponseStream()))
			{
				string line;
				while (null != (line = sr.ReadLine()))
				{
					var match = urlMatcher.Match(line);
					if (match.Success)
					{
						string path = string.Concat(GITHUB_URL, match.Value);
						var uri = new Uri(path);
						var changelogUri = new Uri(GetChangelogPath(path));
                        try
						{
							string[] split = uri.ToString().Split('v');
							string[] ver = split[split.Length - 1].Split('.');

							AppVersion version = new AppVersion(uri, new Version($"{ver[0]}.{ver[1]}.{ver[2]}.{ver[3]}"), changelogUri);

							if (!AvailableVersions.Contains(version))
								AvailableVersions.Add(version);
						}
                        catch
                        {
                        }
					}
				}
			}
		}
	
		/// <summary>
		/// Check for an update for the current application, using the supplied GitHub settings.
		/// </summary>
		/// <returns>[0] = isUpdateAvailable (bool), [1] = latestVersion (Version), [2] = changelog (string, [3] = number of updates)</returns>
		public static async Task<object[]> CheckForUpdate()
		{
			FetchAllVersions();

			Version latestVersion;
			Version currentVersion = Assembly.GetEntryAssembly().GetName().Version;
			Version maxVersion = AvailableVersions.Max(x => x.Version);
			int numberUpdates = 0;

			//Download changelog
			string changelog = await DownloadChangelog();
			StringBuilder newChangelog = new StringBuilder();

            if (string.IsNullOrWhiteSpace(changelog))
            {
				changelog = "Failed to download.";
				numberUpdates = 1;
            }
            else
			{
				//Trim all versions lesser or equal to current
				using (StringReader reader = new StringReader(changelog))
				{
					string line;
					while ((line = reader.ReadLine()) != null)
					{
						if (line.Length > 0)
						{
							Version ver;
							if (Version.TryParse(line.TrimEnd(':', ' '), out ver))
							{
								if (ver <= currentVersion)
								{
									break;
								}

								//New version section
								//Append new line if not the first area
								if (newChangelog.Length > 0)
									newChangelog.AppendLine();

								newChangelog.AppendLine(line);
								numberUpdates++;
							}
							else if (!string.IsNullOrWhiteSpace(line))
							{
								newChangelog.AppendLine(line);
							}

						}
					}
				}
			}

			if (maxVersion > currentVersion)
            {
				latestVersion = maxVersion;
				return new object[4] { true, latestVersion, newChangelog.ToString(), numberUpdates };
            }

			latestVersion = currentVersion;
			return new object[4] { false, latestVersion, newChangelog.ToString(), 0 };
		}

		/// <summary>
		/// Download the latest version and returns the path to it.\n\n
		/// NOTE: Always downloads the latest version published on GitHub, regardless of the current assembly version! (Use CheckForUpdate for this)
		/// </summary>
		public static string DownloadUpdate()
		{
			if (AvailableVersions.Count == 0)
				FetchAllVersions();

			Version maxVersion = AvailableVersions.Max(x => x.Version);
			AppVersion update = AvailableVersions.FirstOrDefault(x => x.Version == maxVersion);

			//Get the temp file path and ensure the directory exists
			string localPath = GetUpdateFilePath();
			Directory.CreateDirectory(Path.GetDirectoryName(localPath));

			//Download file
			try
			{
				using (var client = new WebClient())
				{
					client.DownloadFile(update.Download, localPath);
				}
			}
			catch (WebException ex)
			{
				IsDownloadSuccessful = false;
				DownloadFailedText = $"Web exception: {ex.Message}\n" +
					$"Response: {ex.Response}\n" +
					$"Status: {ex.Status}\n\n" +
					$"Update download failed.";
				return "";
			}
			catch (Exception ex)
			{
				IsDownloadSuccessful = false;
				DownloadFailedText = $"Exception: {ex.Message}\n" +
					$"Source: {ex.Source}\n\n" +
					$"Update download failed.";
				return "";
			}

			IsDownloadSuccessful = true;
			return localPath;
		}

		/// <summary>
		/// Download and install the latest version. (Use CheckForUpdate first to ensure the current version is not the latest!)\n\n
		/// NOTE: THIS WILL CLOSE THE CURRENT PROCESS AND START THE UPDATER.
		/// </summary>
		public static void UpdateApplication()
        {
			//Extract bootstrapper
			byte[] bootstrapperBytes = Properties.Resources.UpdateBootstrapper;
			string bootstrapperPath = GetBootstrapperPath();

			File.WriteAllBytes(bootstrapperPath, bootstrapperBytes);

			//Arguments
			string updateZipPath = GetUpdateFilePath();
			string applicationFolderPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
			string applicationExePath = Assembly.GetEntryAssembly().Location;
			int callingProcessId = Process.GetCurrentProcess().Id;
			string processName = Process.GetCurrentProcess().ProcessName;
			string defaultAppName = (string.IsNullOrWhiteSpace(DEFAULT_APP_NAME)) ? Path.GetFileName(applicationExePath) : DEFAULT_APP_NAME;

			//Start bootstraper
			ProcessStartInfo processInfo = new ProcessStartInfo(bootstrapperPath, $"\"{updateZipPath}\" \"{applicationFolderPath}\" \"{applicationExePath}\" \"{callingProcessId}\" \"{processName}\" \"{defaultAppName}\"");
			processInfo.Verb = "runas"; //Run process with admin rights - UAC will prompt user if enabled
			Process.Start(processInfo);

			//Close this process
			Environment.Exit(0);
		}

		public static void DeleteBootstraper()
        {
			try
			{
				File.Delete(GetBootstrapperPath());
			}
			catch { }
		}

		private static string GetUpdateFilePath()
        {
			return Path.GetFullPath(String.Format("{0}/LBAutoUpdater/{1}/update.zip", Path.GetTempPath(), APP_TAG));
		}

		private static string GetBootstrapperPath()
		{
			return Path.GetFullPath(String.Format("{0}/LBAutoUpdater/{1}/AutoUpdater.exe", Path.GetTempPath(), APP_TAG));
			//return $"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}/{APP_TAG}_AutoUpdater.exe";
		}

		private static async Task<string> DownloadChangelog()
		{
			Version maxVersion = AvailableVersions.Max(x => x.Version);
			AppVersion update = AvailableVersions.FirstOrDefault(x => x.Version == maxVersion);

			string ret = null;

			try
			{
				using (var client = new WebClient())
				{
					ret = client.DownloadString(update.ChangelogDownload);
				}
			}
			catch
			{
				return "";
			}

			return ret;
		}
	
		private static string GetChangelogPath(string zipPath)
        {
			for (int i = zipPath.Length - 1; i >= 0; i--)
            {
				if(zipPath[i] == '/')
                {
					return $"{zipPath.Remove(i + 1, zipPath.Length - (i + 1))}/changelog.txt";

				}
            }

			return null;
		}
	}

	public struct AppVersion
    {
		public Uri ChangelogDownload;
		public Uri Download;
		public Version Version;

		public AppVersion(Uri uri, Version version, Uri changelogUri)
        {
			Download = uri;
			Version = version;
			ChangelogDownload = changelogUri;
        }
    }
}
