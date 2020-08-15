using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace EAS_Decoder {
	class Program {
		static void DisplayHelp() {
			Console.Write("EASDecoder [args]\n\n" +
						"Arguemnts:\n" +
						"    -s [DIRECTORY]: Directory to sox.exe (not required if sox is installed in its default location on any drive)\n" +
						"    -i, --input, or --input-file [FILEPATH]: Input file to analyze\n" +
						"    -t, or --type [TYPE]: The type of the input file (assumed to be .raw if not specified)\n" +
						"    -h or --help: Displays this help page\n");
		}
		static void Main(string[] args) {
			string soxDirectory = null;
			string inputFileDirectory = null;
			string inputFileType = "raw";

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
			soxDirectory = Sox.GetSoxProcess(soxDirectory);

			if (inputFileType != "raw") {
				Console.WriteLine("raw files can only be used until integration with sox is complete");
				Environment.Exit(6);
			}

			Decode.DecodeEASTones(inputFileDirectory);
		}
	}
}
