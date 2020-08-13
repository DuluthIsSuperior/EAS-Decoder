using EAS_Decoder.Multimon;
using System;
using System.Collections.Generic;
using System.Text;

namespace EAS_Decoder {
	static class demodEAS {
		public static Multimon.Multimon multimon = new Multimon.Multimon(new Multimon.Multimon.demod_param {
			name = "EAS",
			float_samples = true,
			samplerate = FREQ_SAMP,
			overlap = CORRLEN,
			init = eas_init,
			demod = eas_demod,
			deinit = null
		});
		const double FREQ_MARK = 2083.3;
		const double FREQ_SPACE = 1562.5;
		const int FREQ_SAMP = 22050;
		const decimal BAUD = 520.83M;
		const int CORRLEN = (int) (FREQ_SAMP / BAUD);
		static float[] eascorr_mark_i = new float[CORRLEN];
		static float[] eascorr_mark_q = new float[CORRLEN];
		static float[] eascorr_space_i = new float[CORRLEN];
		static float[] eascorr_space_q = new float[CORRLEN];

		public static void eas_init(Multimon.Multimon.demod_state s) {
			// NOTE: Percision differences between C# and C

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
		}

		public static void eas_demod(Multimon.Multimon.demod_state s, Multimon.Multimon.buffer buffer, int length) {

		}
	}
}
