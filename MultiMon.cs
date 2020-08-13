using System;

namespace EAS_Decoder.Multimon {
	public class Multimon {
        enum EAS_L2_State {
            EAS_L2_IDLE = 0,
            EAS_L2_HEADER_SEARCH = 1,
            EAS_L2_READING_MESSAGE = 2,
            EAS_L2_READING_EOM = 3,
        };
        enum EAS_L1_State {
            EAS_L1_IDLE = 0,
            EAS_L1_SYNC = 1,
        };
        public struct l2_state_eas {
            char[] last_message; //[269];
            char[][] msg_buf; //[4][269];
            char[] head_buf; //[4];
            UInt32 headlen;
            UInt32 msglen;
            UInt32 msgno;
            UInt32 state;
        };
        struct l1_state_eas {
            uint dcd_shreg;
            uint sphase;
            byte lasts;    // unsigned char in C is 1 byte; byte is unsigned always
            uint subsamp;
            byte byte_counter;  // unsigned char
            int dcd_integrator;
            UInt32 state;
        };
        public class buffer {    // typedef struct buffer {} buffer_t
            unsafe short* sbuffer;
            unsafe float* fbuffer;
        }
        public struct demod_param {
            public string name;    // char*
            public bool float_samples; // if false samples are short instead
            public int samplerate;  // unsigned 
            public uint overlap;
			public Action<demod_state> init;    //void (*init)(struct demod_state *s);
            public Action<demod_state, buffer, int> demod; // void (*demod)(struct demod_state *s, buffer_t buffer, int length);
            public Action<demod_state> deinit; // void (*deinit)(struct demod_state *s);
        };

        public struct demod_state {
            public demod_param dem_par;    // demod_param*
            l2_state_eas eas;
            l1_state_eas l1_eas;
        }

        public demod_param demod_eas;  // extern const struct demod_param demod_eas;


        public Multimon(demod_param p) {
            demod_eas = p;
		}
    }
}
