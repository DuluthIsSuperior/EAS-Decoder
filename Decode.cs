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
		static DemodEAS.DemodState dem_st = DemodEAS.EASInit(new DemodEAS.DemodState());
		static short[] global_buffer = new short[8192];
		static float[] global_fbuf = new float[16384];
		static uint global_fbuf_cnt = 0;

		public static bool DecodeEAS(byte[] raw, int i) {
			uint overlap = (uint) DemodEAS.overlap;

			int idx = 0;
			if (i < 0) {
				Console.WriteLine("An error occurred reading from the input file");
				return false;
			} else if (i == 0) {
				return false;
			} else {
				Buffer.BlockCopy(raw, 0, global_buffer, 0, i);

				while (true) {
					i -= sizeof(short);
					if (i < sizeof(short)) {
						break;
					}
					idx++;
					global_fbuf[global_fbuf_cnt++] = global_buffer[idx] * (1.0F / 32768.0F);
				}
				if (i != 0) {
					Console.WriteLine("warn: uneven number of samples read");
				}
				if (global_fbuf_cnt > overlap) {
					dem_st = DemodEAS.EASDemod(dem_st, global_fbuf, (int) (global_fbuf_cnt - overlap));   // process buffer
					Array.Copy(global_fbuf, global_fbuf_cnt - overlap, global_fbuf, 0, overlap * sizeof(float));
					global_fbuf_cnt = overlap;
				}
			}

			Array.Clear(global_buffer, 0, global_buffer.Length);
			return dem_st.eas_2.state != DemodEAS.EAS_L2_IDLE;
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
				byte[] raw = new byte[global_buffer.Length * 2];
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
