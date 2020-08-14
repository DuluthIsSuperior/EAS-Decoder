using System;
using System.Collections.Generic;
/*
 * This code was ported over from multimon-ng - https://github.com/EliasOenal/multimon-ng/
 * filter.h -- optimized filter routines
 *
 * Copyright (C) 1996  
 *		Thomas Sailer (sailer@ife.ee.ethz.ch, hb9jnx@hb9w.che.eu)
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

namespace EAS_Decoder {

	static class Filter {
		public static float mac2(float[] a, int start, float[] b, uint size) {
			int aIdx = start;
			int bIdx = 0;
			float sum = 0;
			for (int i = 0; i < size; i++) {
				sum += a[aIdx++] * b[bIdx++];
			}
			return sum;
		}

		public static float fsqr(float f) {
			return f * f;
		}
	}
}
