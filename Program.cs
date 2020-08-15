using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace EAS_Decoder {
	class Program {
		static void DisplayHelp() {
			Console.WriteLine("\nUsage:\n    EASDecoder [args]\n\n" +
						"Arguments:\n" +
						"    -s [DIRECTORY]: Directory to sox.exe (not required if sox is installed in its default location on any drive)\n" +
						"    -i or --input [FILEPATH]: Input file to analyze\n" +
						"    -t or --type [TYPE]: The type of the input file (assumed to be .raw if not specified)\n" +
						"    -o or --output [FILEPATH]: Output file to convert input file to raw using sox\n" +
						"    -r or --record: Saves a snippet of any alerts that this program reads in using parameters from the input file\n" +
						"                    Does not work if the input file is already raw\n" +
						"    -h or --help: Displays this help page\n" +
						"\nThis program currently does not support livestreaming audio yet. All URLs will be rejected for now.");
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

		static void Main(string[] args) {
			string soxDirectory = null;
			string inputFileDirectory = null;
			string inputFileType = "raw";
			string outputFileDirectory = null;
			bool recordOnEAS = false;

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
				} else if (args[i] == "-r" || args[i] == "--record") {
					Console.WriteLine("-r is currently not supported. Unspecified behavior may occur.");
					recordOnEAS = true;
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

				int soxExitCode;
				if (recordOnEAS) {
					soxExitCode = Sox.GetFileInformation(inputFileDirectory);
					DidSoxFail(soxExitCode);
				}

				soxExitCode = Sox.ConvertAndDecode(inputFileType, inputFileDirectory, outputFileDirectory);
				DidSoxFail(soxExitCode);
			} else {
				Decode.DecodeFromFile(inputFileDirectory);
			}
		}
	}
}
