namespace xwcs.core.evt
{
    public interface IEvent
    {
        object Sender { get; }
        object Type { get; }
        object Data { get; }
    }
}
