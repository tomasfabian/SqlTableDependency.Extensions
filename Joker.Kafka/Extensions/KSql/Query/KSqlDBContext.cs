﻿using System;
using Kafka.DotNet.ksqlDB.Extensions.KSql.Linq;
using Kafka.DotNet.ksqlDB.Extensions.KSql.RestApi;
using Microsoft.Extensions.DependencyInjection;

namespace Kafka.DotNet.ksqlDB.Extensions.KSql.Query
{
  public sealed class KSqlDBContext
  {
    private readonly KSqlDBContextOptions contextOptions;
    private IServiceCollection serviceCollection;

    public KSqlDBContext(KSqlDBContextOptions contextOptions)
    {
      this.contextOptions = contextOptions ?? throw new ArgumentNullException(nameof(contextOptions));

      RegisterDependencies();
    }

    private void RegisterDependencies()
    {
      serviceCollection = new ServiceCollection();

      serviceCollection.AddTransient<IKSqlQbservableProvider, QbservableProvider>();

      var uri = new Uri(contextOptions.Url);

      serviceCollection.AddTransient<IKSqlQueryGenerator, KSqlQueryGenerator>();

      serviceCollection.AddTransient<IHttpClientFactory, HttpClientFactory>(s =>
        new HttpClientFactory(uri));

      serviceCollection.AddTransient<IKSqldbProvider, KSqlDbQueryStreamProvider>();
      serviceCollection.AddSingleton(contextOptions.QueryStreamParameters);
      serviceCollection.AddTransient<IKStreamSetDependencies, KStreamSetDependencies>();
    }

    private bool wasRegistered;

    public IQbservable<TEntity> CreateStreamSet<TEntity>(string streamName = null)
    {
      if(!wasRegistered)
      {
        wasRegistered = true;

        RegisterDependencies();
      }

      if (streamName == String.Empty)
        streamName = null;

      var serviceProvider = serviceCollection.BuildServiceProvider(new ServiceProviderOptions {ValidateScopes = true});

      var dependencies = serviceProvider.GetService<IKStreamSetDependencies>();
      
      dependencies.QueryContext.StreamName = streamName;

      return new KQueryStreamSet<TEntity>(dependencies);
    }
  }
}