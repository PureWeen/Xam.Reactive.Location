using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Text;
using Xamarin.DispatchScheduler;

namespace Xam.Reactive
{
    public class XamarinSchedulerFactory : ISchedulerFactory
    {
        public IScheduler CurrentThread => CurrentThreadScheduler.Instance;
        public IScheduler Dispatcher => XamarinDispatcherScheduler.Current;
        public IScheduler Immediate => ImmediateScheduler.Instance;
        public IScheduler NewThread => NewThreadScheduler.Default;
        public IScheduler TaskPool => TaskPoolScheduler.Default;
    }
}
