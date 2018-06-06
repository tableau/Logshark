namespace Logshark.ConnectionModel.Postgres
{
    public interface IPostgresUserInfo
    {
        string Username { get; }
        string Password { get; }
    }
}