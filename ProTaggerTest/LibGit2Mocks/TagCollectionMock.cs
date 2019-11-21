using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProTaggerTest.LibGit2Mocks
{
    class TagCollectionMock : TagCollection
    {
        public TagCollectionMock(IList<TagMock> tags)
        {
            _tags = tags.ToDictionary(t => t.CanonicalName);
        }
        private readonly IDictionary<string, TagMock> _tags;
        public override IEnumerator<Tag> GetEnumerator()
        {
            return _tags
                .Select(keyVal => keyVal.Value)
                .GetEnumerator();
        }
    
        public override Tag this[string name] => _tags[name];

        public override Tag Add(string name, GitObject target)
        {
            throw new NotImplementedException();
        }
        public override Tag Add(string name, GitObject target, bool allowOverwrite)
        {
            throw new NotImplementedException();
        }
        public override Tag Add(string name, GitObject target, Signature tagger, string message)
        {
            throw new NotImplementedException();
        }
        public override Tag Add(string name, GitObject target, Signature tagger, string message, bool allowOverwrite)
        {
            throw new NotImplementedException();
        }
        public override Tag Add(string name, string objectish)
        {
            throw new NotImplementedException();
        }
        public override Tag Add(string name, string objectish, bool allowOverwrite)
        {
            throw new NotImplementedException();
        }
        public override Tag Add(string name, string objectish, Signature tagger, string message)
        {
            throw new NotImplementedException();
        }
        public override Tag Add(string name, string objectish, Signature tagger, string message, bool allowOverwrite)
        {
            throw new NotImplementedException();
        }
        public override void Remove(string name)
        {
            throw new NotImplementedException();
        }
        public override void Remove(Tag tag)
        {
            throw new NotImplementedException();
        }
    }
}
