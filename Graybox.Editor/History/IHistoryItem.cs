using Graybox.Editor.Documents;
using System;

namespace Graybox.Editor.History
{
    public interface IHistoryItem : IDisposable
    {
        string Name { get; }
        bool SkipInStack { get; }
        bool ModifiesState { get; }
        void Undo(Document document);
        void Redo(Document document);
    }
}
