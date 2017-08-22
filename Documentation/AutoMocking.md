
## <a id="auto-mocking-using-moq"></a>Auto-Mocking using Moq

One of the really cool features of DI is the fact that it makes testing code much, much easier.  This is because you can easily substitute one dependency for another by using a different Composition Root.  For example, if you only want to test a particular class (let's call it Foo) and don't care about testing its dependencies, you might write 'mocks' for them so that you can isolate Foo specifically.

```csharp
public class Foo
{
    IWebServer _webServer;

    public Foo(IWebServer webServer)
    {
        _webServer = webServer;
    }

    public void Initialize()
    {
        ...
        var x = _webServer.GetSomething();
        ...
    }
}
```

In this example, we have a class Foo that interacts with a web server to retrieve content.  This would normally be very difficult to test for the following reasons:

* You would have to set up an environment where it can properly connect to a web server (configuring ports, urls, etc.)
* Running the test could be slower and limit how much testing you can do
* The web server itself could contain bugs so you couldn't with certainty isolate Foo as the problematic part of the test
* You can't easily configure the values returned from the web server to test sending various inputs to the Foo class

However, if we create a mock class for IWebServer then we can address all these problems:

```csharp
public class MockWebServer : IWebServer
{
    ...
}
```

Then hook it up in our installer:

```csharp
Container.Bind<IWebServer>().To<MockWebServer>().AsSingle();
```

Then you can implement the fields of the IWebServer interface and configure them based on what you want to test on Foo. Hopefully You can see how this can make life when writing tests much easier.

Zenject also allows you to even avoid having to write the MockWebServer class in favour of using a library called "Moq" which does all the work for you.

Note that by default, Auto-mocking is not enabled in Zenject.  If you wish to use the auto-mocking feature then you need to go to your Zenject install directory and extract the contents of "Extras/ZenjectAutoMocking.zip".  Note also that AutoMocking is incompatible with webplayer builds, and you will also need to change your "Api Compatibility Level" from ".NET 2.0 Subset" to ".NET 2.0" (you can find this in PC build settings)

After extracting the auto mocking package it is just a matter of using the following syntax to mock out various parts of your project:

```csharp
Container.Bind<IFoo>().FromMock();
```

However, this approach will not allow you to take advantage of the advanced features of Moq.  For more advanced usages, see the documentation for Moq
