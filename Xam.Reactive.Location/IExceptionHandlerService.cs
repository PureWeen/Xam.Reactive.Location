using System;

namespace Xam.Reactive
{
    public interface IExceptionHandlerService
    {
        IObservable<Exception> OnError { get; }

        bool LogException(Exception exc);
    }
}