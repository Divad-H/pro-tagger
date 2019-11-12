﻿using LibGit2Sharp;
using System;
using System.Collections.Generic;

namespace procom_tagger_test.LibGit2Mocks
{
    class CommitMock : Commit
    {
        public CommitMock(string message, string messageShort, Signature authorAndCommitter, IEnumerable<CommitMock> parents)
        {
            _message = message;
            _messageShort = messageShort;
            _authorAndCommitter = authorAndCommitter;
            _parents = parents;
        }

        public override TreeEntry this[string relativePath] => throw new NotImplementedException();

        private readonly string _message;
        public override string Message => _message;

        private readonly string _messageShort;
        public override string MessageShort => _messageShort;

        public override string Encoding => throw new NotImplementedException();

        private readonly Signature _authorAndCommitter;
        public override Signature Author => _authorAndCommitter;
        public override Signature Committer => _authorAndCommitter;

        public override Tree Tree => throw new NotImplementedException();

        private readonly IEnumerable<CommitMock> _parents;
        public override IEnumerable<Commit> Parents => _parents;

        public override IEnumerable<Note> Notes => throw new NotImplementedException();
    }
}
