namespace WinSub
{
    public class EventArgs<T> : System.EventArgs
    {
        public T Value { get; private set; }
        public EventArgs(T value) { Value = value; }
    }
}
