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
				return $"Unrecognized Watch ({eventCode})";
			} else if (eventCode[2] == 'W') {
				if (subCode == "AV") { return "Avalanche Warning"; } else if (subCode == "BZ") { return "Blizzard Warning"; } else if (subCode == "CD") { urgent = true; return "Civil Danger Warning"; } else if (subCode == "CF") { return "Coastal Flood Warning"; } else if (subCode == "DS") { return "Dust Storm Warning"; } else if (subCode == "EQ") { return "Earthquake Warning"; } else if (subCode == "FF") { return "Flash Flood Warning"; } else if (subCode == "FL") { return "Flood Warning"; } else if (subCode == "FR") { return "Fire Warning"; } else if (subCode == "HM") { urgent = true; return "Hazardous Materials Warning"; } else if (subCode == "HU") { return "Hurricane Warning"; } else if (subCode == "HW") { return "High Wind Warning"; } else if (subCode == "LE") { urgent = true; return "Law Enforcement Warning"; } else if (subCode == "NU") { urgent = true; return "Nuclear Power Plant Warning"; } else if (subCode == "RH") { urgent = true; return "Radiological Hazard Warning"; } else if (subCode == "SM") { return "Special Marine Warning"; } else if (subCode == "SP") { urgent = true; return "Shelter In-Place Warning"; } else if (subCode == "TR") { return "Tropical Storm Warning"; } else if (subCode == "TS") { return "Tsunami Warning"; } else if (subCode == "VO") { return "Volcano Warning"; } else if (subCode == "WS") { return "Winter Storm Warning"; }
				return $"Unrecognized Warning ({eventCode})";
			} else if (eventCode[2] == 'S') {
				if (subCode == "FF") { return "Flash Flood Statement"; } else if (subCode == "FL") { return "Flood Statement"; } else if (subCode == "HL") { return "Hurricane Statement"; } else if (subCode == "SP") { return "Special Weather Statement"; } else if (subCode == "SV") { return "Severe Weather Statement"; }
				return $"Unrecognized Statement ({eventCode})";
			} else if (eventCode[2] == 'E') {
				if (subCode == "CA") { return "Child Abduction Emergency"; } else if (subCode == "LA") { return "Local Area Emergency"; } else if (subCode == "TO") { return "911 Telephone Outage Emergency"; }
				return $"Unrecognized Emergency ({eventCode})";
			} else if (eventCode == "SVR") { return "Severe Thunderstorm Warning"; } else if (eventCode == "TOR") { return "Tornado Warning"; } else if (eventCode == "ADR") { return "Administrative Message"; } else if (eventCode == "CEM") { return "Civil Emergency Message"; } else if (eventCode == "DMO") { return "Practice/Demo"; } else if (eventCode == "EAN") { national = true; return "Emergency Action Notification"; } else if (eventCode == "EAT") { national = true; return "Emergency Action Termination"; } else if (eventCode == "EVI") { national = true; return "Evacuation Immediate"; } else if (eventCode == "NIC") { return "National Information Center"; } else if (eventCode == "NMN") { return "Network Message Notification"; } else if (eventCode == "NPT") { return "National Periodic Test"; } else if (eventCode == "RMT") { return "Required Monthly Test"; } else if (eventCode == "RWT") { return "Required Weekly Test"; }
			return $"Unrecognized Alert ({eventCode})";
		}

		static Tuple<string, string, string, string, string, string, string>[] validation = new Tuple<string, string, string, string, string, string, string>[3] { null, null, null };
		static void TryParseDetails(int headerNumber, string message) {
			string issuer = message.Length >= 8 ? message[5..8] : "???";
			string eventCode = message.Length >= 12 ? message[9..12] : "???";
			string SAMECountyCodes = message.Length >= 13 + 23 ? message[13..^23] : null;
			string date = null;
			string UTCTime = null;
			string sender = null;

			int idx = 13 + (SAMECountyCodes != null ? SAMECountyCodes.Length : 0) + 1;
			string duration = null;
			if (message.Length >= idx) {
				string d = message[idx..(idx + 4)];
				if (int.TryParse(d, out _)) {
					duration = d;
				}
			}
			idx += 5;

			if (message.Length >= idx + 3) {
				date = message[idx..(idx + 3)];
			}
			idx += 3;

			if (message.Length >= idx + 4) {
				UTCTime = message[idx..(idx + 4)];
			}
			idx += 5;

			if (message.Length >= idx) {
				sender = message.Substring(idx);
			}

			validation[headerNumber] = new Tuple<string, string, string, string, string, string, string>(issuer, eventCode, SAMECountyCodes, date, UTCTime, duration, sender);
		}

		static string GetIssuerName(string issuerCode) {
			switch (issuerCode) {
				case "PEP":
					return "A Primary Entry Point System";
				case "CIV":
					return "Civil Authorities";
				case "WXR":
					return "The National Weather Service";
				case "EAS":
					return "An Emergency Alert System Participant";
				case "EAN":
					return "Emergency Action Notification Network";
				default:
					return "An Unknown Source";
			}
		}

		static bool IsEqualAndValid(string value1, string value2, string invalid) {
			return (value1 == value2) && value1 != invalid && value2 != invalid;
		}

		static void PrintMessageDetails(string message) {
			string[] issuerCodes = new string[3];
			string[] eventCodes = new string[3];
			string[] countyCodes = new string[3];
			string[] durations = new string[3];
			for (int i = 0; i < 3; i++) {
				Tuple<string, string, string, string, string, string, string> v = validation[i];
				if (v != null) {
					issuerCodes[i] = v.Item1;
					eventCodes[i] = v.Item2;
					countyCodes[i] = v.Item3;
					durations[i] = v.Item6;
				} else {
					issuerCodes[i] = "???";
					eventCodes[i] = "???";
					countyCodes[i] = null;
					durations[i] = null;
				}
			}

			string issuer;

			bool _01 = IsEqualAndValid(issuerCodes[0], issuerCodes[1], "???");
			bool _02 = IsEqualAndValid(issuerCodes[0], issuerCodes[2], "???");
			bool _12 = IsEqualAndValid(issuerCodes[1], issuerCodes[2], "???");
			if ((_01 && _12) || _01 || _02) {
				issuer = GetIssuerName(issuerCodes[0]);
			} else if (_12) {
				issuer = GetIssuerName(issuerCodes[1]);
			} else {    // if none are equal
				issuer = "An Unknown Source";
				for (int i = 0; i < 3; i++) {
					issuerCodes[i] = GetIssuerName(issuerCodes[i]);
					if (!issuerCodes[i].Contains("Unknown")) {
						issuer = issuerCodes[i];
						break;
					}
				}
			}

			string eventName;
			bool urgent = false;
			bool national = false;
			_01 = IsEqualAndValid(eventCodes[0], eventCodes[1], "???");
			_02 = IsEqualAndValid(eventCodes[0], eventCodes[2], "???");
			_12 = IsEqualAndValid(eventCodes[1], eventCodes[2], "???");
			if ((_01 && _12) || _01 || _02) {
				eventName = GetEventName(eventCodes[0], out urgent, out national);
			} else if (_12) {
				eventName = GetEventName(eventCodes[1], out urgent, out national);
			} else {    // if none are equal
				eventName = $"Unrecognized Alert ({string.Join(", ", eventCodes)})";
				for (int i = 0; i < 3; i++) {
					eventCodes[i] = GetEventName(eventCodes[i], out urgent, out national);
					if (!eventCodes[i].Contains("Unrecognized")) {
						eventName = eventCodes[i];
						break;
					}
				}
			}

			Console.WriteLine($"\n{(national ? "NATIONAL ALERT" : "EMERGENCY ALERT SYSTEM")}\n\n" +
				$"{issuer} has issued a {eventName} for");

			string sameCodes;
			_01 = IsEqualAndValid(countyCodes[0], countyCodes[1], null);
			_02 = IsEqualAndValid(countyCodes[0], countyCodes[2], null);
			_12 = IsEqualAndValid(countyCodes[1], countyCodes[2], null);
			bool none = false;
			if ((_01 && _12) || _01 || _02) {
				sameCodes = countyCodes[0];
			} else if (_12) {
				sameCodes = countyCodes[1];
			} else {    // if none are equal
				none = true;
				sameCodes = $"{countyCodes[0]}-{countyCodes[1]}-{countyCodes[2]}";
			}

			string[] SAMECountyCodes = new string[0];
			if (sameCodes != null) {
				SAMECountyCodes = sameCodes.Split('-');
			}
			List<string> unknownCounty = new List<string>();
			for (int i = 0; i < SAMECountyCodes.Length; i++) {
				string countyCode = SAMECountyCodes[i];
				if (Program.CountyCodes.ContainsKey(countyCode)) {
					Console.Write(Program.CountyCodes[countyCode]);
					if (i != SAMECountyCodes.Length - 1) {
						Console.Write(" - ");
					}
				} else {
					unknownCounty.Add(countyCode);
				}
			}

			Console.WriteLine();
			string sender = "unknown";
			StringBuilder timeInfo = new StringBuilder("on ");
			for (int i = validation.Length - 1; i >= 0; i--) {
				if (validation[i] != null) {
					if (validation[i].Item4 != null && int.TryParse(validation[i].Item4, out int ordinalDate)) {
						timeInfo.Append($"{new DateTime(DateTime.Now.Year, 1, 1).AddDays(ordinalDate - 1).ToShortDateString()} at ");
					} else {
						timeInfo.Append("unknown date at ");
					}
					if (validation[i].Item5 != null) {
						timeInfo.Append($"{validation[i].Item5[0..2]}:{validation[i].Item5[2..4]} UTC for ");
					} else {
						timeInfo.Append("unknown time for ");
					}
					sender = validation[i].Item7;
					break;
				}
			}

			int hours = -1;
			int minutes = -1;
			_01 = IsEqualAndValid(durations[0], durations[1], null);
			_02 = IsEqualAndValid(durations[0], durations[2], null);
			_12 = IsEqualAndValid(durations[1], durations[2], null);
			if ((_01 && _12) || _01 || _02) {
				hours = int.Parse(durations[0][0..2]);
				minutes = int.Parse(durations[0][2..4]);
			} else if (_12) {
				hours = int.Parse(durations[1][0..2]);
				minutes = int.Parse(durations[1][2..4]);
			} else {    // if none are equal
				for (int i = 0; i < 3; i++) {
					if (durations[i] != null) {
						hours = int.Parse(durations[i][0..2]);
						minutes = int.Parse(durations[i][2..4]);
						break;
					}
				}
			}
			if (hours != -1 && minutes != -1) {
				timeInfo.Append($"{hours} hour{(hours != 1 ? "s" : "")} and {minutes} minute{(minutes != 1 ? "s" : "")}");
			} else {
				timeInfo.Append("an unknown duration");
			}

			Console.WriteLine(timeInfo.ToString());
			Console.WriteLine($"Sent by {sender}");

			Console.WriteLine();
			if (none) {
				Console.WriteLine("No counties matched between each header - duplicate counties may have been printed");
			}
			if (unknownCounty.Count > 0) {
				Console.WriteLine($"Unknown county code{(unknownCounty.Count != 1 ? "s" : "")} found");
				foreach (string county in unknownCounty) {
					Console.WriteLine(county);
				}
				Console.WriteLine("If audio quality is poor, this maybe an error. If these codes are valid, please run this program with the '-u' flag.\n");
			}

			validation = new Tuple<string, string, string, string, string, string, string>[3];
		}

		static bool header = false;
		static bool eom = false;
		static long timeout;
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
					if (dem_st.headerStart != 0 && !header) {
						record = true;
						dem_st.headerStart += (uint) bytesReadIn;
						startByte = dem_st.headerStart;
						header = true;
					}
					if (dem_st.headerEnd != 0) {
						dem_st.headerEnd += (uint) bytesReadIn;
						headerLastDetected = dem_st.headerEnd;

						TryParseDetails(headerTonesReadIn, dem_st.message);

						if (++headerTonesReadIn == 3) {
							headerTonesReadIn = 0;
							PrintMessageDetails(dem_st.message);
							timeout = 0;
						}
						timeout = dem_st.headerEnd - dem_st.headerStart;
						dem_st.headerStart = 0;
						dem_st.headerEnd = 0;
						header = false;
					}
					if (dem_st.eomStart != 0 && !eom) {
						dem_st.eomStart += (uint) bytesReadIn;
						eom = true;
					}
					if (dem_st.eomEnd != 0) {
						dem_st.eomEnd += (uint) bytesReadIn;
						eomLastDetected = dem_st.eomEnd;
						eomTonesReadIn++;
						timeout = (dem_st.eomEnd - dem_st.eomStart);
						if (eomTonesReadIn == 3) {
							eomTonesReadIn = 0;
							record = false;
							endByte = eomLastDetected;
							timeout = 0;
						}
						dem_st.eomStart = 0;
						dem_st.eomEnd = 0;
						eom = false;
					}

					if (Program.Livestream) {
						//TODO: Look into extending timeout
						if (headerTonesReadIn > 0 && DateTime.Now - dem_st.headerDetected > new TimeSpan(0, 0, 5)) {
							headerTonesReadIn = 0;
							Console.WriteLine("Timeout occured waiting for EAS header tones");
							PrintMessageDetails(dem_st.message);
							timeout = 0;
						}
						if (eomTonesReadIn > 0 && DateTime.Now - dem_st.eomDetected > new TimeSpan(0, 0, 5)) {
							eomTonesReadIn = 0;
							record = false;
							Console.WriteLine("Timeout occured waiting for EOM tones");
							timeout = 0;
						}
					} else if (ProcessManager.bitrate != 0) {
						if (headerTonesReadIn > 0 && bytesReadIn - headerLastDetected > timeout * 3 + (ProcessManager.bitrate / 8)) {
							headerTonesReadIn = 0;
							Console.WriteLine("Timeout occured waiting for EAS header tones");
							PrintMessageDetails(dem_st.message);
							timeout = 0;
						}
						if (eomTonesReadIn > 0 && bytesReadIn - eomLastDetected > timeout * 300 + (ProcessManager.bitrate / 8)) {
							eomTonesReadIn = 0;
							record = false;
							Console.WriteLine("Timeout occured waiting for EOM tones");
							timeout = 0;
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
