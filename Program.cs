using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;

namespace EAS_Decoder {
	class Program {
		static void Main(string[] args) {
			string soxDirectory = null;

			for (int i = 0; i < args.Length; i++) {
				if (args[i] == "-s") {
					string possibleSoxDirectory = args[i + 1];
					if (File.Exists(possibleSoxDirectory)) {
						soxDirectory = args[i + 1];
						i++;
					} else {
						Console.WriteLine("Could not find sox.exe at the given directory");
					}
				} else if (args[i] == "-h" || args[i] == "--help") {
					Console.Write("EASDecoder [args]\n\n" +
						"Arguemnts:\n" +
						"-s [DIRECTORY]: Directory to sox.exe (not required if sox is installed in its default location on any drive)\n" +
						"-h or --help: Displays this help page\n");
					Environment.Exit(0);
				} else {
					Console.WriteLine($"Invalid argument: {args[i]}");
					Environment.Exit(2);
				}
			}

			foreach (DriveInfo drive in DriveInfo.GetDrives()) {
				string programFilesDirectory = $@"{drive.Name}Program Files (x86)";
				if (Directory.Exists(programFilesDirectory)) {
					string[] programFilesFolders = Directory.GetDirectories(programFilesDirectory);
					foreach (string programFilesFolder in programFilesFolders) {
						if (programFilesFolder.Contains("sox")) {
							string possibleSoxDirectory = $@"{programFilesFolder}\sox.exe";
							if (File.Exists(possibleSoxDirectory)) {
								soxDirectory = possibleSoxDirectory;
								break;
							}
						}
					}
					if (soxDirectory != null) {
						break;
					}
				}
			}

			if (soxDirectory == null) {
				Console.WriteLine("Could not find sox.exe. This program is required to demodulate audio files. If you have it installed outside of your " +
					"Program Files (x86) folder, please use the -s <DIRECTORY> flag to link the sox executable");
				Environment.Exit(1);
			}

			ProcessStartInfo startInfo = new ProcessStartInfo {
				FileName = soxDirectory,
				//Arguments = @"-V2 -V2 -t wav .\output.wav -t raw -esigned-integer -b16 -r 22050 .\output.raw remix 1",
				Arguments = "--version",
				WindowStyle = ProcessWindowStyle.Hidden,
				UseShellExecute = false,
				CreateNoWindow = false,
				WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
				RedirectStandardOutput = true

			};
			bool isSox = false;
			using (Process soxProcess = Process.Start(startInfo)) {
				while (!soxProcess.StandardOutput.EndOfStream) {
					string line = soxProcess.StandardOutput.ReadLine();
					if (line.Contains("SoX")) {
						isSox = true;
					}
				}
				soxProcess.WaitForExit();
			}

			if (!isSox) {
				Console.WriteLine("Invalid path to SoX or non-SoX executable launched");
				Environment.Exit(3);
			}



			Console.WriteLine("DONE");
			Console.ReadKey();
		}
	}
}
