namespace xwcs.core.evt
{
    interface IEvent
    {
        object sender { get; }
        object type { get; }
        object data { get; }
    }
}
