using System.Collections.Concurrent;
using NetHandler.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace NetHandler
{
    public class Dispatchr : IDispatchr
    {
        private readonly IServiceProvider _serviceProvider;
        
        private static readonly ConcurrentDictionary<Type, Func<object, object, CancellationToken, Task<object>>> _handlerCache =
            new ConcurrentDictionary<Type, Func<object, object, CancellationToken, Task<object>>>();

        public Dispatchr(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

        public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            var handlerType = typeof(IRequestHandler<,>).MakeGenericType(request.GetType(), typeof(TResponse));

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

            var invoker = _handlerCache.GetOrAdd(handler.GetType(), CreateHandlerInvoker);
            var result = await invoker(handler, request, cancellationToken);
            return (TResponse)result;
        }
        
        private static Func<object, object, CancellationToken, Task<object>> CreateHandlerInvoker(Type handlerType)
        {
            var handlerInterface = handlerType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));

            if (handlerInterface == null)
                throw new InvalidOperationException("O handler não implementa IRequestHandler<TRequest, TResponse>.");

            var requestType = handlerInterface.GetGenericArguments()[0];

            // Procura o método "Handle" com os parâmetros (TRequest, CancellationToken).
            var methodInfo = handlerType.GetMethod("Handle", new[] { requestType, typeof(CancellationToken) });
            if (methodInfo == null)
            {
                throw new InvalidOperationException("O handler não implementa o método Handle com a assinatura esperada.");
            }

            return async (handler, request, cancellationToken) =>
            {
                var task = (Task)methodInfo.Invoke(handler, new object[] { request, cancellationToken });
                await task.ConfigureAwait(false);
                var taskType = task.GetType();
                var resultProperty = taskType.GetProperty("Result");
                return resultProperty != null ? resultProperty.GetValue(task) : null;
            };
        }
        
        public async Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
           
            var handlers = _serviceProvider.GetServices<INotificationHandler<TNotification>>();
            if (handlers == null || !handlers.Any())
                return;
            
            foreach (var handler in handlers)
            {
                await handler.Handle(notification, cancellationToken);
            }
        }
    }
}
