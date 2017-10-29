using System;
using Akka.Actor;

namespace WinTail
{
    /// <summary>
    /// Actor responsible for reading FROM the console. 
    /// Also responsible for calling <see cref="ActorSystem.Terminate"/>.
    /// </summary>
    internal class ConsoleReaderActor : UntypedActor
    {
        public const string ExitCommand = "exit";

        public const string StartCommand = "start";

        private readonly IActorRef validationActor;

        public ConsoleReaderActor(IActorRef validationActor)
        {
            this.validationActor = validationActor;
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case StartCommand:
                    this.DoPrintInstructions();
                    break;
            }

            this.GetAndValidateInput();
        }

        private void GetAndValidateInput()
        {
            switch (Console.ReadLine())
            {
                case var m when string.Equals(m, ExitCommand, StringComparison.OrdinalIgnoreCase):
                    Context.System.Terminate();
                    break;
                case var m:
                    this.validationActor.Tell(m);
                    break;
            }
        }

        private void DoPrintInstructions()
        {
            Console.WriteLine("Please provide the URI of a log file on disk.\n");
        }
    }
}