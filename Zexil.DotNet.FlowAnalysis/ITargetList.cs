using System.Collections.Generic;

namespace Zexil.DotNet.FlowAnalysis {
	/// <summary>
	/// Switch target list
	/// </summary>
	public interface ITargetList : IList<IBasicBlock> {
		/// <summary>
		/// Owner of current instance
		/// </summary>
		IBasicBlock? Owner { get; set; }
	}
}
