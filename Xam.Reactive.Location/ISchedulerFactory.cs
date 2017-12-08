using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Text;

namespace Xam.Reactive
{
    public interface ISchedulerFactory
    {
        IScheduler CurrentThread { get; }
        IScheduler Dispatcher { get; }
        IScheduler Immediate { get; }
        IScheduler NewThread { get; }
        IScheduler TaskPool { get; }
    }
}
