using System;
using System.IO;

namespace EAS_Decoder {
	class Program {
		static void Main(string[] args) {
			//ProcessStartInfo startInfo = new ProcessStartInfo {
			//	FileName = "sox/sox.exe",
			//	Arguments = @"-V2 -V2 -t wav .\output.wav -t raw -esigned-integer -b16 -r 22050 .\output.raw remix 1",
			//	WindowStyle = ProcessWindowStyle.Hidden,
			//	UseShellExecute = false,
			//	CreateNoWindow = false,
			//	WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
			//};
			//using (Process soxProcess = Process.Start(startInfo)) {
			//	soxProcess.WaitForExit();
			//}

			string soxDirectory = null;

			foreach (DriveInfo drive in DriveInfo.GetDrives()) {
				string programFilesDirectory = $@"{drive.Name}Program Files (x86)";
				if (Directory.Exists(programFilesDirectory)) {
					string[] programFilesFolders = Directory.GetDirectories(programFilesDirectory);
					foreach (string programFilesFolder in programFilesFolders) {
						if (programFilesFolder.Contains("sox")) {
							
						}
					}
				}
			}
			Console.WriteLine("DONE");
			Console.ReadKey();
		}
	}
}
