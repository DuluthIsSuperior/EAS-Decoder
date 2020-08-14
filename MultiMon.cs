/*
 * This code was ported over from multimon-ng - https://github.com/EliasOenal/multimon-ng/
 * Copyright (C) 1996
 *      Thomas Sailer (sailer@ife.ee.ethz.ch, hb9jnx@hb9w.che.eu)
 *
 * Added eas parts - A. Maitland Bottoms 27 June 2000
 *
 * Copyright (C) 2012-2014
 *      Elias Oenal    (multimon-ng@eliasoenal.com)
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
	public class Multimon {
        public const int EAS_L2_IDLE = 0;
        public const int EAS_L2_HEADER_SEARCH = 1;
        public const int EAS_L2_READING_MESSAGE = 2;
        public const int EAS_L2_READING_EOM = 3;
        public const int EAS_L1_IDLE = 0;
        public const int EAS_L1_SYNC = 1;
        public class State2 {
            public char[] last_message;
            public char[][] msg_buf;
            public char[] head_buf;
            public uint headlen;
            public uint msglen;
            public uint msgno;
            public uint state;

            public State2() {
                last_message = new char[269];
                msg_buf = new char[4][];
                for (int i = 0; i < 4; i++) {
                    msg_buf[i] = new char[269];
				}
                head_buf = new char[4];
			}
        };
        public class State1 {
            public uint dcd_shreg;
            public uint sphase;
            public byte lasts;    // unsigned char in C is 1 byte; byte is unsigned always
            public uint subsamp;
            public byte byte_counter;  // unsigned char
            public int dcd_integrator;
            public uint state;
        };
        public class Buffer {    // typedef struct buffer {} buffer_t
            public short[] sbuffer;    // short*
            public float[] fbuffer;    // float*
        }
        public class DemodParam {
            public string name;    // char*
            public bool float_samples; // if false samples are short instead
            public int samplerate;  // unsigned 
            public uint overlap;
			public Func<DemodState, DemodState> init;    //void (*init)(struct demod_state *s);
            public Func<DemodState, Buffer, int, DemodState> demod; // void (*demod)(struct demod_state *s, buffer_t buffer, int length);
            public Action<DemodState> deinit; // void (*deinit)(struct demod_state *s);
        };

        public struct DemodState {
            public DemodParam dem_par;    // demod_param*
            public State2 eas;
            public State1 eas_2;
        }
    }
}
