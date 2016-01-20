namespace xwcs.core.user
{
    public interface IUser
    {
        string Name { get; }
        string Login { get; }
        bool isAdmin();
    }
}

