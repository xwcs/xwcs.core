namespace xwcs.core.evt
{
    /*
        NOTE:  MENU_tool_bar is toolbar under menu
    */
    public enum MenuDestination { MENU_file_open = 0, MENU_tools, MENU_about , MENU_tool_bar};

    public class MenuAddRequest
    {
        public MenuDestination destination { get; set; }
        public DevExpress.XtraBars.BarItem content { get; set; }
    }

    public class AddToolBarRequest
    {

        public MenuAddRequest[] content {get; set;}
       
        public AddToolBarRequest(MenuAddRequest[] content)
        {
            this.content = content;
        }
    }

    public class AddToolBarRequestEvent : Event
    {
        public AddToolBarRequestEvent(object sender, AddToolBarRequest requestData) : base(sender, EventType.AddToolBarRequestEvent, requestData)
        {
        }

        public AddToolBarRequest requestData
        {
            set { _data = value; }
            get { return (AddToolBarRequest) _data; }
        }
    }
}
