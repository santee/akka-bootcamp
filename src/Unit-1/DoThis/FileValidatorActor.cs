namespace WinTail
{
    using System.IO;

    using Akka.Actor;

    public class FileValidatorActor : UntypedActor
    {
        private readonly IActorRef consoleWriterActor;

        private readonly IActorRef tailCoordinatorActor;

        public FileValidatorActor(IActorRef consoleWriterActor, IActorRef tailCoordinatorActor)
        {
            this.consoleWriterActor = consoleWriterActor;
            this.tailCoordinatorActor = tailCoordinatorActor;
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case string m when string.IsNullOrWhiteSpace(m):
                    this.consoleWriterActor.Tell(new Messages.NullInputError("Input was blank, please try again \n"));
                    this.Sender.Tell(new Messages.ContinueProcessing());
                    break;
                case string m when this.IsFileUri(m):
                    this.consoleWriterActor.Tell(new Messages.InputSuccess($"Starting processing for {m}"));
                    this.tailCoordinatorActor.Tell(new TailCoordinatorActor.StartTail(m, this.consoleWriterActor));
                    break;
                default:
                    this.consoleWriterActor.Tell(new Messages.ValidationError($"{message} is not an existing URI on disk"));
                    this.Sender.Tell(new Messages.ContinueProcessing());
                    break;
            }
        }

        private bool IsFileUri(string uri)
        {
            return File.Exists(uri);
        }
    }
}