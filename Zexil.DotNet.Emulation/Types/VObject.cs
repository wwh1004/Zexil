namespace Zexil.DotNet.Emulation.Types {
	public class VObject {
		public T Default<T>() where T : VObject {
			return (T)Default();
		}

		public virtual VObject Default() {
			return new VObject();
		}

		public T Duplicate<T>() where T : VObject {
			return (T)Duplicate();
		}

		public virtual VObject Duplicate() {
			return new VObject();
		}
	}
}
