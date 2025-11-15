namespace Core.Server.UseCase;

public interface IUseCaseAsync<in TParams, TResult>
{
    Task<TResult> ExecuteAsync(TParams input);
}