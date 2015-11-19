namespace xwcs.core.plgs
{
    public interface IPlugin
    {
        string name { get; }
        void init(IPluginHost host);


        //Just for test
        void testFireEvent();
    }
}
