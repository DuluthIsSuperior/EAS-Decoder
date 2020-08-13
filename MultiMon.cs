using System;

namespace EAS_Decoder {
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
    public struct demod_param {
        unsafe char* name;
        bool float_samples; // if false samples are short instead
        uint samplerate;
        uint overlap;
        unsafe void* init;// (struct demod_state *s);
        unsafe void* demod; // (struct demod_state *s, buffer_t buffer, int length);
        unsafe void* deinit; // (struct demod_state *s);
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
    struct demod_state {
        unsafe demod_param* dem_par;
        l2_state_eas eas;
        l1_state_eas l1_eas;
    }
    class buffer {    // typedef struct buffer {} buffer_t
        unsafe short* sbuffer;
        unsafe float* fbuffer;
	}

	class Multimon {
        demod_param demod_eas;  // extern const struct demod_param demod_eas;
    }
}
