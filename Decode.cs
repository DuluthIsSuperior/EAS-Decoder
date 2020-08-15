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
		public static void DecodeEASTones(string inputFilePath) {
			DemodEAS.DemodState dem_st = new DemodEAS.DemodState();
			uint overlap = 0;

			dem_st = DemodEAS.EASInit(dem_st);

			if (DemodEAS.overlap > overlap) {
				overlap = (uint) DemodEAS.overlap;
			}


			short[] buffer = new short[8192];
			float[] fbuf = new float[16384];
			uint fbuf_cnt = 0;

			FileStream fd = null;
			try {
				fd = File.OpenRead(inputFilePath);
			} catch (Exception) {
				Console.WriteLine("An error occurred opening the input file");
				Environment.Exit(9);
			}

			Console.WriteLine("Beginning demodulation...");
			int bytesReadIn = 0;
			while (true) {
				byte[] raw = new byte[buffer.Length * 2];
				int i = fd.Read(raw, 0, raw.Length);
				bytesReadIn += i;

				int idx = 0;

				if (i < 0) {
					Console.WriteLine("An error occurred reading from the input file");
					return;
				} else if (i == 0) {
					break;
				} else if (i > 0) {
					Buffer.BlockCopy(raw, 0, buffer, 0, raw.Length);

					while (true) {
						i -= sizeof(short);
						if (i < sizeof(short)) {
							break;
						}
						idx++;
						fbuf[fbuf_cnt++] = buffer[idx] * (1.0F / 32768.0F);
					}
					if (i != 0) {
						Console.WriteLine("warn: uneven number of samples read");
					}
					if (fbuf_cnt > overlap) {
						dem_st = DemodEAS.EASDemod(dem_st, fbuf, (int) (fbuf_cnt - overlap));	// process buffer
						Array.Copy(fbuf, fbuf_cnt - overlap, fbuf, 0, overlap * sizeof(float));
						fbuf_cnt = overlap;
					}

					Array.Clear(buffer, 0, buffer.Length);
				}
			}
		}
	}
}
