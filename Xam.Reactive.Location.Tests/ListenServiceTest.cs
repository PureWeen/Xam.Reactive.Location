using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Moq;
using System.Reactive.Linq;
using Microsoft.Reactive.Testing;
using Xam.Reactive.Tests;

namespace Xam.Reactive.Location.Tests
{
    public class ListenServiceTest
    {
        static IExceptionHandlerService NullExceptionService 
            => new Mock<IExceptionHandlerService>().Object;

        [Fact]
        public void CancelGetLocationAfterGivenTime()
        {
            TimeoutException exc = null;
            int timeToTimeOut = 10000;
            TestSchedulers scheduler = new TestSchedulers();
            Mock<ILocationListener> listener = new Mock<ILocationListener>();
            LocationService service =
                new LocationService(listener.Object, NullExceptionService, scheduler);

            listener.Setup(x => x.StartListeningForLocationChanges)
                .Returns(Observable.Never<LocationRecorded>());

            service
                .GetDeviceLocation(timeToTimeOut)
                .Subscribe(_=> Assert.True(false, "Subscription shouldn't have fired"),
                (Exception e) =>
                {
                    if (e is TimeoutException thrown)
                    {
                        exc = thrown;
                        return;
                    }

                    Assert.True(false, $"{e} thrown which was unexpected");
                });


            scheduler
                .TaskPool
                .AdvanceBy(TimeSpan.FromMilliseconds(timeToTimeOut).Ticks);

            Assert.NotNull(exc);
        }

        [Fact]
        public void PositionDataSetAfterGivenTime()
        {
            LocationRecorded positionExpected = new LocationRecorded();
            LocationRecorded positionReturned = null;
            int timeToTimeOut = 10000;
            Mock<ILocationListener> listener = new Mock<ILocationListener>();
            TestSchedulers scheduler = new TestSchedulers();
            LocationService service =
                new LocationService(listener.Object, NullExceptionService, scheduler);



            // return data after half of timeout
            listener
                .Setup(x => x.StartListeningForLocationChanges)
                .Returns(
                    Observable.Timer(TimeSpan.FromMilliseconds(timeToTimeOut / 2), scheduler.TaskPool)
                        .Select(_=> positionExpected)
                );



            service
                .GetDeviceLocation(timeToTimeOut)
                .Subscribe(pos => positionReturned = pos);

            // Advance far beyond the necessary time
            scheduler
                .TaskPool
                .AdvanceBy(TimeSpan.FromMilliseconds(timeToTimeOut * 2).Ticks);

            Assert.Equal(positionExpected, positionReturned);

        }





        [Fact]
        public void GetLastKnownLocationReturnsCorrectPosition()
        {
            int timeToTimeOut = 10000;
            LocationRecorded firstPosition = new LocationRecorded()
            {
                Recorded = DateTimeOffset.UtcNow.AddMilliseconds(-timeToTimeOut)
            };

            LocationRecorded secondPosition = new LocationRecorded()
            {
                Recorded = DateTimeOffset.UtcNow
            };

            LocationRecorded positionReturned = null;
            Mock<ILocationListener> listener = new Mock<ILocationListener>();
            TestSchedulers scheduler = new TestSchedulers();
            LocationService service =
                new LocationService(listener.Object, NullExceptionService, scheduler);

            listener
                .Setup(x => x.StartListeningForLocationChanges)
                .Returns(new[] { firstPosition, secondPosition }.ToObservable());


            service
                .GetLastKnownDeviceLocation(timeToTimeOut - 1, timeToTimeOut)
                .Subscribe(pos => positionReturned = pos);

            // Advance far beyond the necessary time
            scheduler
                .TaskPool
                .AdvanceBy(timeToTimeOut);

            Assert.Equal(secondPosition, positionReturned);

        }
    }
}
