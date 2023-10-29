## EdgeDb.Net.Configurator
Is a lib that helps to easily configure and add `EdgeDb.Net` in a ***single line*** (`services.ConfigureEdgeDbDatabase(Settings.Database.ProjectName, poolCfg)`) using just only project name, see following full example that a bit larger then single line:

```csharp
private void ConfigureDatabase(IServiceCollection services)
{
    EdgeDBClientPoolConfig poolCfg = new EdgeDBClientPoolConfig()
    {
        ClientType = EdgeDBClientType.Tcp,
        SchemaNamingStrategy = INamingStrategy.CamelCaseNamingStrategy,
        DefaultPoolSize = 256,
        ConnectionTimeout = 5000,
        MessageTimeout = 10000
    };
    services.ConfigureEdgeDbDatabase("10.50.55.41", Settings.Database.ProjectName, poolCfg, null, true);
}
```

or you could pass an array of directories where edgedb project credentials could be found, i.e. `services.ConfigureEdgeDbDatabase("10.50.55.41", Settings.Database.ProjectName, poolCfg, new[] {"/usr/myapp/edgedb", "/usr/share"}, true);`

or if you are running app && edgedb on the same machine use simplified method `ConfigureLocalEdgeDbDatabase`:

```csharp
private void ConfigureDatabase(IServiceCollection services)
{
    EdgeDBClientPoolConfig poolCfg = new EdgeDBClientPoolConfig()
    {
        ClientType = EdgeDBClientType.Tcp,
        SchemaNamingStrategy = INamingStrategy.CamelCaseNamingStrategy,
        DefaultPoolSize = 256,
        ConnectionTimeout = 5000,
        MessageTimeout = 10000
    };
    services.ConfigureLocalEdgeDbDatabase(Settings.Database.ProjectName, poolCfg);
}
```

### LIMITATIONS

1. works if your `edgedb` server on the same machine as application.
2. supports only `Windows` and `Linux`, if required more `OS`, please add `PR` and/or issue.

Advantage of this - you just need **only EdgeDb Project name**.

### NuGet
[Configurator Nuget package](https://www.nuget.org/packages/Wissance.EdgeDb.Configurator/)
