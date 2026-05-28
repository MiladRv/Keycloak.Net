using Moq;

namespace Keycloak.Net.Sdk.UnitTests.Helpers;

public static class HttpClientFactoryHelper
{
    public static (IHttpClientFactory Factory, FakeHttpMessageHandler Handler) Create(
        string baseAddress = "http://localhost:8080/")
    {
        var handler = new FakeHttpMessageHandler();
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri(baseAddress) };

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        return (factory.Object, handler);
    }
}
