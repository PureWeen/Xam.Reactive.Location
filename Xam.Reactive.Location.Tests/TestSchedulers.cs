using Microsoft.Reactive.Testing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading.Tasks;


/// <summary>
/// Motivation for this found here
/// http://www.introtorx.com/content/v1.0.10621.0/16_TestingRx.html#SchedulerDI
/// </summary>
namespace Xam.Reactive.Tests
{

    public sealed class TestSchedulers : ISchedulerFactory
    {
        private readonly TestScheduler _currentThread = new TestScheduler();
        private readonly TestScheduler _dispatcher = new TestScheduler();
        private readonly TestScheduler _immediate = new TestScheduler();
        private readonly TestScheduler _newThread = new TestScheduler();
        private readonly TestScheduler _taskPool = new TestScheduler();

        #region Explicit implementation of ISchedulerFactory
        IScheduler ISchedulerFactory.CurrentThread { get { return _currentThread; } }
        IScheduler ISchedulerFactory.Dispatcher { get { return _dispatcher; } }
        IScheduler ISchedulerFactory.Immediate { get { return _immediate; } }
        IScheduler ISchedulerFactory.NewThread { get { return _newThread; } }
        IScheduler ISchedulerFactory.TaskPool { get { return _taskPool; } }
        #endregion

        public TestScheduler CurrentThread { get { return _currentThread; } }
        public TestScheduler Dispatcher { get { return _dispatcher; } }
        public TestScheduler Immediate { get { return _immediate; } }
        public TestScheduler NewThread { get { return _newThread; } }
        public TestScheduler TaskPool { get { return _taskPool; } }
    }

}
