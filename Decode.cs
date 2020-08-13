using System;
using System.Collections.Generic;
using System.Text;
using EAS_Decoder.Multimon;

namespace EAS_Decoder {
	static class Decode {
		static Multimon.Multimon.demod_param demod_eas;
		static Multimon.Multimon.demod_state dem_st = new Multimon.Multimon.demod_state();

		public static void input_file() {

		}

		public static void DecodeEASTones() {
			demod_eas = demodEAS.multimon.demod_eas;

			int sample_rate = -1;
			uint overlap = 0;
			string inputFile = "output2.raw";
			string input_type = "raw";

			dem_st.dem_par = demod_eas;
			demod_eas.init(dem_st);
			sample_rate = demod_eas.samplerate;

			if (demod_eas.overlap > overlap) {
				overlap = demod_eas.overlap;
			}

			Console.WriteLine($"{sample_rate} {overlap}");
			Console.WriteLine("Beginning demodulation...");
		}
	}
}
