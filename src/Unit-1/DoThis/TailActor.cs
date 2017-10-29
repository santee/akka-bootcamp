namespace WinTail
{
    using System.IO;
    using System.Text;

    using Akka.Actor;
    public class TailActor : UntypedActor
    {
        private readonly IActorRef reporterActor;

        private readonly string filePath;

        private FileObserver observer;

        private FileStream fileStream;

        private StreamReader fileStreamReader;

        public class FileWrite
        {
            public string FileName { get; }

            public FileWrite(string fileName)
            {
                this.FileName = fileName;
            }
        }

        public class FileError
        {
            public string FileName { get; }

            public string Reason { get; }

            public FileError(string fileName, string reason)
            {
                this.FileName = fileName;
                this.Reason = reason;
            }
        }

        public class InitialRead
        {
            public string FileName { get; }

            public string Text { get; }

            public InitialRead(string fileName, string text)
            {
                this.FileName = fileName;
                this.Text = text;
            }
        }

        public TailActor(IActorRef reporterActor, string filePath)
        {
            this.reporterActor = reporterActor;
            this.filePath = filePath;
            this.observer = new FileObserver(this.Self, Path.GetFullPath(this.filePath));
            this.observer.Start();

            this.fileStream = new FileStream(Path.GetFullPath(filePath), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            this.fileStreamReader = new StreamReader(this.fileStream, Encoding.UTF8);

            var text = this.fileStreamReader.ReadToEnd();
            this.Self.Tell(new InitialRead(filePath, text));
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case FileWrite msg:
                    var text = this.fileStreamReader.ReadToEnd();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        this.reporterActor.Tell(text);
                    }
                    break;

                case FileError error:
                    this.reporterActor.Tell($"Tail error: {error.Reason}");
                    break;

                case InitialRead read:
                    this.reporterActor.Tell(read.Text);
                    break;
            }
        }
    }
}