using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;

namespace Xam.Reactive
{
    internal static class Extensions
    {
        internal static IObservable<T> CatchAndLog<T>(
           this IObservable<T> This,
           IExceptionHandlerService ExceptionService,
           Func<IObservable<T>> handledResult)
        {

            return This.Catch((Exception exc) =>
            {
                if (!ExceptionService.LogException(exc))
                {
                    return Observable.Throw<T>(exc);
                }

                return handledResult();
            });
        }


        internal static IObservable<T> CatchAndLog<T>(
            this IObservable<T> This, 
            IExceptionHandlerService ExceptionService,
            T handledResult)
        {

            return CatchAndLog<T>(This, ExceptionService, () => Observable.Return<T>(handledResult));
        }


        internal static IObservable<T> CatchAndLog<T>(
            this IObservable<T> This,
            IExceptionHandlerService ExceptionService,
            IObservable<T> handledResult)
        {

            return CatchAndLog<T>(This, ExceptionService, () => handledResult);
        }


        internal static void DisposeWith(this IDisposable This, CompositeDisposable disp)
        {
            disp.Add(This);
        }

        internal static IObservable<T> Log<T>(this IObservable<T> This, string message)
        {
            return
                This.Do((value)=> Debug.WriteLine($"OnNext:{message}:{value}"),
                (e)=>
                {
                    Debug.WriteLine($"OnError:{message}:{e}");
                },
                () =>
                {
                     Debug.WriteLine($"OnCompleted:{message}");
                });
        }
    }
}
