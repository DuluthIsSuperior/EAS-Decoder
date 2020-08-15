using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace EAS_Decoder {
	class Program {
		static void DisplayHelp() {
			Console.Write("EASDecoder [args]\n\n" +
						"Arguemnts:\n" +
						"    -s [DIRECTORY]: Directory to sox.exe (not required if sox is installed in its default location on any drive)\n" +
						"    -i or --input [FILEPATH]: Input file to analyze\n" +
						"    -t or --type [TYPE]: The type of the input file (assumed to be .raw if not specified)\n" +
						"    -o or --output [FILEPATH]: Output file to convert input file to raw using sox\n" +
						"    -l or --livestream: Indicates that your input file is a livestream\n" +
						"    -h or --help: Displays this help page\n");
		}
		static void Main(string[] args) {
			string soxDirectory = null;
			string inputFileDirectory = null;
			string inputFileType = "raw";
			string outputFileDirectory = null;
			bool isLivestream = false;

			for (int i = 0; i < args.Length; i++) {
				if (args[i] == "-s") {
					i++;
					if (File.Exists(args[i])) {
						soxDirectory = args[i];
					} else {
						Console.WriteLine("Could not find sox.exe at the given directory");
					}
				} else if (args[i] == "-i" || args[i] == "--input-file" || args[i] == "--input") {
					i++;
					if (File.Exists(args[i])) {
						inputFileDirectory = args[i];
					} else {
						Console.WriteLine($"Could not open the file at the given directory: {args[i]}");
						DisplayHelp();
						Environment.Exit(4);
					}
				} else if (args[i] == "-t" || args[i] == "--type") {
					i++;
					inputFileType = args[i];
				} else if (args[i] == "-l") {
					isLivestream = true;
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
								Console.WriteLine();
								break;
							}
						}
					}
					outputFileDirectory = args[i];
				} else if (args[i] == "-h" || args[i] == "--help") {
					DisplayHelp();
					Environment.Exit(0);
				} else {
					Console.WriteLine($"Invalid argument: {args[i]}");
					Environment.Exit(2);
				}
			}

			if (inputFileDirectory == null) {
				Console.WriteLine("An input file path is required");
				DisplayHelp();
				Environment.Exit(5);
			}

			if (inputFileType != "raw") {
				Sox.GetSoxProcess(soxDirectory);
				int soxExitCode = Sox.ConvertAndDecode(inputFileType, inputFileDirectory, outputFileDirectory);
				if (soxExitCode < 0) {
					if (soxExitCode == -1) {
						Console.WriteLine("\nSox could not open your input file due to the MAD library not being able to load.\n" +
							"You may need to add the files 'libmad-0.dll' and 'libmp3lame-0.dll' to the directory sox is installed at.");
					} else if (soxExitCode == -2) {
						Console.WriteLine("\nSox was unable to open your input file. Please make sure your file is a valid audio file.\n" +
							"Please make sure that the file type given to the '-t' flag matches your input file type.");
					}
					Environment.Exit(8);
				}
			} else {
				Decode.DecodeFromFile(inputFileDirectory);
			}
		}
	}
}
