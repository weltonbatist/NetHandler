# NetHandler

NetHandler é uma biblioteca .NET focada em simplificar a comunicação e o desacoplamento entre componentes da sua aplicação. Inspirada nos princípios do mediator, ela oferece uma abordagem intuitiva para implementar os padrões de Command e Query, além de suportar notificações, permitindo construir aplicações mais modulares, testáveis e fáceis de manter.

Se você busca uma alternativa leve e com uma curva de aprendizado suave ao MediatR, o NetHandler oferece uma solução eficiente para orquestrar o fluxo de trabalho da sua aplicação através de mensagens e handlers dedicados.


# Exemplo de configuração  IoC

``` js

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Registra os handlers e o dispatchr, procurando no assembly atual.
        services.AddNetHandler(Assembly.GetExecutingAssembly());

        // Registra os notification handlers automaticamente
        services.AddNetNotificationHandlers(Assembly.GetExecutingAssembly());

        // Outros serviços...
        services.AddControllers();
    }
    
    // Resto da configuração do Startup...
}

```

# Exemplo de utilização  RequestHandler

``` js

    // Definição de uma request (comando) que espera uma resposta (neste caso, um bool).
    public class CreateOrderCommand : IRequest<bool>
    {
        public string OrderId { get; set; }
        public decimal Amount { get; set; }
    }

    // Implementação do handler para a request CreateOrderCommand.
    public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, bool>
    {
        public async Task<bool> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
        {
            // Lógica de criação do pedido.
            Console.WriteLine($"Criando pedido {command.OrderId} com valor {command.Amount}.");
            await Task.CompletedTask;
            return true;
        }
    }

     ...

    // Exemplo de uso no programa:

    // Resolve o Dispatchr configurado via DI.
    var dispatchr = serviceProvider.GetRequiredService<IDispatchr>();

    // Cria o comando (request) para criar um pedido.
    var command = new CreateOrderCommand
    {
        OrderId = "XYZ789",
        Amount = 123.45m
    };

    // Envia o comando e aguarda a resposta.
    // O Dispatchr utilizará reflection e cache para resolver o handler adequado (CreateOrderHandler).
    bool result = await dispatchr.SendAsync<bool>(command);

    Console.WriteLine($"Comando processado. Resultado: {result}");

```

# Exemplo de utilização  Notification

``` js

    public class OrderCreatedNotification : INotification
    {
        public string OrderId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // Exemplo de handler que processa a notificação
    public class NotifyAdminHandler : INotificationHandler<OrderCreatedNotification>
    {
        public async Task Handle(OrderCreatedNotification notification, CancellationToken cancellationToken)
        {
            Console.WriteLine($"[Admin] Pedido {notification.OrderId} criado em {notification.CreatedAt}.");
            await Task.CompletedTask;
        }
    }
     ...

    // Exemplo de uso no programa:

    // Resolve o Dispatchr
    var dispatchr = serviceProvider.GetRequiredService<IDispatchr>();

    // Cria a notificação (por exemplo, um evento de pedido criado)
    var notification = new OrderCreatedNotification
    {
        OrderId = "ABC123",
        CreatedAt = DateTime.Now
    };

    // Publica a notificação: todos os handlers registrados para OrderCreatedNotification serão executados.
    await dispatchr.PublishAsync(notification);

```
