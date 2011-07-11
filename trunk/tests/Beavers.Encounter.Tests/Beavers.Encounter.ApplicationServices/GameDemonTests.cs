using System;
using System.Threading;
using NUnit.Framework;
using Rhino.Mocks;
using Beavers.Encounter.ApplicationServices;

namespace Tests.Beavers.Encounter.ApplicationServices
{
    [TestFixture]
    public class GameDemonTests
    {
        [Test]
        public void RecalcTest()
        {
            var moks = new MockRepository();
            var service = moks.DynamicMock<IRecalcGameStateService>();

            Expect.Call(service.GameId).Return(1).Repeat.Any();
            Expect.Call(() => service.RecalcGameState(DateTime.Now))
                .IgnoreArguments();

            moks.ReplayAll();

            GameDemon demon = GameDemon.GetInstance(service);
            demon.MinimalRecalcPeriod = 0;
            demon.RecalcGameState(null);

            moks.VerifyAll();
        }

        [Test]
        public void RecalcTwoTimesTest()
        {
            var moks = new MockRepository();
            var service = moks.DynamicMock<IRecalcGameStateService>();

            Expect.Call(service.GameId).Return(2).Repeat.Any();
            Expect.Call(() => service.RecalcGameState(DateTime.Now))
                .Repeat.Times(2)
                .IgnoreArguments();

            moks.ReplayAll();

            GameDemon demon = GameDemon.GetInstance(service);
            demon.MinimalRecalcPeriod = 0;
            demon.RecalcGameState(null);
            demon.RecalcGameState(null);

            moks.VerifyAll();
        }

        [Test]
        public void RecalcPeriodTest()
        {
            var moks = new MockRepository();
            var service = moks.DynamicMock<IRecalcGameStateService>();

            Expect.Call(service.GameId).Return(3).Repeat.Any();
            Expect.Call(() => service.RecalcGameState(DateTime.Now))
                .Repeat.Times(1)
                .IgnoreArguments();

            moks.ReplayAll();

            GameDemon demon = GameDemon.GetInstance(service);
            demon.MinimalRecalcPeriod = 1;
            demon.RecalcGameState(null);
            demon.RecalcGameState(null);

            moks.VerifyAll();
        }

        [Test]
        public void CanHandleExceptionTest()
        {
            var moks = new MockRepository();
            var service = moks.DynamicMock<IRecalcGameStateService>();

            Expect.Call(service.GameId).Return(4).Repeat.Any();
            Expect.Call(() => service.RecalcGameState(DateTime.Now))
                .Throw(new Exception("Error"))
                .IgnoreArguments();

            moks.ReplayAll();

            GameDemon demon = GameDemon.GetInstance(service);
            demon.MinimalRecalcPeriod = 0;
            demon.RecalcGameState(null);

            moks.VerifyAll();
        }

        [Test]
        public void CanLockConcurentThreadsTest()
        {
            var moks = new MockRepository();
            var service = moks.DynamicMock<IRecalcGameStateService>();

            Expect.Call(service.GameId).Return(5).Repeat.Any();

            var helper = new ConcurentThreadHelper();
            Expect.Call(() => service.RecalcGameState(DateTime.Now))
                .Do(helper.Action())
                .IgnoreArguments();

            moks.ReplayAll();

            GameDemon demon = GameDemon.GetInstance(service);
            demon.MinimalRecalcPeriod = 0;

            helper.Demon = demon;
            
            demon.RecalcGameState(null);

            moks.VerifyAll();
        }

        private class ConcurentThreadHelper
        {
            public GameDemon Demon;

            public Action<DateTime> Action()
            {
                return dateTime => Demon.RecalcGameState(DateTime.Now);
            }
        }

        [Test]
        public void CanStartStopTest()
        {
            var moks = new MockRepository();
            var service = moks.DynamicMock<IRecalcGameStateService>();

            Expect.Call(service.GameId).Return(6).Repeat.Any();
            Expect.Call(() => service.RecalcGameState(DateTime.Now))
                .IgnoreArguments();

            moks.ReplayAll();

            GameDemon demon = GameDemon.GetInstance(service);
            demon.MinimalRecalcPeriod = 0;
            demon.Start();
            Thread.Sleep(1000);
            demon.Stop();

            moks.VerifyAll();
        }
    }
}
