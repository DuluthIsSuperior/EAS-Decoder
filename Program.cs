﻿using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;

namespace EAS_Decoder {
	class Program {
		static void DisplayHelp() {
			Console.WriteLine("\nUsage:\n    EASDecoder [args]\n\n" +
						"Arguments:\n" +
						"    -s [DIRECTORY]: Directory to sox.exe (not required if sox is installed in its default location on any drive)\n" +
						"    -i or --input [FILEPATH]: Input file to analyze\n" +
						"                              Do NOT put a URL in this argument, audio files do not record properly if you do so\n" +
						"    -t or --type [TYPE]: The type of the input file (assumed to be .raw if not specified)\n" +
						"    -o or --output [FILEPATH]: Output file to convert input file to raw using sox\n" +
						"    -r or --record: Saves a snippet of any alerts that this program reads in using parameters from the input file\n" +
						"                    Does not work if the input file is already raw\n" +
						"    -h or --help: Displays this help page");
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
						if (!ValidURL(args[i])) {
							Console.WriteLine($"Could not open file or URL: {args[i]}");
							Environment.Exit(4);
						}
						inputFileDirectory = args[i];
					}
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
				} else if (args[i] == "-h" || args[i] == "--help") {
					DisplayHelp();
					Environment.Exit(0);
				} else {
					Console.WriteLine($"Invalid argument: {args[i]}");
					Environment.Exit(2);
				}
			}

			if (inputFileDirectory != null) {
				if (inputFileType != "raw") {
					Sox.GetSoxProcess(soxDirectory);

					int soxExitCode;
					if (recordOnEAS) {
						soxExitCode = Sox.GetFileInformation(inputFileDirectory);
						DidSoxFail(soxExitCode);
					}

					soxExitCode = Sox.ConvertAndDecode(inputFileType, inputFileDirectory, inputFileType, outputFileDirectory);
					DidSoxFail(soxExitCode);
				} else {
					Decode.DecodeFromFile(inputFileDirectory);
				}
			} else {    // if both are null
				Console.WriteLine("An input file or a streaming audio file is required");
			}
		}
	}
}
