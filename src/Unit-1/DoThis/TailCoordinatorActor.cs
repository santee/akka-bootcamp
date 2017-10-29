namespace WinTail
{
    using System;

    using Akka.Actor;
    public class TailCoordinatorActor : UntypedActor
    {
        public class StartTail
        {
            public string FilePath { get; }

            public IActorRef ReporterActor { get; }

            public StartTail(string filePath, IActorRef reporterActor)
            {
                this.FilePath = filePath;
                this.ReporterActor = reporterActor;
            }
        }

        public class StopTail
        {
            public string FilePath { get; }

            public StopTail(string filePath)
            {
                this.FilePath = filePath;
            }
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case StartTail startTail:
                    Context.ActorOf(Props.Create(() => new TailActor(startTail.ReporterActor, startTail.FilePath)));
                    break;
            }
        }

        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new OneForOneStrategy(
                10,
                TimeSpan.FromSeconds(30),
                x => Directive.Restart);
        }
    }
}