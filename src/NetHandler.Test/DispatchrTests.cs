using Moq;
using NetHandler.Interfaces;

namespace NetHandler.Test
{
    public class DispatchrTests
    {
        [Fact]
        public async Task SendAsync_ShouldInvokeHandlerAndReturnResponse()
        {
            // Arrange
            var mockServiceProvider = new Mock<IServiceProvider>();
            var mockHandler = new Mock<IRequestHandler<IRequest<string>, string>>();
            mockHandler
                .Setup(h => h.Handle(It.IsAny<IRequest<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("Response");

            // Ajuste para lidar com proxy do Moq
            mockServiceProvider
                .Setup(sp => sp.GetService(It.Is<Type>(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))))
                .Returns(mockHandler.Object);

            var dispatchr = new Dispatchr(mockServiceProvider.Object);
            var request = new Mock<IRequest<string>>().Object;

            // Act
            var response = await dispatchr.SendAsync(request);

            // Assert
            Assert.Equal("Response", response);
            mockHandler.Verify(h => h.Handle(request, It.IsAny<CancellationToken>()), Times.Once);
        }


        [Fact]
        public async Task PublishAsync_ShouldInvokeAllNotificationHandlers()
        {
            // Arrange
            var mockServiceProvider = new Mock<IServiceProvider>();
            var mockHandler1 = new Mock<INotificationHandler<INotification>>();
            var mockHandler2 = new Mock<INotificationHandler<INotification>>();

            // Simula o retorno de uma coleção de handlers
            mockServiceProvider
                .Setup(sp => sp.GetService(typeof(IEnumerable<INotificationHandler<INotification>>)))
                .Returns(new[] { mockHandler1.Object, mockHandler2.Object });

            var dispatchr = new Dispatchr(mockServiceProvider.Object);
            var notification = new Mock<INotification>().Object;

            // Act
            await dispatchr.PublishAsync(notification);

            // Assert
            mockHandler1.Verify(h => h.Handle(notification, It.IsAny<CancellationToken>()), Times.Once);
            mockHandler2.Verify(h => h.Handle(notification, It.IsAny<CancellationToken>()), Times.Once);
        }


        [Fact]
        public async Task SendAsync_ShouldThrowExceptionWhenHandlerNotRegistered()
        {
            // Arrange
            var mockServiceProvider = new Mock<IServiceProvider>();
            mockServiceProvider
                .Setup(sp => sp.GetService(It.IsAny<Type>()))
                .Returns(null);

            var dispatchr = new Dispatchr(mockServiceProvider.Object);
            var request = new Mock<IRequest<string>>().Object;

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => dispatchr.SendAsync(request));
        }
    }
}
