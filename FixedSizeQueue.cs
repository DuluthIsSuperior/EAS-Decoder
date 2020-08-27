using System;
using System.Collections.Concurrent;

namespace EAS_Decoder {
	public class FixedSizeQueue<T> : ConcurrentQueue<T> {
		private readonly object syncObject = new object();

		public uint Size { get; private set; }

		public FixedSizeQueue(uint size) {
			Size = size;
		}

		public new void Enqueue(T obj) {
			base.Enqueue(obj);
			lock (syncObject) {
				while (base.Count > Size) {
					T outObj;
					base.TryDequeue(out outObj);
				}
			}
		}
	}
}
