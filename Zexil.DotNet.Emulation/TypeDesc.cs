using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Zexil.DotNet.Emulation {
	/// <summary>
	/// Runtime type
	/// </summary>
	public sealed class TypeDesc {
		private readonly Type _internalValue;
		private readonly List<FieldDesc> _fields;
		private readonly List<FieldDesc> _staticFields;

		/// <summary>
		/// Internal value
		/// </summary>
		public Type InternalValue => _internalValue;

		/// <summary>
		/// All instance fields defined in <see cref="Type"/> .
		/// </summary>
		public IEnumerable<FieldDesc> Fields => _fields;

		/// <summary>
		/// All static fields defined in <see cref="Type"/> .
		/// </summary>
		public IEnumerable<FieldDesc> StaticFields => _staticFields;

		internal TypeDesc(ExecutionEngine runtime, Type type) {
			_internalValue = type;
			runtime.AddType(this);
			_fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly).Select(t => new FieldDesc(runtime, t)).ToList();
			_fields.Sort((x, y) => (int)(x.Offset - y.Offset));
			_staticFields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly).Select(t => new FieldDesc(runtime, t)).ToList();
		}

		/// <inheritdoc/>
		public override string ToString() {
			return _internalValue.ToString();
		}
	}
}
