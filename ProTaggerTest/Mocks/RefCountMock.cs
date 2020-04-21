using ProTagger.Utilities;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Text;

namespace ProTaggerTest.Mocks
{
    class RefCountMock : IRefCount
    {
        public IDisposable AddRef()
            => Disposable.Empty;

        public IDisposable? TryAddRef()
            => Disposable.Empty;

        public void Dispose()
        {}
    }
}
