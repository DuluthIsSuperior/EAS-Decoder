/*
 * This code was ported over from multimon-ng - https://github.com/EliasOenal/multimon-ng/
 * demod_eas.cs -- Emergency Alert System demodulator
 *
 *		See http://www.nws.noaa.gov/nwr/nwrsame.htm
 *
 * Copyright (C) 2000
 *		A. Maitland Bottoms <bottoms@debian.org>
 *
 * Licensed under same terms and based upon the
 *		demod_eas_2.c -- 1200 baud AFSK demodulator
 *
 * Copyright (C) 1996
 * Thomas Sailer (sailer@ife.ee.ethz.ch, hb9jnx@hb9w.che.eu)
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

namespace EAS_Decoder {

	static class DemodEAS {
		public static Multimon.DemodParam demod_eas = new Multimon.DemodParam {
			name = "EAS",
			float_samples = true,
			samplerate = FREQ_SAMP,
			overlap = CORRLEN,
			init = EASInit,
			demod = EASDemod,
			deinit = null
		};
		const double FREQ_MARK = 2083.3;
		const double FREQ_SPACE = 1562.5;
		const int FREQ_SAMP = 22050;
		const decimal BAUD = 520.83M;
		const int CORRLEN = (int) (FREQ_SAMP / BAUD);
		readonly static float[] eascorr_mark_i = new float[CORRLEN];
		readonly static float[] eascorr_mark_q = new float[CORRLEN];
		readonly static float[] eascorr_space_i = new float[CORRLEN];
		readonly static float[] eascorr_space_q = new float[CORRLEN];
		const int SUBSAMP = 2;
		const int INTEGRATOR_MAXVAL = 10;
		const int SPHASEINC = (int) (0x10000u * BAUD * SUBSAMP / FREQ_SAMP);
		const int DLL_MAX_INC = 8192;
		const byte PREAMBLE = (byte) '\xAB';    // unsigned char
		const string HEADER_BEGIN = "ZCZC";
		const int MAX_HEADER_LEN = 4;
		const string EOM = "NNNN";
		const int MAX_MSG_LEN = 268;

		public static Multimon.DemodState EASInit(Multimon.DemodState s) {
			// NOTE: Percision differences between C# and C

			s.eas_2 = new Multimon.State1();
			s.eas = new Multimon.State2();

			float f;
			int i;
			for (f = 0, i = 0; i < CORRLEN; i++) {
				eascorr_mark_i[i] = (float) Math.Cos(f);
				eascorr_mark_q[i] = (float) Math.Sin(f);
				f += (float) (2.0 * Math.PI * FREQ_MARK / FREQ_SAMP);
				//Console.WriteLine(f);
			}
			for (f = 0, i = 0; i < CORRLEN; i++) {
				eascorr_space_i[i] = (float) Math.Cos(f);
				eascorr_space_q[i] = (float) Math.Sin(f);
				f += (float) (2.0 * Math.PI * FREQ_SPACE / FREQ_SAMP);
			}
			return s;
		}

		static bool CharacterAllowed(char data) {
			if ((data & 0x80) != 0) {
				return false;
			} else if (data == 10 || data == 13) {
				return true;
			} else if (data >= 32 && data <= 126) {
				return true;
			}
			return false;
		}

		static bool IsEqualUpToN(char[] s1, string s2, uint n) {
			return string.Compare(string.Join("", s1), 0, s2, 0, (int) n) == 0;
		}

		static Multimon.DemodState EASFrame(Multimon.DemodState s, char data) {
			if (data != 0) {
				if (s.eas.state == Multimon.EAS_L2_IDLE) {
					s.eas.state = Multimon.EAS_L2_HEADER_SEARCH;
				}
				if (s.eas.state == Multimon.EAS_L2_HEADER_SEARCH && s.eas.headlen < MAX_HEADER_LEN) {
					s.eas.head_buf[s.eas.headlen] = data;
					s.eas.headlen++;
				}
				if (s.eas.state == Multimon.EAS_L2_HEADER_SEARCH && s.eas.headlen >= MAX_HEADER_LEN) {
					if (IsEqualUpToN(s.eas.head_buf, HEADER_BEGIN, s.eas.headlen)) {
						s.eas.state = Multimon.EAS_L2_READING_MESSAGE;
					} else if (IsEqualUpToN(s.eas.head_buf, EOM, s.eas.headlen)) {
						s.eas.state = Multimon.EAS_L2_READING_EOM;
					} else {
						s.eas.state = Multimon.EAS_L2_IDLE;
						s.eas.headlen = 0;
					}
				} else if (s.eas.state == Multimon.EAS_L2_READING_MESSAGE && s.eas.msglen <= MAX_MSG_LEN) {
					s.eas.msg_buf[s.eas.msgno][s.eas.msglen] = data;
					s.eas.msglen++;
				}
			} else {
				if (s.eas.state == Multimon.EAS_L2_READING_MESSAGE) {
					int lastHyphen = -1;
					for (int i = 0; i < s.eas.msg_buf[s.eas.msgno].Length; i++) {
						if (s.eas.msg_buf[s.eas.msgno][i] == '-') {
							lastHyphen = i;
						}
					}
					if (lastHyphen != -1) {
						s.eas.msg_buf[s.eas.msgno][lastHyphen + 1] = '\0';
					}

					string easMessage = "";
					foreach (char c in s.eas.msg_buf[s.eas.msgno]) {
						if (c == 0) {
							break;
						}
						easMessage += c;
					}

					Console.WriteLine($"{s.dem_par.name}: {HEADER_BEGIN}{easMessage}");
				} else if (s.eas.state == Multimon.EAS_L2_READING_EOM) {
					Console.WriteLine($"{s.dem_par.name}: {EOM}");
				}

				s.eas.state = Multimon.EAS_L2_IDLE;
				s.eas.msglen = 0;
				s.eas.headlen = 0;
			}

			return s;
		}
		static float Mac(float[] a, int start, float[] b, uint size) {
			int aIdx = start;
			int bIdx = 0;
			float sum = 0;
			for (int i = 0; i < size; i++) {
				sum += a[aIdx++] * b[bIdx++];
			}
			return sum;
		}
		public static Multimon.DemodState EASDemod(Multimon.DemodState s, Multimon.Buffer buffer, int length) {
			float f;
			float dll_gain;

			int idx = 0;
			if (s.eas_2.subsamp != 0) {
				int numfill = SUBSAMP - (int) s.eas_2.subsamp;
				if (length < numfill) {
					s.eas_2.subsamp += (uint) length;
					return s;
				}
				idx += numfill;
				length -= numfill;
				s.eas_2.subsamp = 0;
			}

			while (true) {
				length -= SUBSAMP;
				if (length < SUBSAMP) {
					break;
				}
				idx += SUBSAMP;
				f = (float) Math.Pow(Mac(buffer.fbuffer, idx,  eascorr_mark_i, CORRLEN), 2.0) +
					(float) Math.Pow(Mac(buffer.fbuffer, idx,  eascorr_mark_q, CORRLEN), 2.0) -
					(float) Math.Pow(Mac(buffer.fbuffer, idx, eascorr_space_i, CORRLEN), 2.0) -
					(float) Math.Pow(Mac(buffer.fbuffer, idx, eascorr_space_q, CORRLEN), 2.0);
				s.eas_2.dcd_shreg <<= 1;
				s.eas_2.dcd_shreg |= (f > 0 ? (uint) 1 : (uint) 0);
				if (f > 0 && s.eas_2.dcd_integrator < INTEGRATOR_MAXVAL) {
					s.eas_2.dcd_integrator += 1;
				} else if (f < 0 && s.eas_2.dcd_integrator > -INTEGRATOR_MAXVAL) {
					s.eas_2.dcd_integrator -= 1;
				}

				dll_gain = 0.5F;

				if (((s.eas_2.dcd_shreg ^ (s.eas_2.dcd_shreg >> 1)) & 1) == 1) {
					if (s.eas_2.sphase < (0x8000u - (SPHASEINC / 8))) {
						if (s.eas_2.sphase > SPHASEINC / 2) {
							s.eas_2.sphase -= (uint) Math.Min((int) (s.eas_2.sphase * dll_gain), DLL_MAX_INC);
						}
					} else {
						if (s.eas_2.sphase < (0x10000u - SPHASEINC / 2)) {
							s.eas_2.sphase += (uint) Math.Min((int) (0x10000u - s.eas_2.sphase) * dll_gain, DLL_MAX_INC);
						}
					}
				}

				s.eas_2.sphase += SPHASEINC;

				if (s.eas_2.sphase >= 0x10000u) {
					s.eas_2.sphase = 1;
					s.eas_2.lasts >>= 1;
					s.eas_2.lasts |= (byte) (((s.eas_2.dcd_integrator >= 0 ? 1 : 0) << 7) & 0x80u);

					if (s.eas_2.lasts == PREAMBLE && s.eas.state != Multimon.EAS_L2_READING_MESSAGE) {
						s.eas_2.state = Multimon.EAS_L1_SYNC;
						s.eas_2.byte_counter = 0;
					} else if (s.eas_2.state == Multimon.EAS_L1_SYNC) {
						s.eas_2.byte_counter++;
						if (s.eas_2.byte_counter == 8) {
							if (CharacterAllowed((char) s.eas_2.lasts)) {
								s = EASFrame(s, (char) s.eas_2.lasts);
							} else {
								s.eas_2.state = Multimon.EAS_L1_IDLE;
								s = EASFrame(s, (char) 0x00);
							}
							s.eas_2.byte_counter = 0;
						}
					}
				}
			}
			s.eas_2.subsamp = (uint) length;
			return s;
		}
	}
}
