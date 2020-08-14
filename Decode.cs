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
		static Multimon.DemodParam demod_eas;
		static Multimon.DemodState dem_st = new Multimon.DemodState();

		static Tuple<float[], short[]> ProcessBuffer(float[] float_buf, short[] short_buf, uint len) {
			Multimon.Buffer buffer = new Multimon.Buffer {
				fbuffer = float_buf,
				sbuffer = short_buf
			};
			dem_st = demod_eas.demod(dem_st, buffer, (int) len);
			return new Tuple<float[], short[]>(buffer.fbuffer, buffer.sbuffer);
		}

		public static void InputFile(uint overlap, string fname) {
			int i;
			short[] buffer = new short[8192];
			float[] fbuf = new float[16384];
			uint fbuf_cnt = 0;

			FileStream fd = File.OpenRead(fname);

			int bytesReadIn = 0;
			while (true) {
				byte[] raw = new byte[buffer.Length * 2];
				i = fd.Read(raw, 0, raw.Length);
				bytesReadIn += i;

				int idx = 0;

				if (i < 0) {
					Console.WriteLine("Error");
					Environment.Exit(4);
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
						Tuple<float[], short[]> p = ProcessBuffer(fbuf, buffer, fbuf_cnt - overlap);
						fbuf = p.Item1;
						buffer = p.Item2;
						Array.Copy(fbuf, fbuf_cnt - overlap, fbuf, 0, overlap * sizeof(float));
						fbuf_cnt = overlap;
					}

					Array.Clear(buffer, 0, buffer.Length);
				}
			}
			Console.WriteLine(bytesReadIn);
		}

		public static void DecodeEASTones() {
			demod_eas = DemodEAS.demod_eas;

			int sample_rate = -1;
			uint overlap = 0;
			string inputFile = "aTOR.raw";

			dem_st.dem_par = demod_eas;
			dem_st = demod_eas.init(dem_st);
			sample_rate = demod_eas.samplerate;

			if (demod_eas.overlap > overlap) {
				overlap = demod_eas.overlap;
			}

			Console.WriteLine($"{sample_rate} {overlap}");
			Console.WriteLine("Beginning demodulation...");
			InputFile(overlap, inputFile);
		}
	}
}
