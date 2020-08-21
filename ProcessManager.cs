﻿using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;

namespace EAS_Decoder {
	public class FixedSizeQueue<T> : ConcurrentQueue<T> {
		private readonly object syncObject = new object();

		public uint Size { get; private set; }

		public FixedSizeQueue(uint size) {
			Size = size;
		}

		public new void Enqueue(T obj) {
			base.Enqueue(obj);
			lock (syncObject) {
				while (base.Count > Size) {
					T outObj;
					base.TryDequeue(out outObj);
				}
			}
		}
	}
	static class ProcessManager {
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

		static void ConvertRAWToMP3(string filename) {
			Console.WriteLine($"Alert saved to {filename}.mp3");
			ProcessStartInfo startInfo = new ProcessStartInfo {
				FileName = "cmd",
				Arguments = $"/C \"sox -r 22050 -e signed -b 16 -t raw \"{filename}.raw\" -t mp3 \"{filename}.mp3\"\"",
				WindowStyle = ProcessWindowStyle.Hidden,
				UseShellExecute = false,
				CreateNoWindow = false,
				WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
				RedirectStandardOutput = false,
				RedirectStandardError = true
			};
			using (Process soxProcess = Process.Start(startInfo)) {
				string line = soxProcess.StandardError.ReadLine();
				while (line != null) {
					Console.WriteLine(line);
				}
				soxProcess.WaitForExit();
			}
		}

		static string AddLeadingZero(int value) {
			return $"{(value < 10 ? "0" : "")}{value}";
		}

		static void SaveEASRecording(ref FileStream easRecord, FixedSizeQueue<byte> bufferBefore, string fileName) {
			while (!bufferBefore.IsEmpty) {
				byte[] b = new byte[1];
				if (bufferBefore.TryDequeue(out b[0])) {
					easRecord.Write(b);
				}
			}

			easRecord.Close();
			easRecord = null;

			ConvertRAWToMP3(fileName);
			File.Delete($"{fileName}.raw");
		}

		public static uint samplerate;
		static DateTime fileCreated = DateTime.Now;
		static FixedSizeQueue<byte> bufferBefore = null;
		static FileStream easRecord = null;
		static readonly string[] months = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
		public static int ConvertAndDecode(string inputFile, string inputFileType, string outputFile) {
			ProcessStartInfo startInfo = new ProcessStartInfo {
				FileName = "cmd",
				Arguments = $"/C \"ffmpeg -i {inputFile} -f {inputFileType} - | sox -V2 -V2 -t {inputFileType} - -t raw -e signed-integer -b 16 -r 22050 - remix 1\"",
				WindowStyle = ProcessWindowStyle.Hidden,
				UseShellExecute = false,
				CreateNoWindow = false,
				WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
				RedirectStandardOutput = true,
				RedirectStandardError = true
			};
			FileStream fs = null;
			if (outputFile != null) {
				File.WriteAllText(outputFile, string.Empty);
				fs = new FileStream(outputFile, FileMode.OpenOrCreate);
			}

			int didNotLoad;
			using (Process soxProcess = Process.Start(startInfo)) {
				FileStream baseStream = (FileStream) soxProcess.StandardOutput.BaseStream;
				int lastRead = 0;
				bool record = false;
				string fileName = null;
				bool needToBuffer = true;

				do {	// sox nor ffmpeg will not produce anything on stdout if an error occurs
					byte[] buffer = new byte[8192];
					lastRead = baseStream.Read(buffer, 0, buffer.Length);
					Tuple<bool, uint, uint> info = null;
					if (lastRead > 0) {
						info = Decode.DecodeEAS(buffer, lastRead);
						record = info.Item1;
						if (fs != null) {
							fs.Write(buffer, 0, lastRead);
							fs.Flush();
						}
					}

					if (bufferBefore != null) {
						if (needToBuffer) {
							for (int i = 0; i < lastRead; i++) {
								bufferBefore.Enqueue(buffer[i]);
							}
							if (easRecord != null && bufferBefore.Size == bufferBefore.Count) {
								SaveEASRecording(ref easRecord, bufferBefore, fileName);
							}
						}
						if (record && easRecord == null) {
							DateTime recordingStarted = fileCreated.AddSeconds(info.Item2 / samplerate);
							string month = $"{AddLeadingZero(recordingStarted.Month)}{months[recordingStarted.Month - 1]}";
							string time = $"{AddLeadingZero(recordingStarted.Hour)}{AddLeadingZero(recordingStarted.Minute)}{AddLeadingZero(recordingStarted.Second)}";
							fileName = $"{recordingStarted.Year}_{month}_{recordingStarted.Day} - {time}";
							easRecord = new FileStream($"{fileName}.raw", FileMode.OpenOrCreate);
							while (!bufferBefore.IsEmpty) {
								byte[] b = new byte[1];
								if (bufferBefore.TryDequeue(out b[0])) {
									easRecord.Write(b);
								}
								needToBuffer = false;
							}
						} else if (record && easRecord != null) {
							if (needToBuffer) {
								while (!bufferBefore.IsEmpty) {
									byte[] b = new byte[1];
									if (bufferBefore.TryDequeue(out b[0])) {
										easRecord.Write(b);
									}
									needToBuffer = false;
								}
							} else {
								easRecord.Write(buffer, 0, lastRead);
							}
						} else if (!record && easRecord != null && !needToBuffer) {
							easRecord.Write(buffer, 0, lastRead);
							needToBuffer = true;
							bufferBefore = new FixedSizeQueue<byte>(samplerate * 5);
						}
					}
				} while (lastRead > 0);

				didNotLoad = FailedToLoad(soxProcess.StandardError);
				if (easRecord != null) {
					SaveEASRecording(ref easRecord, bufferBefore, fileName);
				}
				soxProcess.WaitForExit();
			}
			return didNotLoad;
		}

		public static int GetFileInformation(string filepath) {
			fileCreated = DateTime.Now;

			ProcessStartInfo startInfo = new ProcessStartInfo {
				FileName = "cmd",
				Arguments = $"/C \"sox --i {filepath}\"",
				WindowStyle = ProcessWindowStyle.Hidden,
				UseShellExecute = false,
				CreateNoWindow = false,
				WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
				RedirectStandardOutput = true,
				RedirectStandardError = true
			};

			int didNotLoad;
			using (Process soxProcess = Process.Start(startInfo)) {
				while (!soxProcess.StandardOutput.EndOfStream) {
					string[] line = soxProcess.StandardOutput.ReadLine().Split(' ');
					if (line[0] == "Sample" && line[1] == "Rate") {
						uint.TryParse(line[^1], out samplerate);
					}
				}

				didNotLoad = FailedToLoad(soxProcess.StandardError);
				soxProcess.WaitForExit();
			}

			if (didNotLoad != 0) {
				return didNotLoad;
			}

			if (samplerate == 0) {
				Console.WriteLine("Could not read sample rate of your input file from Sox");
				return -3;
			}

			bufferBefore = new FixedSizeQueue<byte>(samplerate * 5);
			return 0;
		}
	}
}