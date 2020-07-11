namespace Zexil.DotNet.FlowAnalysis {
	/// <summary>
	/// Block handler
	/// </summary>
	/// <param name="block"></param>
	/// <returns></returns>
	public delegate bool BlockHandler(Block block);

	/// <summary>
	/// Block handler
	/// </summary>
	/// <typeparam name="TBlock"></typeparam>
	/// <param name="block"></param>
	/// <returns></returns>
	public delegate bool BlockHandler<TBlock>(TBlock block) where TBlock : Block;
}
