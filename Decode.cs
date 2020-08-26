/*
 * This code was ported over from multimon-ng - https://github.com/EliasOenal/multimon-ng/
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EAS_Decoder {
	static class Decode {
		public static DemodEAS.DemodState dem_st = DemodEAS.EASInit(new DemodEAS.DemodState());
		static short[] buffer = new short[8192];
		static float[] fbuf = new float[16384];
		static uint global_fbuf_cnt = 0;

		public static uint headerLastDetected = 0;
		public static int headerTonesReadIn = 0;
		public static uint eomLastDetected = 0;
		public static int eomTonesReadIn = 0;

		public static int bytesReadIn = 0;
		static bool record = false;

		static string GetEventName(string eventCode, out bool urgent, out bool national) {
			urgent = false;
			national = false;
			string subCode = eventCode.Substring(0, 2);
			if (eventCode[2] == 'A') {  // watch
				if (subCode == "AV") { return "Avalanche Watch"; } else if (subCode == "CF") { return "Coastal Flood Watch"; } else if (subCode == "FF") { return "Flash Flood Watch"; } else if (subCode == "FL") { return "Flood Watch"; } else if (subCode == "HU") { return "Hurricane Watch"; } else if (subCode == "HW") { return "High Wind Watch"; } else if (subCode == "SV") { return "Severe Thunderstorm Watch"; } else if (subCode == "TO") { return "Tornado Watch"; } else if (subCode == "TR") { return "Tropical Storm Watch"; } else if (subCode == "TS") { return "Tsunami Watch"; } else if (subCode == "WS") { return "Winter Storm Watch"; }
				return "Unrecognized Watch";
			} else if (eventCode[2] == 'W') {
				if (subCode == "AV") { return "Avalanche Warning"; } else if (subCode == "BZ") { return "Blizzard Warning"; } else if (subCode == "CD") { urgent = true; return "Civil Danger Warning"; } else if (subCode == "CF") { return "Coastal Flood Warning"; } else if (subCode == "DS") { return "Dust Storm Warning"; } else if (subCode == "EQ") { return "Earthquake Warning"; } else if (subCode == "FF") { return "Flash Flood Warning"; } else if (subCode == "FL") { return "Flood Warning"; } else if (subCode == "FR") { return "Fire Warning"; } else if (subCode == "HM") { urgent = true; return "Hazardous Materials Warning"; } else if (subCode == "HU") { return "Hurricane Warning"; } else if (subCode == "HW") { return "High Wind Warning"; } else if (subCode == "LE") { urgent = true; return "Law Enforcement Warning"; } else if (subCode == "NU") { urgent = true; return "Nuclear Power Plant Warning"; } else if (subCode == "RH") { urgent = true; return "Radiological Hazard Warning"; } else if (subCode == "SM") { return "Special Marine Warning"; } else if (subCode == "SP") { urgent = true; return "Shelter In-Place Warning"; } else if (subCode == "TR") { return "Tropical Storm Warning"; } else if (subCode == "TS") { return "Tsunami Warning"; } else if (subCode == "VO") { return "Volcano Warning"; } else if (subCode == "WS") { return "Winter Storm Warning"; }
				return "Unrecognized Warning";
			} else if (eventCode[2] == 'S') {
				if (subCode == "FF") { return "Flash Flood Statement"; } else if (subCode == "FL") { return "Flood Statement"; } else if (subCode == "HL") { return "Hurricane Statement"; } else if (subCode == "SP") { return "Special Weather Statement"; } else if (subCode == "SV") { return "Severe Weather Statement"; }
				return "Unrecognized Statement";
			} else if (eventCode[2] == 'E') {
				if (subCode == "CA") { return "Child Abduction Emergency"; } else if (subCode == "LA") { return "Local Area Emergency"; } else if (subCode == "TO") { return "911 Telephone Outage Emergency"; }
				return "Unrecognized Emergency";
			} else if (eventCode == "SVR") { return "Severe Thunderstorm Warning"; } else if (eventCode == "TOR") { return "Tornado Warning"; } else if (eventCode == "ADR") { return "Administrative Message"; } else if (eventCode == "CEM") { return "Civil Emergency Message"; } else if (eventCode == "DMO") { return "Practice/Demo"; } else if (eventCode == "EAN") { national = true; return "Emergency Action Notification"; } else if (eventCode == "EAT") { national = true; return "Emergency Action Termination"; } else if (eventCode == "EVI") { national = true; return "Evacuation Immediate"; } else if (eventCode == "NIC") { return "National Information Center"; } else if (eventCode == "NMN") { return "Network Message Notification"; } else if (eventCode == "NPT") { return "National Periodic Test"; } else if (eventCode == "RMT") { return "Required Monthly Test"; } else if (eventCode == "RWT") { return "Required Weekly Test"; }
			return $"Unrecognized Alert ({eventCode})";
		}

		static void PrintMessageDetails(string message) {
			bool urgentIssuer = false;

			Console.WriteLine(string.Join(", ", issuerReceived));

			string issuerCode = message[5..8];
			string issuer;
			switch (issuerCode) {
				case "PEP":
					issuer = "A Primary Entry Point System";
					urgentIssuer = true;
					break;
				case "CIV":
					issuer = "Civil Authorities";
					break;
				case "WXR":
					issuer = "The National Weather Service";
					break;
				case "EAS":
					issuer = "An Emergency Alert System Participant";
					break;
				case "EAN":
					issuer = "Emergency Action Notification Network";
					urgentIssuer = true;
					break;
				default:
					issuer = "An Unknown Source";
					break;
			}

			string eventCode = message[9..12];
			string eventName = GetEventName(eventCode, out bool urgentAlert, out bool nationalAlert);

			Console.WriteLine($"\n{(nationalAlert ? "NATIONAL ALERT" : "EMERGENCY ALERT SYSTEM")}\n\n" +
				$"{issuer} has issued a {eventName} for");
			string[] SAMECountyCodes = message[13..^23].Split('-');
			List<string> unknownCounty = new List<string>();
			for (int i = 0; i < SAMECountyCodes.Length; i++) {
				string countyCode = SAMECountyCodes[i];
				if (Program.countyCodes.ContainsKey(countyCode)) {
					Console.Write(Program.countyCodes[countyCode]);
					if (i != SAMECountyCodes.Length - 1) {
						Console.Write(" - ");
					}
				} else {
					unknownCounty.Add(countyCode);
				}
			}
			Console.WriteLine();
			StringBuilder timeInfo = new StringBuilder("on ");
			if (int.TryParse(message[^17..^14], out int julianDate)) {
				timeInfo.Append(new DateTime(DateTime.Now.Year, 1, 1).AddDays(julianDate - 1).ToShortDateString());
			} else {
				timeInfo.Append(message[^17..^14]);
			}
			timeInfo.Append($" at {message[^14..^12]}:{message[^12..^10]} for ");
			string duration = message[^22..^18];
			if (int.TryParse(duration[0..2], out int hours)) {
				timeInfo.Append($"{hours} hour{(hours != 1 ? "s" : "")} and ");
			} else {
				timeInfo.Append($"{duration[0..2]} hour(s) and ");
			}
			if (int.TryParse(duration[2..4], out int minutes)) {
				timeInfo.Append($"{minutes} minute{(minutes != 1 ? "s" : "")}");
			} else {
				timeInfo.Append($"{duration[2..4]} minute(s)");
			}
			Console.WriteLine(timeInfo.ToString());

			if (Program.livestream) {
				Console.WriteLine("Please listen to the audio for an accurate expiration time");
				if (urgentIssuer || urgentAlert || nationalAlert) {
					Console.WriteLine("\nURGENT!");
				}
			}
			Console.WriteLine();
			if (unknownCounty.Count > 0) {
				Console.WriteLine($"Unknown county code{(unknownCounty.Count != 1 ? "s" : "")} found");
				foreach (string county in unknownCounty) {
					Console.WriteLine(county);
				}
				Console.WriteLine("If audio quality is poor, this maybe an error. If these codes are valid, please run this program with the '-u' flag.\n");
			}

			issuerReceived = new string[3];
		}

		static string[] issuerReceived = new string[3];
		public static Tuple<bool, uint, uint> DecodeEAS(byte[] raw, int i) {
			uint overlap = (uint) DemodEAS.overlap;
			uint startByte = 0;
			uint endByte = 0;

			int idx = 0;
			if (i <= 0) {
				if (i < 0) {
					Console.WriteLine("An error occurred reading from the input file");
				}
				return new Tuple<bool, uint, uint>(record, 0, 0);
			} else {
				Buffer.BlockCopy(raw, 0, buffer, 0, i);
				int bytesToRead = i;

				while (true) {
					i -= sizeof(short);
					if (i < sizeof(short)) {
						break;
					}
					idx++;
					fbuf[global_fbuf_cnt++] = buffer[idx] * (1.0F / 32768.0F);
				}
				if (i != 0) {
					Console.WriteLine("warn: uneven number of samples read");
				}
				if (global_fbuf_cnt > overlap) {
					dem_st = DemodEAS.EASDemod(dem_st, fbuf, (int) (global_fbuf_cnt - overlap));   // process buffer
					if (dem_st.headerStart != 0) {
						record = true;
						dem_st.headerStart += (uint) bytesReadIn;
						startByte = dem_st.headerStart;
					}
					if (dem_st.headerEnd != 0) {
						headerLastDetected = dem_st.headerEnd + (uint) bytesReadIn;
						issuerReceived[headerTonesReadIn] = dem_st.message[5..8];
						if (++headerTonesReadIn == 3) {
							headerTonesReadIn = 0;
							PrintMessageDetails(dem_st.message);
						}
						dem_st.headerStart = 0;
						dem_st.headerEnd = 0;
						
					}
					if (dem_st.eomStart != 0) {
						dem_st.eomStart += (uint) bytesReadIn;
					}
					if (dem_st.eomEnd != 0) {
						eomLastDetected = dem_st.eomEnd + (uint) bytesReadIn;
						eomTonesReadIn++;
						if (eomTonesReadIn == 3) {
							eomTonesReadIn = 0;
							record = false;
							endByte = eomLastDetected;
						}
						dem_st.eomStart = 0;
						dem_st.eomEnd = 0;
					}

					if (ProcessManager.samplerate != 0) {
						if (headerTonesReadIn > 0 && bytesReadIn + bytesToRead - headerLastDetected > ProcessManager.samplerate * 5) {
							headerTonesReadIn = 0;
							Console.WriteLine("Timeout occured waiting for EAS header tones");
							PrintMessageDetails(dem_st.message);
						}
						if (eomTonesReadIn > 0 && bytesReadIn + bytesToRead - eomLastDetected > ProcessManager.samplerate * 5) {
							eomTonesReadIn = 0;
							record = false;
							Console.WriteLine("Timeout occured waiting for EOM tones");
						}
					}

					Array.Copy(fbuf, global_fbuf_cnt - overlap, fbuf, 0, overlap * sizeof(float));
					global_fbuf_cnt = overlap;
				}
				bytesReadIn += bytesToRead;
			}
			Array.Clear(buffer, 0, buffer.Length);
			return new Tuple<bool, uint, uint>(record, startByte, endByte);
		}
		public static void DecodeFromFile(string inputFilePath) {
			FileStream fd = null;
			try {
				fd = File.OpenRead(inputFilePath);
			} catch (Exception) {
				Console.WriteLine("An error occurred opening the input file");
				Environment.Exit(9);
			}

			while (true) {
				byte[] raw = new byte[buffer.Length * 2];
				int i = fd.Read(raw, 0, raw.Length);

				if (i < 0) {
					Console.WriteLine("An error occurred reading from the input file");
					return;
				} else if (i == 0) {
					return;
				} else if (i > 0) {
					DecodeEAS(raw, i);
				}
			}
		}
	}
}
