namespace NetHandler.Interfaces;

// INTERFACE PARA O HANDLER QUE PROCESSA UMA REQUEST DO TIPO TRequest E RETORNA TResponse
public interface IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken cancellation);
}