using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using NetHandler.Interfaces;

namespace NetHandler.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registra o Dispatchr e todos os IRequestHandler encontrados nos assemblies informados.
        /// </summary>
        /// <param name="services">O ServiceCollection onde os serviços serão registrados.</param>
        /// <param name="assemblies">Os assemblies onde serão procurados os handlers.</param>
        /// <returns>O mesmo ServiceCollection para encadeamento de métodos.</returns>
        public static IServiceCollection AddNetHandler(this IServiceCollection services, params Assembly[] assemblies)
        {
            services.AddSingleton<IDispatchr, Dispatchr>();

            // Obtém todos os tipos que implementam IRequestHandler<,> dos assemblies informados.
            var handlerInterfaceType = typeof(IRequestHandler<,>);
            var handlerTypes = assemblies
                .SelectMany(a => a.GetExportedTypes())
                .Where(t => t.IsClass && !t.IsAbstract)
                .Select(t => new 
                { 
                    Type = t, 
                    Interfaces = t.GetInterfaces()
                        .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterfaceType)
                })
                .Where(x => x.Interfaces.Any());

            // Registra cada handler no container de DI
            foreach (var handler in handlerTypes)
            {
                foreach (var handlerInterface in handler.Interfaces)
                {
                    services.AddTransient(handlerInterface, handler.Type);
                }
            }

            return services;
        }
        
        /// <summary>
        /// Registra o Dispatchr e os handlers explicitamente.
        /// </summary>
        /// <param name="services">O ServiceCollection onde os serviços serão registrados.</param>
        /// <param name="handlerTypes">Os tipos dos handlers a serem registrados.</param>
        /// <returns>O mesmo ServiceCollection para encadeamento de métodos.</returns>
        public static IServiceCollection AddNetHandler(this IServiceCollection services, params Type[] handlerTypes)
        {
            services.AddSingleton<IDispatchr, Dispatchr>();

            var handlerInterfaceType = typeof(IRequestHandler<,>);

            foreach (var handlerType in handlerTypes)
            {
                if (!handlerType.IsClass || handlerType.IsAbstract)
                    throw new ArgumentException($"O tipo '{handlerType.FullName}' deve ser uma classe concreta.", nameof(handlerTypes));

                // Verifica se o tipo implementa pelo menos uma interface IRequestHandler<,>
                var implementedInterfaces = handlerType.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterfaceType)
                    .ToList();

                if (!implementedInterfaces.Any())
                {
                    throw new ArgumentException($"O tipo '{handlerType.FullName}' não implementa IRequestHandler<TRequest, TResponse>.", nameof(handlerTypes));
                }

                // Registra o handler para cada interface que ele implementar
                foreach (var handlerInterface in implementedInterfaces)
                {
                    services.AddTransient(handlerInterface, handlerType);
                }
            }

            return services;
        }
    }
}