using System;
using System.IO;
using System.Runtime.CompilerServices;

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

			soxDirectory = Sox.GetSoxProcess(soxDirectory);

			Decode.DecodeEASTones("output.raw");
			Decode.DecodeEASTones("output2.raw");
			Decode.DecodeEASTones("anSVR.raw");
			Decode.DecodeEASTones("aTOR.raw");

			Console.WriteLine("DONE");
			Console.ReadKey();
		}
	}
}
