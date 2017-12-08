using System;
using System.Reactive.Linq;

namespace Xam.Reactive
{
    public class NullCheckPermissionProvider : ICheckPermissionProvider
    {
        public IObservable<bool> Location => Observable.Return(true);
    }
}