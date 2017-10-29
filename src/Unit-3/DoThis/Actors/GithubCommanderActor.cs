using System;
using System.Linq;
using Akka.Actor;
using Akka.Routing;

namespace GithubActors.Actors
{
    /// <summary>
    /// Top-level actor responsible for coordinating and launching repo-processing jobs
    /// </summary>
    public class GithubCommanderActor : ReceiveActor, IWithUnboundedStash
    {
        #region Message classes

        public class CanAcceptJob
        {
            public CanAcceptJob(RepoKey repo)
            {
                Repo = repo;
            }

            public RepoKey Repo { get; private set; }
        }

        public class AbleToAcceptJob
        {
            public AbleToAcceptJob(RepoKey repo)
            {
                Repo = repo;
            }

            public RepoKey Repo { get; private set; }
        }

        public class UnableToAcceptJob
        {
            public UnableToAcceptJob(RepoKey repo)
            {
                Repo = repo;
            }

            public RepoKey Repo { get; private set; }
        }

        #endregion

        private IActorRef coordinator;
        private IActorRef canAcceptJobSender;

        private RepoKey repoJob;

        private int pendingJobReplies;

        public GithubCommanderActor()
        {
            this.Ready();
        }

        private void Ready()
        {
            this.Receive<CanAcceptJob>(job =>
                                      {
                                          
                                          this.coordinator.Tell(job);
                                          this.repoJob = job.Repo;
                                          this.BecomeAsking();
                                      });
        }

        private void BecomeAsking()
        {
            this.canAcceptJobSender = this.Sender;
            this.pendingJobReplies = this.coordinator.Ask<Routees>(new GetRoutees()).Result.Members.Count();
            this.Become(this.Asking);

            Context.SetReceiveTimeout(TimeSpan.FromSeconds(3));
        }

        private void Asking()
        {
            this.Receive<CanAcceptJob>(job => this.Stash.Stash());

            this.Receive<UnableToAcceptJob>(job =>
                                                {
                                                    this.pendingJobReplies--;
                                                    if (this.pendingJobReplies == 0)
                                                    {
                                                        this.canAcceptJobSender.Tell(job);
                                                        this.BecomeReady();
                                                    }
                                                });

            this.Receive<AbleToAcceptJob>(job =>
                                         {
                                             this.canAcceptJobSender.Tell(job);

                                             //start processing messages
                                             this.Sender.Tell(new GithubCoordinatorActor.BeginJob(job.Repo));

                                             //launch the new window to view results of the processing
                                             Context.ActorSelection(ActorPaths.MainFormActor.Path).Tell(new MainFormActor.LaunchRepoResultsWindow(job.Repo, Sender));

                                             this.BecomeReady();
                                         });

            this.Receive<ReceiveTimeout>(
                timeout =>
                    {
                        this.canAcceptJobSender.Tell(new UnableToAcceptJob(this.repoJob));
                        this.BecomeReady();
                    });
        }

        private void BecomeReady()
        {
            this.Become(this.Ready);
            this.Stash.UnstashAll();

            Context.SetReceiveTimeout(null);
        }

        protected override void PreStart()
        {
            // create a broadcast router who will ask all of them 
            // if they're available for work
            this.coordinator =
                Context.ActorOf(Props.Create(() => new GithubCoordinatorActor()).WithRouter(FromConfig.Instance), ActorPaths.GithubCoordinatorActor.Name);
            base.PreStart();
        }

        protected override void PreRestart(Exception reason, object message)
        {
            //kill off the old coordinator so we can recreate it from scratch
            this.coordinator.Tell(PoisonPill.Instance);
            base.PreRestart(reason, message);
        }

        public IStash Stash { get; set; }
    }
}
