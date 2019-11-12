using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Text;

namespace ReacitveMvvm
{
    public interface ISchedulers
    {
        IScheduler Dispatcher { get; }
    }
}
