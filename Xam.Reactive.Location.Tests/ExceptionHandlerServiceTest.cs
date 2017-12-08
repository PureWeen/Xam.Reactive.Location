using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Xam.Reactive.Tests
{
    public class ExceptionHandlerServiceTest
    {

        [Fact]
        public void ReturnsFalseWhenNothingIsSubscribed()
        {
            ExceptionHandlerService handlerService = new ExceptionHandlerService();
            Assert.False(handlerService.LogException(new Exception()));
        }


        [Fact]
        public void ReturnsTrueWhenSomethingIsSubscribed()
        {
            ExceptionHandlerService handlerService = new ExceptionHandlerService();
            var disp = handlerService.OnError.Subscribe();
            Assert.True(handlerService.LogException(new Exception()));
            disp.Dispose();
            Assert.False(handlerService.LogException(new Exception()));
        }



        [Fact]
        public void ExceptionPassedToObservable()
        {
            Exception exc = null;
            Exception excThrown = new Exception();
            ExceptionHandlerService handlerService = new ExceptionHandlerService();
            var disp = handlerService.OnError.Subscribe(e => exc = e);
            handlerService.LogException(excThrown);


            Assert.Equal(excThrown, exc);
        }
    }
}
