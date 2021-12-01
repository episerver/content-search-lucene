using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace EPiServer.Core.Internal
{
    /// <summary>
    /// Traverses all content in the tree.
    /// </summary>
    internal class SlimContentReader
    {
        private readonly IContentRepository _contentRepository;
        private readonly Stack<ContentReference> _backlog = new Stack<ContentReference>();
        private readonly Queue<IContent> _queue = new Queue<IContent>();
        private readonly Func<IContent, bool> _traverseChildren;

        public SlimContentReader(IContentRepository contentRepository, ContentReference start)
            : this(contentRepository, start, c => true)
        {
        }

        public SlimContentReader(IContentRepository contentRepository, ContentReference start, Func<IContent, bool> traverseChildren)
        {
            _contentRepository = contentRepository;
            _backlog.Push(start);
            _traverseChildren = traverseChildren;
        }

        public IContent Current
        {
            get;
            private set;
        }

        public bool Next()
        {
            if (_backlog.Count == 0 && _queue.Count == 0)
            {
                return false;
            }

            if (_queue.Count == 0)
            {
                var traverseChildren = true;
                var currentReference = _backlog.Pop();
                foreach (var item in _contentRepository.GetLanguageBranches<IContent>(currentReference))
                {
                    traverseChildren &= _traverseChildren(item);
                    _queue.Enqueue(item);
                }

                if (traverseChildren)
                {
                    var children = _contentRepository.GetChildren<IContent>(currentReference, CultureInfo.InvariantCulture).ToArray();
                    for (var i = children.Length; i > 0; i--)
                    {
                        var childReference = new ContentReference(children[i - 1].ContentLink.ID, children[i - 1].ContentLink.ProviderName);
                        _backlog.Push(childReference);
                    }
                }
            }

            Current = _queue.Dequeue();
            return true;
        }

    }
}
