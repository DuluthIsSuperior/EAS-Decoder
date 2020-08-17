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
using System.IO;

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
						if (++headerTonesReadIn == 3) {
							headerTonesReadIn = 0;
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

					if (Sox.samplerate != 0) {
						if (headerTonesReadIn > 0 && bytesReadIn + bytesToRead - headerLastDetected > Sox.samplerate * 5) {
							headerTonesReadIn = 0;
							Console.WriteLine("Timeout occured waiting for EAS header tones");
						}
						if (eomTonesReadIn > 0 && bytesReadIn + bytesToRead - eomLastDetected > Sox.samplerate * 5) {
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
