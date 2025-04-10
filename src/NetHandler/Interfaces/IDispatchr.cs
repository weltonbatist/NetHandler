namespace NetHandler.Interfaces;

public interface IDispatchr
{
    Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
    Task PublishAsync<TNotification>(TNotification notification, bool isParallel = false, CancellationToken cancellationToken = default)
        where TNotification : INotification;
}