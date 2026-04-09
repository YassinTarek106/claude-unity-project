using NUnit.Framework;
using YassinTarek.SimonSays.Core.EventBus;

namespace YassinTarek.Tests.EditMode
{
    public sealed class EventBusTests
    {
        private EventBus _bus;

        [SetUp]
        public void SetUp() => _bus = new EventBus();

        private struct TestEvent
        {
            public int Value;
        }

        private struct OtherEvent { }

        [Test]
        public void Subscribe_ReceivesPublishedEvent()
        {
            var received = false;
            void Handler(TestEvent _) => received = true;

            _bus.Subscribe<TestEvent>(Handler);
            _bus.Publish(new TestEvent());

            Assert.IsTrue(received);
        }

        [Test]
        public void Unsubscribe_StopsReceivingEvents()
        {
            var count = 0;
            void Handler(TestEvent _) => count++;

            _bus.Subscribe<TestEvent>(Handler);
            _bus.Unsubscribe<TestEvent>(Handler);
            _bus.Publish(new TestEvent());

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Publish_WithNoSubscribers_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _bus.Publish(new TestEvent()));
        }

        [Test]
        public void Subscribe_MultipleHandlers_AllReceive()
        {
            var countA = 0;
            var countB = 0;
            void HandlerA(TestEvent _) => countA++;
            void HandlerB(TestEvent _) => countB++;

            _bus.Subscribe<TestEvent>(HandlerA);
            _bus.Subscribe<TestEvent>(HandlerB);
            _bus.Publish(new TestEvent());

            Assert.AreEqual(1, countA);
            Assert.AreEqual(1, countB);
        }

        [Test]
        public void Publish_PassesPayloadCorrectly()
        {
            var received = -1;
            void Handler(TestEvent evt) => received = evt.Value;

            _bus.Subscribe<TestEvent>(Handler);
            _bus.Publish(new TestEvent { Value = 42 });

            Assert.AreEqual(42, received);
        }

        [Test]
        public void Publish_DifferentEventTypes_DoNotCross()
        {
            var testEventFired = false;
            var otherEventFired = false;
            void TestHandler(TestEvent _) => testEventFired = true;
            void OtherHandler(OtherEvent _) => otherEventFired = true;

            _bus.Subscribe<TestEvent>(TestHandler);
            _bus.Subscribe<OtherEvent>(OtherHandler);
            _bus.Publish(new TestEvent());

            Assert.IsTrue(testEventFired);
            Assert.IsFalse(otherEventFired);
        }

        [Test]
        public void Unsubscribe_DuringDispatch_DoesNotThrow()
        {
            void Handler(TestEvent _) => _bus.Unsubscribe<TestEvent>(Handler);

            _bus.Subscribe<TestEvent>(Handler);
            Assert.DoesNotThrow(() => _bus.Publish(new TestEvent()));
        }
    }
}
