using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;

namespace Xam.Reactive
{
    public class ExceptionHandlerService : IExceptionHandlerService
    {
        private Subject<Exception> _onErrorSubj = new Subject<Exception>();
        bool _someoneIsWatchingErrors = false;

        private IObservable<Exception> _onError;
        public ExceptionHandlerService()
        {
            _onError = Observable.Create<Exception>(subj =>
            {
                CompositeDisposable disp = new CompositeDisposable();
                disp.Add(_onErrorSubj.Subscribe(subj));
                _someoneIsWatchingErrors = true;

                disp.Add(
                    Disposable.Create(() =>
                    {
                        _someoneIsWatchingErrors = false;
                    })
                );


                return disp;
            })
            .Publish()
            .RefCount();
        }

        public IObservable<Exception> OnError
        {
            get
            {
                return _onError;
            }
        }


        public bool LogException(Exception exc)
        {
            if (_someoneIsWatchingErrors)
            {
                _onErrorSubj.OnNext(exc);
                return true;
            }

            return false;
        }

    }
}
