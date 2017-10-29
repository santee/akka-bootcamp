using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Akka.Actor;
using Akka.Util.Internal;
using ChartApp.Actors;

namespace ChartApp
{
    using Akka.Dispatch;

    public partial class Main : Form
    {
        private IActorRef chartActor;
        private readonly AtomicCounter seriesCounter = new AtomicCounter(1);

        private IActorRef coordinatorActor;
        private Dictionary<ChartingMessages.CounterType, IActorRef> toggleActors = new Dictionary<ChartingMessages.CounterType, IActorRef>();


        public Main()
        {
            this.InitializeComponent();
        }

        #region Initialization


        private void Main_Load(object sender, EventArgs e)
        {
            this.chartActor = Program.ChartActors.ActorOf(Props.Create(() => new ChartingActor(this.sysChart)), "charting");

            this.chartActor.Tell(new ChartingActor.InitializeChart(null));

            this.coordinatorActor = Program.ChartActors.ActorOf(Props.Create(() => new PerformanceCounterCoordinatorActor(this.chartActor)), "counters");

            this.toggleActors[ChartingMessages.CounterType.Cpu] = Program.ChartActors.ActorOf(
                Props.Create(() => new ButtonToggleActor(this.coordinatorActor, this.btnCpu, ChartingMessages.CounterType.Cpu, false))
                    .WithDispatcher(Dispatchers.SynchronizedDispatcherId));

            this.toggleActors[ChartingMessages.CounterType.Memory] = Program.ChartActors.ActorOf(
                Props.Create(() => new ButtonToggleActor(this.coordinatorActor, this.btnMemory, ChartingMessages.CounterType.Memory, false))
                     .WithDispatcher(Dispatchers.SynchronizedDispatcherId));

            this.toggleActors[ChartingMessages.CounterType.Disk] = Program.ChartActors.ActorOf(
                Props.Create(() => new ButtonToggleActor(this.coordinatorActor, this.btnDisk, ChartingMessages.CounterType.Disk, false))
                     .WithDispatcher(Dispatchers.SynchronizedDispatcherId));

            this.toggleActors[ChartingMessages.CounterType.Cpu].Tell(new ButtonToggleActor.Toggle());
        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            //shut down the charting actor
            this.chartActor.Tell(PoisonPill.Instance);

            //shut down the ActorSystem
            Program.ChartActors.Terminate();
        }

        #endregion

        private void BtnCpuClick(object sender, EventArgs e)
        {
            this.toggleActors[ChartingMessages.CounterType.Cpu].Tell(new ButtonToggleActor.Toggle());
        }

        private void BtnMemoryClick(object sender, EventArgs e)
        {
            this.toggleActors[ChartingMessages.CounterType.Memory].Tell(new ButtonToggleActor.Toggle());
        }

        private void BtnDiskClick(object sender, EventArgs e)
        {
            this.toggleActors[ChartingMessages.CounterType.Disk].Tell(new ButtonToggleActor.Toggle());
        }
    }
}
