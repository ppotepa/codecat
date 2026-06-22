namespace Codecat.Scanning;

public interface IErrorHandler<TError>
{
    void Handle(TError error, string context);
}
