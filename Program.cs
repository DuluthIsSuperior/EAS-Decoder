using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace EAS_Decoder {
	class Program {
		public static bool Livestream {
			get;
			private set;
		}
		public static JObject CountyCodes {
			get;
			private set;
		}
		static void DisplayHelp() {
			Console.WriteLine("\nUsage:\n    EASDecoder [args]\n\n" +
						"Arguments:\n" +
						" *  -s or --sox [FILEPATH]: Directory to sox.exe\n" +
						" *  -f or --ffmpeg [FILEPATH]: Directory to ffmpeg.exe\n" +
						"    -i or --input [FILEPATH]: Input file to analyze\n" +
						"    -t or --type [TYPE]: The type of the input file (assumed to be .raw if not specified)\n" +
						"    -o or --output [FILEPATH]: Output file to convert input file to raw using sox\n" +
						"    -r or --record: Saves recordings of EAS alerts that this program reads in using parameters from the input file\n" +
						"    -u or --update: Attempts to update the local copy of county and event/alert codes\n" +
						"                    Does not work if the input file is already raw\n" +
						"    -u or --update: Attempts to update the local copy of county and event/alert codes\n" +
						"    -h or --help: Displays this help page\n\n" +
						"For more information on flags bulleted with an asterisk, type in the flag followed by -h or --help\n" +
						"e.g. More information about -s can be displayed by typing \"EASDecoder -s -h\"");
		}

		static void DidSoxFail(int soxExitCode) {
			if (soxExitCode < 0) {
				if (soxExitCode == -1) {
					Console.WriteLine("\nSox could not open your input file due to the MAD library not being able to load.\n" +
						"You may need to add the files 'libmad-0.dll' and 'libmp3lame-0.dll' to the directory sox is installed at.");
				} else if (soxExitCode == -2) {
					Console.WriteLine("\nSox was unable to open your input file. Please make sure your file is a valid audio file.\n" +
						"Please make sure that the file type given to the '-t' flag matches your input file type.");
				} else if (soxExitCode == -3) {
					Console.WriteLine("\nWe could not read the sample rate of your input file from Sox. Please make sure your audio file is valid.");
				}
				Environment.Exit(8);
			}
		}

		static bool ValidURL(string url) {
			try {
				HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
				request.Method = "GET";
				request.Timeout = 5000;
				using (HttpWebResponse response = request.GetResponse() as HttpWebResponse) {
					int statusCode = (int) response.StatusCode;
					if (statusCode >= 100 && statusCode < 400) {
						return true;
					} else if (statusCode >= 500 && statusCode <= 510) {
						Console.WriteLine($"error: URL exists, but a {statusCode} was returned - connection failed");
						return false;
					}
					return false;
				}
			} catch {
				return false;
			}
		}

		static void GetSAMECodesFromInternet() {
			Console.Write("Downloading SAME Codes... ");
			JObject SAMECodes = new JObject();
			string URL = "https://www.weather.gov/source/nwr/SameCode.txt";
			HttpWebRequest request = (HttpWebRequest) WebRequest.Create(URL);
			request.Headers.Add(HttpRequestHeader.UserAgent, "blah");
			HttpWebResponse response;
			try {
				using (response = (HttpWebResponse) request.GetResponse()) {
					using (StreamReader stream = new StreamReader(response.GetResponseStream())) {
						while (!stream.EndOfStream) {
							string line = stream.ReadLine();
							string SAMECode = line[0..6];
							string location = line[7..];
							SAMECodes[SAMECode] = location;
						}
					}
				}

				URL = "https://www.weather.gov/source/gis/Shapefiles/WSOM/marnwr05de17.txt";    // consult https://www.weather.gov/marine/wxradio if a different URL is needed
				request = (HttpWebRequest) WebRequest.Create(URL);
				request.Headers.Add(HttpRequestHeader.UserAgent, "blah");
				using (response = (HttpWebResponse) request.GetResponse()) {
					using (StreamReader stream = new StreamReader(response.GetResponseStream())) {
						while (!stream.EndOfStream) {
							string[] line = stream.ReadLine().Split('|');
							string sameCode = $"0{line[1]}";
							string zoneName = line[2];
							if (zoneName.Contains("Synopsis")) {
								continue;
							}
							SAMECodes[sameCode] = $"{zoneName}, {line[0]}";
						}
					}
				}

				File.WriteAllText("SAMECodes.json", SAMECodes.ToString());
			} catch (WebException e) {
				response = (HttpWebResponse) e.Response;
				Console.WriteLine($"Could not update SAME county codes - a {response.StatusCode} error was returned accessing {URL}");
			}
			Console.WriteLine("Done");
		}

		static void Main(string[] args) {
			string inputFileDirectory = null;
			string inputFileType = "raw";
			string outputFileDirectory = null;
			bool recordOnEAS = false;

			for (int i = 0; i < args.Length; i++) {
				if (args[i] == "-s" || args[i] == "--sox") {
					i++;
					if (File.Exists(args[i])) {
						ProcessManager.SoxDirectory = args[i];
					} else if (args[i] == "-h" || args[i] == "--help") {
						Console.WriteLine("\nUsage:\n    EASDecoder [-s or --sox] [DIRECTORY]\n\n" +
							"The program sox is required to convert the incoming audio file into raw data for this program to decode EAS tones.\n" +
							"If sox is not added to Path in the Environment Variables, then this flag with a valid path to sox.exe is required.\n\n" +
							"DIRECTORY: Directory to sox.exe");
						Environment.Exit(0);
					} else {
						Console.WriteLine("error -s: Could not find: {args[i]}\n");
						Environment.Exit(13);
					}
				} else if (args[i] == "-f" || args[i] == "--ffmpeg") {
					i++;
					if (File.Exists(args[i])) {
						ProcessManager.FfmpegDirectory = args[i];
					} else if (args[i] == "-h" || args[i] == "--help") {
						Console.WriteLine("\nUsage:\n    EASDecoder [-f or --ffmpeg] [DIRECTORY]\n\n" +
							"The program ffmpeg is required to dump the incoming audio file's data for this program to decode EAS tones.\n" +
							"It also allows streaming audio to be used and monitored in real time.\n" +
							"If ffmpeg isn't added to Path in the Environment Variables, then this flag with a valid path to ffmpeg.exe is required.\n\n" +
							"DIRECTORY: Directory to ffmpeg.exe");
						Environment.Exit(0);
					} else {
						Console.WriteLine($"error -f: Could not find: {args[i]}\n");
						Environment.Exit(14);
					}
				} else if (args[i] == "-i" || args[i] == "--input-file" || args[i] == "--input") {
					i++;
					if (File.Exists(args[i])) {
						inputFileDirectory = args[i];
					} else {
						if (!ValidURL(args[i])) {
							Console.WriteLine($"Could not open file or URL: {args[i]}");
							Console.WriteLine("If your file path contains spaces, please put it in quotation marks");
							Environment.Exit(4);
						}
						Livestream = true;
						inputFileDirectory = args[i];
						Console.WriteLine($"info: Successfully pinged {inputFileDirectory}");
					}
				} else if (args[i] == "-u" || args[i] == "--update") {
					GetSAMECodesFromInternet();
				} else if (args[i] == "-t" || args[i] == "--type") {
					i++;
					inputFileType = args[i];
				} else if (args[i] == "-r" || args[i] == "--record") {
					if (inputFileType == "raw") {
						Console.WriteLine("Recording on alert is not supported when the input file is raw");
					} else {
						recordOnEAS = true;
					}
				} else if (args[i] == "-o" || args[i] == "--output" || args[i] == "--output-file") {
					i++;
					if (File.Exists(args[i])) {
						while (true) {
							Console.Write($"warn: File already exists at {args[i]}. File will be overwritten! Continue (Y/N)? ");
							string response = Console.ReadKey().KeyChar.ToString().ToUpper();
							if (response == "N") {
								Console.WriteLine();
								Environment.Exit(0);
							} else if (response == "Y") {
								Console.Write("\nAre you sure (Y/N)? ");
								response = Console.ReadKey().KeyChar.ToString().ToUpper();
								if (response != "Y") {
									Console.WriteLine("Operation aborted");
									Environment.Exit(0);
								}
								Console.WriteLine();
								break;
							}
						}
					}
					if (inputFileType == null) {
						Console.WriteLine("Please specify the input file's type before using the output flag");
						Environment.Exit(0);
					}
					string outputPath = $"{args[i]}.{inputFileType}.raw";
					Console.WriteLine($"Output file will be written to {outputPath}");
					outputFileDirectory = outputPath;
				} else if (args[i] == "-u" || args[i] == "--update") {
					GetSAMECodesFromInternet();
				} else if (args[i] == "-h" || args[i] == "--help") {
					DisplayHelp();
					Environment.Exit(0);
				} else {
					Console.WriteLine($"Invalid argument: {args[i]}");
					Environment.Exit(2);
				}
			}

			try {
				string SAMEJson = File.ReadAllText("SAMECodes.json");
				CountyCodes = JObject.Parse(SAMEJson);
			} catch (Exception) {
				Console.WriteLine("There was a problem loading in the database of SAME county codes.\nPlease run this program next time using the '-u' flag.");
			}
			if (inputFileDirectory != null) {
				if (inputFileType != "raw") {
					Console.WriteLine($"info: Monitoring {inputFileDirectory}");
					int soxExitCode = ProcessManager.GetFileInformation(inputFileDirectory, recordOnEAS);
					DidSoxFail(soxExitCode);

					soxExitCode = ProcessManager.ConvertAndDecode(inputFileDirectory, inputFileType, outputFileDirectory);
					DidSoxFail(soxExitCode);
				} else {
					Decode.DecodeFromFile(inputFileDirectory);
				}
			} else {
				Console.WriteLine("An input file or a streaming audio file is required");
			}
		}
	}
}
