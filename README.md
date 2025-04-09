# NetHandler

NetHandler é uma biblioteca .NET focada em simplificar a comunicação e o desacoplamento entre componentes da sua aplicação. Inspirada nos princípios do mediator, ela oferece uma abordagem intuitiva para implementar os padrões de Command e Query, além de suportar notificações, permitindo construir aplicações mais modulares, testáveis e fáceis de manter.

Se você busca uma alternativa leve e com uma curva de aprendizado suave ao MediatR, o NetHandler oferece uma solução eficiente para orquestrar o fluxo de trabalho da sua aplicação através de mensagens e handlers dedicados.


# Exemplo de configuração de IoC

``` js
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Registra os handlers e o dispatchr, procurando no assembly atual.
        services.AddNetHandler(Assembly.GetExecutingAssembly());

        // Outros serviços...
        services.AddControllers();
    }
    
    // Resto da configuração do Startup...
}

```
