using System.Reactive.Concurrency;

namespace ReacitveMvvm
{
    public interface ISchedulers
    {
        IScheduler Dispatcher { get; }
    }
}
