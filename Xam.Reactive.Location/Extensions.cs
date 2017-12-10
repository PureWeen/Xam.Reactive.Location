using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;

namespace Xam.Reactive
{
    public static class Extensions
    {
        public static IObservable<T> CatchAndLog<T>(
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


        public static IObservable<T> CatchAndLog<T>(
            this IObservable<T> This, 
            IExceptionHandlerService ExceptionService,
            T handledResult)
        {

            return CatchAndLog<T>(This, ExceptionService, () => Observable.Return<T>(handledResult));
        }


        public static IObservable<T> CatchAndLog<T>(
            this IObservable<T> This,
            IExceptionHandlerService ExceptionService,
            IObservable<T> handledResult)
        {

            return CatchAndLog<T>(This, ExceptionService, () => handledResult);
        }


        public static void DisposeWith(this IDisposable This, CompositeDisposable disp)
        {
            disp.Add(This);
        }
    }
}
