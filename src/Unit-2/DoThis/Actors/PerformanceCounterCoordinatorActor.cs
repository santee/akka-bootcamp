namespace ChartApp.Actors
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.Windows.Forms.DataVisualization.Charting;

    using Akka.Actor;

    public class PerformanceCounterCoordinatorActor : ReceiveActor
    {
        public class Watch
        {
            public ChartingMessages.CounterType Counter { get; }

            public Watch(ChartingMessages.CounterType counter)
            {
                this.Counter = counter;
            }
        }

        public class Unwatch
        {
            public ChartingMessages.CounterType Counter { get; }

            public Unwatch(ChartingMessages.CounterType counter)
            {
                this.Counter = counter;
            }
        }

        private static readonly Dictionary<ChartingMessages.CounterType, Func<PerformanceCounter>>
            CounterGenerators = new Dictionary<ChartingMessages.CounterType, Func<PerformanceCounter>>()
                {
                    { ChartingMessages.CounterType.Cpu, () => new PerformanceCounter("Processor", "% Processor Time", "_Total", true)},
                    { ChartingMessages.CounterType.Memory, () => new PerformanceCounter("Memory", "% Committed Bytes in Use", true)},
                    { ChartingMessages.CounterType.Disk, () => new PerformanceCounter("LogicalDisk", "% Disk Time", "_Total", true)}
                };

        private static readonly Dictionary<ChartingMessages.CounterType, Func<Series>>
            CounterSeries = new Dictionary<ChartingMessages.CounterType, Func<Series>>()
                {
                    { ChartingMessages.CounterType.Cpu, () => new Series(ChartingMessages.CounterType.Cpu.ToString())
                        {
                            ChartType = SeriesChartType.SplineArea,
                            Color = Color.DarkGreen,
                        } },
                    { ChartingMessages.CounterType.Memory, () => new Series(ChartingMessages.CounterType.Memory.ToString())
                        {
                            ChartType = SeriesChartType.FastLine,
                            Color = Color.MediumBlue,
                        } },
                    { ChartingMessages.CounterType.Disk, () => new Series(ChartingMessages.CounterType.Disk.ToString())
                        {
                            ChartType = SeriesChartType.SplineArea,
                            Color = Color.DarkRed,
                        } },
                };

        private Dictionary<ChartingMessages.CounterType, IActorRef> counterActors;

        private IActorRef chartingActor;

        public PerformanceCounterCoordinatorActor(IActorRef chartingActor)
            :this(chartingActor, new Dictionary<ChartingMessages.CounterType, IActorRef>())
        {
        }

        public PerformanceCounterCoordinatorActor(IActorRef chartingActor, Dictionary<ChartingMessages.CounterType, IActorRef> counterActors)
        {
            this.counterActors = counterActors;
            this.chartingActor = chartingActor;

            this.Receive<Watch>(
                watch =>
                    {
                        if (!this.counterActors.ContainsKey(watch.Counter))
                        {
                            var counterActor = Context.ActorOf(
                                Props.Create(
                                    () => new PerformanceCounterActor(
                                        watch.Counter.ToString(),
                                        CounterGenerators[watch.Counter])));
                            this.counterActors[watch.Counter] = counterActor;
                        }

                        this.chartingActor.Tell(new ChartingActor.AddSeries(CounterSeries[watch.Counter]()));

                        this.counterActors[watch.Counter].Tell(new ChartingMessages.SubscribeCounter(watch.Counter, chartingActor));
                    });

            this.Receive<Unwatch>(
                unwatch =>
                    {
                        if (this.counterActors.ContainsKey(unwatch.Counter))
                        {
                            this.counterActors[unwatch.Counter].Tell(
                                new ChartingMessages.UnsubscribeCounter(unwatch.Counter, chartingActor));
                            this.chartingActor.Tell(new ChartingActor.RemoveSeries(unwatch.Counter.ToString()));
                        }
                    });
        }
    }
}