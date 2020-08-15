using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;

namespace EAS_Decoder {
	public class FixedSizeQueue<T> : ConcurrentQueue<T> {
		private readonly object syncObject = new object();

		public int Size { get; private set; }

		public FixedSizeQueue(int size) {
			Size = size;
		}

		public new void Enqueue(T obj) {
			base.Enqueue(obj);
			while (base.Count > Size) {
				T outObj;
				base.TryDequeue(out outObj);
			}
		}
	}
	static class Sox {
		static string soxDirectory = null;

		static ProcessStartInfo GetSoxStartInfo(string args, bool redirectStdout, bool redirectStderr) {
			return new ProcessStartInfo {
				FileName = soxDirectory,
				Arguments = args,
				WindowStyle = ProcessWindowStyle.Hidden,
				UseShellExecute = false,
				CreateNoWindow = false,
				WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
				RedirectStandardOutput = redirectStdout,
				RedirectStandardError = redirectStderr
			};
		}

		static int FailedToLoad(StreamReader stderr) {
			bool MADFailedToLoad = false;
			bool fileFailedToOpen = false;

			string line = stderr.ReadLine();
			while (line != null) {
				if (line.Contains("Unable to load MAD decoder library")) {
					MADFailedToLoad = true;
				}
				if (line.Contains("can't open input file")) {
					fileFailedToOpen = true;
				}
				line = stderr.ReadLine();
			}

			return MADFailedToLoad ? -1 : fileFailedToOpen ? -2 : 0;
		}

		public static string GetSoxProcess(string possibleDirectory) {
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

			ProcessStartInfo startInfo = GetSoxStartInfo("--version", true, false);
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

		static int samplerate = -1;
		static FixedSizeQueue<byte> buffer5Seconds = null;
		static bool isRecording = false;
		static MemoryStream ms = null;
		public static int ConvertAndDecode(string type, string inputFile, string outputFile) {
			if (soxDirectory == null) {
				Console.WriteLine("error: Internal Error");
				Environment.Exit(7);
			}
			ProcessStartInfo startInfo = GetSoxStartInfo($@"-V2 -V2 -t {type} {inputFile} -t raw -esigned-integer -b16 -r 22050 - remix 1", true, true);
			FileStream fs = null;
			if (outputFile != null) {
				File.WriteAllText(outputFile, string.Empty);
				fs = new FileStream(outputFile, FileMode.OpenOrCreate);
			}

			int didNotLoad;
			using (Process soxProcess = Process.Start(startInfo)) {
				FileStream baseStream = (FileStream) soxProcess.StandardOutput.BaseStream;
				int lastRead = 0;
				do {	// sox will not produce anything on stdout if an error occurs
					byte[] buffer = new byte[16384];
					lastRead = baseStream.Read(buffer, 0, buffer.Length);
					bool record = false;
					if (lastRead > 0) {
						record = Decode.DecodeEAS(buffer, lastRead);
						if (fs != null) {
							fs.Write(buffer, 0, lastRead);
						}
					}

					if (buffer5Seconds != null) {
						if (!isRecording) {
							for (int i = 0; i < lastRead; i++) {
								buffer5Seconds.Enqueue(buffer[i]);
							}
						}
						if (record && !isRecording) {
							ms = new MemoryStream();
							while (!buffer5Seconds.IsEmpty) {
								byte b;
								if (buffer5Seconds.TryDequeue(out b)) {
									ms.WriteByte(b);
								}
							}
							isRecording = true;
						} else if (isRecording) {
							ms.Write(buffer);
							if (!record) {
								ms.Flush();
								ms.Close();
								isRecording = false;
							}
						}
					}
				} while (lastRead > 0);

				didNotLoad = FailedToLoad(soxProcess.StandardError);

				soxProcess.WaitForExit();
			}
			return didNotLoad;
		}

		public static int GetFileInformation(string filepath) {
			ProcessStartInfo startInfo = GetSoxStartInfo($"--i {filepath}", true, true);

			int didNotLoad;
			using (Process soxProcess = Process.Start(startInfo)) {
				while (!soxProcess.StandardOutput.EndOfStream) {
					string[] line = soxProcess.StandardOutput.ReadLine().Split(' ');
					if (line[0] == "Sample" && line[1] == "Rate") {
						int.TryParse(line[^1], out samplerate);
					}
				}

				didNotLoad = FailedToLoad(soxProcess.StandardError);
				soxProcess.WaitForExit();
			}

			if (didNotLoad != 0) {
				return didNotLoad;
			}

			if (samplerate == -1) {
				Console.WriteLine("Could not read sample rate of your input file from Sox");
				return -3;
			}

			buffer5Seconds = new FixedSizeQueue<byte>(samplerate * 5);
			return 0;
		}
	}
}
