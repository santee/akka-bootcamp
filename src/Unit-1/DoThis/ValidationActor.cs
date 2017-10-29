namespace WinTail
{
    using System;

    using Akka.Actor;

    public class ValidationActor : UntypedActor
    {
        private readonly IActorRef consoleWriterActor;

        public ValidationActor(IActorRef consoleWriterActor)
        {
            this.consoleWriterActor = consoleWriterActor;
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case string m when string.IsNullOrWhiteSpace(m):
                    this.consoleWriterActor.Tell(new Messages.NullInputError("No input received"));
                    break;

                case string m when this.IsValid(m):
                    this.consoleWriterActor.Tell(new Messages.InputSuccess("Thank you! Message was valid"));
                    break;

                default:
                    this.consoleWriterActor.Tell(new Messages.ValidationError("Invalid, input had odd number of characters"));
                    break;
            }

            this.Sender.Tell(new Messages.ContinueProcessing());
        }

        private bool IsValid(string message)
        {
            var valid = message.Length % 2 == 0;
            return valid;
        }
    }
}