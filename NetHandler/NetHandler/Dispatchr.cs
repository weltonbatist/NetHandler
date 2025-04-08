using System.Collections.Concurrent;
using NetHandler.Interfaces;

namespace NetHandler;

public class Dispatchr : IDispatchr
{
    private readonly IServiceProvider _serviceProvider;
    // Cache para armazenar os delegates que invocam o método Handle de cada handler.
    private static readonly ConcurrentDictionary<Type, Func<object, object, CancellationToken, Task<object>>> _handlerCache =
        new ConcurrentDictionary<Type, Func<object, object, CancellationToken, Task<object>>>();

    public Dispatchr(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(request.GetType(), typeof(TResponse));
        // Retorna o handler como objeto (evitando o dynamic aqui)
        var handler = _serviceProvider.GetService(handlerType);
        if (handler == null)
        {
            throw new InvalidOperationException(
                $"Nenhum handler registrado para o tipo '{request.GetType().Name}'. " +
                $"Certifique-se de que um handler implementando " +
                $"IRequestHandler<{request.GetType().Name}, {typeof(TResponse).Name}> " +
                $"esteja registrado no container de injeção de dependência."
            );
        }

        // Aqui garantimos que usamos a versão fortemente tipada para obter o tipo do handler
        var invoker = _handlerCache.GetOrAdd(handler.GetType(), CreateHandlerInvoker);
        var result = await invoker(handler, request, cancellationToken);
        return (TResponse)result;
    }

    // Cria um delegate que encapsula a chamada do método Handle do handler, evitando o uso repetido de reflection.
    private static Func<object, object, CancellationToken, Task<object>> CreateHandlerInvoker(Type handlerType)
    {
        // Busca a interface IRequestHandler<TRequest, TResponse> implementada pelo handler.
        var handlerInterface = handlerType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));

        if (handlerInterface == null)
            throw new InvalidOperationException("O handler não implementa IRequestHandler<TRequest, TResponse>.");

        // Extrai o tipo do request (TRequest) a partir dos argumentos genéricos da interface.
        var requestType = handlerInterface.GetGenericArguments()[0];

        // Procura o método "Handle" com os parâmetros (TRequest, CancellationToken)
        var methodInfo = handlerType.GetMethod("Handle", new[] { requestType, typeof(CancellationToken) });
        if (methodInfo == null)
        {
            throw new InvalidOperationException("O handler não implementa o método Handle com a assinatura esperada.");
        }

        // Cria e retorna um delegate que invoca o método Handle via reflection.
        return async (handler, request, cancellationToken) =>
        {
            // Invoca o método e aguarda a Task retornada.
            var task = (Task)methodInfo.Invoke(handler, new object[] { request, cancellationToken });
            await task.ConfigureAwait(false);

            // Se for Task<T>, acessa a propriedade "Result" para obter o valor de retorno.
            var taskType = task.GetType();
            var resultProperty = taskType.GetProperty("Result");
            return resultProperty != null ? resultProperty.GetValue(task) : null;
        };
    }
}