using ReacitveMvvm;
using System.Reactive.Concurrency;

namespace ProTaggerTest.Mocks
{
    class TestSchedulers : ISchedulers
    {
        public IScheduler Dispatcher => Scheduler.Immediate;
        public IScheduler ThreadPool => Scheduler.Immediate;
        public IScheduler Immediate => Scheduler.Immediate;
    }
}
