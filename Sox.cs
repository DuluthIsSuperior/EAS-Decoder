using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace EAS_Decoder {
	static class Sox {
		static public string GetSoxProcess(string possibleDirectory) {
			string soxDirectory = null;

			if (possibleDirectory == null) {
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
			} else {
				soxDirectory = possibleDirectory;
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

			return soxDirectory;
		}
	}
}
