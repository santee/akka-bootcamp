namespace WinTail
{
    using System;
    using System.IO;

    using Akka.Actor;

    public class FileObserver : IDisposable
    {
        private readonly IActorRef tailActor;

        private FileSystemWatcher watcher;
        private readonly string fileDir;
        private readonly string fileNameOnly;

        public FileObserver(IActorRef tailActor, string absoluteFilePath)
        {
            this.tailActor = tailActor;
            this.fileDir = Path.GetDirectoryName(absoluteFilePath);
            this.fileNameOnly = Path.GetFileName(absoluteFilePath);
        }

        /// <summary>
        /// Begin monitoring file.
        /// </summary>
        public void Start()
        {
            // Need this for Mono 3.12.0 workaround
            // uncomment next line if you're running on Mono!
            // Environment.SetEnvironmentVariable("MONO_MANAGED_WATCHER", "enabled");

            // make watcher to observe our specific file
            this.watcher = new FileSystemWatcher(this.fileDir, this.fileNameOnly);

            // watch our file for changes to the file name,
            // or new messages being written to file
            this.watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;

            // assign callbacks for event types
            this.watcher.Changed += this.OnFileChanged;
            this.watcher.Error += this.OnFileError;

            // start watching
            this.watcher.EnableRaisingEvents = true;
        }

        /// <summary>
        /// Stop monitoring file.
        /// </summary>
        public void Dispose()
        {
            this.watcher.Dispose();
        }

        /// <summary>
        /// Callback for <see cref="FileSystemWatcher"/> file error events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnFileError(object sender, ErrorEventArgs e)
        {
            this.tailActor.Tell(new TailActor.FileError(this.fileNameOnly,
                e.GetException().Message),
                ActorRefs.NoSender);
        }

        /// <summary>
        /// Callback for <see cref="FileSystemWatcher"/> file change events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Changed)
            {
                // here we use a special ActorRefs.NoSender
                // since this event can happen many times,
                // this is a little microoptimization
                this.tailActor.Tell(new TailActor.FileWrite(e.Name), ActorRefs.NoSender);
            }
        }
    }
}