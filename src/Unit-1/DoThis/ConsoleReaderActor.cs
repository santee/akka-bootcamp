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

        private readonly IActorRef consoleWriterActor;

        public ConsoleReaderActor(IActorRef consoleWriterActor)
        {
            this.consoleWriterActor = consoleWriterActor;
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case StartCommand:
                    this.DoPrintInstructions();
                    break;
                case Messages.InputError error:
                    this.consoleWriterActor.Tell(error);
                    break;
            }

            this.GetAndValidateInput();
        }

        private void GetAndValidateInput()
        {
            switch (Console.ReadLine())
            {
                case var m when string.IsNullOrWhiteSpace(m):
                    this.Self.Tell(new Messages.NullInputError("No input received"));
                    break;

                case var m when string.Equals(m, ExitCommand, StringComparison.OrdinalIgnoreCase):
                    Context.System.Terminate();
                    break;

                case var m when this.IsValid(m):
                    this.consoleWriterActor.Tell(new Messages.InputSuccess("Thank you! Message was valid"));
                    this.Self.Tell(new Messages.ContinueProcessing());
                    break;
                default:
                    this.Self.Tell(new Messages.ValidationError("Invalid, input had odd number of characters"));
                    break;
            }
        }

        private bool IsValid(string message)
        {
            var valid = message.Length % 2 == 0;
            return valid;
        }

        private void DoPrintInstructions()
        {
            Console.WriteLine("Write whatever you want into the console!");
            Console.WriteLine("Some entries will pass validation, and some won't...\n\n");
            Console.WriteLine("Type 'exit' to quit this application at any time.\n");
        }
    }
}