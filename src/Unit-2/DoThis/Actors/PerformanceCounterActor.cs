namespace ChartApp.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    using Akka.Actor;
    public class PerformanceCounterActor : UntypedActor
    {
        private readonly string seriesName;

        private readonly Func<PerformanceCounter> performanceCounterGenerator;

        private PerformanceCounter counter;

        private readonly HashSet<IActorRef> subscriptions = new HashSet<IActorRef>();

        private readonly ICancelable cancelPublishing = new Cancelable(Context.System.Scheduler);

        public PerformanceCounterActor(string seriesName, Func<PerformanceCounter> performanceCounterGenerator)
        {
            this.seriesName = seriesName;
            this.performanceCounterGenerator = performanceCounterGenerator;
        }

        protected override void PreStart()
        {
            this.counter = this.performanceCounterGenerator();
            Context.System.Scheduler.ScheduleTellRepeatedly(
                TimeSpan.FromMilliseconds(250),
                TimeSpan.FromMilliseconds(250),
                this.Self,
                new ChartingMessages.GatherMetrics(),
                this.Sender,
                this.cancelPublishing );
        }

        protected override void PostStop()
        {
            try
            {
                this.cancelPublishing.Cancel(false);
                this.counter.Dispose();
            }
            catch
            {
                // ignored
            }
            finally
            {
                base.PostStop();
            }
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case ChartingMessages.GatherMetrics _:
                    var metric = new ChartingMessages.Metric(this.seriesName, this.counter.NextValue());
                    foreach (var subscription in this.subscriptions)
                    {
                        subscription.Tell(metric);
                    }
                    break;

                case ChartingMessages.SubscribeCounter sc:
                    this.subscriptions.Add(sc.Subscriber);
                    break;

                case ChartingMessages.UnsubscribeCounter sc:
                    this.subscriptions.Remove(sc.Subscriber);
                    break;
            }
        }
    }
}