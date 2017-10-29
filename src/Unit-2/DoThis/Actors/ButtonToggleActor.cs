namespace ChartApp.Actors
{
    using System.Windows.Forms;

    using Akka.Actor;

    public class ButtonToggleActor : UntypedActor
    {
        public class Toggle
        {
        }

        private readonly ChartingMessages.CounterType counterType;

        private bool isToggledOn;

        private readonly Button button;

        private readonly IActorRef coordinatorActor;

        public ButtonToggleActor(IActorRef coordinatorActor, Button button, ChartingMessages.CounterType counterType, bool isToggledOn = false)
        {
            this.coordinatorActor = coordinatorActor;
            this.button = button;
            this.counterType = counterType;
            this.isToggledOn = isToggledOn;
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case Toggle _ when this.isToggledOn:
                    this.coordinatorActor.Tell(new PerformanceCounterCoordinatorActor.Unwatch(this.counterType));
                    this.FlipToggle();
                    break;

                case Toggle _ when !this.isToggledOn:
                    this.coordinatorActor.Tell(new PerformanceCounterCoordinatorActor.Watch(this.counterType));
                    this.FlipToggle();
                    break;

                default:
                    this.Unhandled(message);
                    break;
            }
        }

        private void FlipToggle()
        {
            this.isToggledOn = !this.isToggledOn;

            var status = this.isToggledOn ? "ON" : "OFF";
            this.button.Text = $@"{this.counterType.ToString().ToUpperInvariant()} ({ status })";
        }
    }
}