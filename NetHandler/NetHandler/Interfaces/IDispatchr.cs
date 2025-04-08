namespace NetHandler.Interfaces;

public interface IDispatchr
{
    Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}