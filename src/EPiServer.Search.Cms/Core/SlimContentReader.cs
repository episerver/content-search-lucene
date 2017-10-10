using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace EPiServer.Core.Internal
{
    /// <summary>
    /// Traverses all content in the tree.
    /// </summary>
    internal class SlimContentReader
    {
        private IContentRepository _contentRepository;
        private Stack<ContentReference> _backlog = new Stack<ContentReference>();
        private Queue<IContent> _queue = new Queue<IContent>();
        private Func<IContent, bool> _traverseChildren;

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
                bool traverseChildren = true;
                ContentReference currentReference = _backlog.Pop();
                foreach (IContent item in _contentRepository.GetLanguageBranches<IContent>(currentReference))
                {
                    traverseChildren &= _traverseChildren(item);
                    _queue.Enqueue(item);
                }

                if (traverseChildren)
                {
                    IContent[] children = _contentRepository.GetChildren<IContent>(currentReference, CultureInfo.InvariantCulture).ToArray();
                    for (int i = children.Length; i > 0; i--)
                    {
                        ContentReference childReference = new ContentReference(children[i - 1].ContentLink.ID, children[i - 1].ContentLink.ProviderName);
                        _backlog.Push(childReference);
                    }
                }
            }

            Current = _queue.Dequeue();
            return true;
        }

    }
}
