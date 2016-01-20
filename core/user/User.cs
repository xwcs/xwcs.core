namespace xwcs.core.user
{
    public class User : IUser
    {
        //Private
        private string _name = string.Empty;
        private string _login = string.Empty;

        public string Name
        {
            get
            {
                return _name;
            }

            set
            {
                _name = value;
            }
        }

        public string Login
        {
            get
            {
                return _login;
            }

            set
            {
                _login = value;
            }
        }


        //Public
        public bool isAdmin()
        {
            return true;
        }
    }
}

