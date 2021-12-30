using System;
using System.IO;
using System.Reflection;
using TrrntZip;
using Directory = RVIO.Directory;
using DirectoryInfo = RVIO.DirectoryInfo;
using FileInfo = RVIO.FileInfo;
using Path = RVIO.Path;


namespace TrrntZipCMD {
	internal class Program {
		private static bool _noRecursion;
		private static bool _guiLaunch;

		private static TorrentZip tz;

		private static StreamWriter logStream = null;

		private static void Main(string[] args) {
			try {

				if (args.Length == 0) {
					Console.WriteLine("");
					Console.WriteLine("trrntzip: missing path");
					Console.WriteLine("Usage: trrntzip [OPTIONS] [PATH/ZIP FILES]");
					return;
				}

				for (var i = 0; i < args.Length; i++) {
					var arg = args[i];
					if (arg.Length < 2) {
						continue;
					}
					if (arg[..1] != "-") {
						continue;
					}

					switch (arg.Substring(1, 1)) {
						case "?":
							Console.WriteLine($"TorrentZip.Net v{Assembly.GetExecutingAssembly().GetName().Version.ToString(3)} - Powered by RomVault");
							Console.WriteLine("");
							Console.WriteLine("Copyright (C) 2021 GordonJ");
							Console.WriteLine("Homepage : http://www.romvault.com/trrntzip");
							Console.WriteLine("");
							Console.WriteLine("Usage: trrntzip [OPTIONS] [PATH/ZIP FILE]");
							Console.WriteLine("");
							Console.WriteLine("Options:");
							Console.WriteLine("");
							Console.WriteLine("-? : show this help");
							Console.WriteLine("-s : prevent sub-directory recursion");
							Console.WriteLine("-f : force re-zip");
							Console.WriteLine("-c : Check files only do not repair");
							Console.WriteLine("-l : verbose logging");
							Console.WriteLine("-v : show version");
							Console.WriteLine("-g : pause when finished");
							return;
						case "s":
							_noRecursion = true;
							break;
						case "f":
							TrrntZip.Program.ForceReZip = true;
							break;
						case "c":
							TrrntZip.Program.CheckOnly = true;
							break;
						case "l":
							TrrntZip.Program.VerboseLogging = true;
							var logtime = DateTime.Now.ToString("yyyy-MM-dd-HHmmss");
							logStream = new StreamWriter($"outlog-{logtime}.txt");
							break;
						case "v":
							Console.WriteLine("TorrentZip v{0}", Assembly.GetExecutingAssembly().GetName().Version);
							return;
						case "g":
							_guiLaunch = true;
							break;
					}
				}

				tz = new TorrentZip {
					StatusCallBack = StatusCallBack,
					StatusLogCallBack = StatusLogCallBack
				};

				foreach (var tArg in args) {
					var arg = tArg;
					if (arg.Length < 2) {
						continue;
					}
					if (arg[..1] == "-") {
						continue;
					}

					if (arg.Length > 2 && arg[..2] == ".\\") {
						arg = arg[2..];
					}
					// first check if arg is a directory
					if (Directory.Exists(arg)) {
						ProcessDir(arg);
						continue;
					}

					// now check if arg is a directory/filename with possible wild cards.

					var dir = Path.GetDirectoryName(arg);
					if (string.IsNullOrEmpty(dir)) {
						dir = Environment.CurrentDirectory;
					}

					var filename = Path.GetFileName(arg);

					var dirInfo = new DirectoryInfo(dir);
					var fileInfo = dirInfo.GetFiles(filename);
					foreach (var file in fileInfo) {
						var ext = Path.GetExtension(file.FullName).ToLower();
						if (!string.IsNullOrEmpty(ext) && ((ext == ".zip") || (ext == ".7z"))) {
							tz.Process(new FileInfo(file.FullName));
						}
					}
				}

				logStream?.Flush();
				logStream?.Close();
				logStream?.Dispose();
				logStream = null;

				if (_guiLaunch) {
					Console.WriteLine("Complete.");
					Console.ReadLine();
				}
			} catch (Exception e) {
				Console.WriteLine("{0} Exception caught.", e);
				logStream?.WriteLine("{0} Exception caught.", e);
				logStream?.Flush();
				logStream?.Close();
				logStream?.Dispose();
				logStream = null;
			}
		}

		private static void ProcessDir(string dirName) {
			Console.WriteLine("Checking Dir : " + dirName);

			var di = new DirectoryInfo(dirName);
			var fi = di.GetFiles();
			foreach (var f in fi) {
				var filename = f.FullName;
				var ext = Path.GetExtension(filename)?.ToLower();
				if (!string.IsNullOrEmpty(ext) && (ext == ".zip" || ext == ".7z")) {
					tz.Process(new FileInfo(filename));
				}
			}

			if (_noRecursion) {
				return;
			}

			var directories = System.IO.Directory.GetDirectories(dirName);
			foreach (var dir in directories) {
				ProcessDir(dir);
			}
		}


		private static void StatusCallBack(int processID, int percent) => Console.Write($"{percent,3}%");

		private static void StatusLogCallBack(int processId, string log) {
			logStream?.WriteLine(log);
			Console.WriteLine(log);
		}
	}
}
