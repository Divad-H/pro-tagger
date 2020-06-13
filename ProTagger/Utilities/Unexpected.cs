namespace ProTagger.Utilities
{
    public class Unexpected
    {
        public string Message { get; }

        public Unexpected(string message)
            => Message = message;
    }
}
