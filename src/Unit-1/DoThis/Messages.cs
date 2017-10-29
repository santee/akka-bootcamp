namespace WinTail
{
    public static class Messages
    {
        public class ContinueProcessing
        {
        }

        public class InputSuccess
        {
            public string Reason { get; }

            public InputSuccess(string reason)
            {
                this.Reason = reason;
            }
        }

        public class InputError
        {
            public string Reason { get; }

            public InputError(string reason)
            {
                this.Reason = reason;
            }
        }

        public class NullInputError : InputError
        {
            public NullInputError(string reason)
                : base(reason)
            {
            }
        }

        public class ValidationError : InputError
        {
            public ValidationError(string reason)
                : base(reason)
            {
            }
        }
    }
}