
using Graybox.Editor.Documents;

namespace Graybox.Editor.Actions
{
	public interface IAction : IDisposable
	{
		bool SkipInStack { get; }
		bool ModifiesState { get; }
		void Reverse( Document document );
		void Perform( Document document );
	}
}
