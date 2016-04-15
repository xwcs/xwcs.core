namespace xwcs.core.evt
{
    interface IEvent
    {
        object Sender { get; }
        object Type { get; }
        object Data { get; }
    }
}
