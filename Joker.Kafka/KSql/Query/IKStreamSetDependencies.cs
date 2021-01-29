﻿using Kafka.DotNet.ksqlDB.Extensions.KSql.RestApi;
using Kafka.DotNet.ksqlDB.Extensions.KSql.RestApi.Parameters;

namespace Kafka.DotNet.ksqlDB.Extensions.KSql.Query
{
  public interface IKStreamSetDependencies
  {
    IKSqlDbProvider KsqlDBProvider { get; }
    IKSqlQueryGenerator KSqlQueryGenerator { get; }
    QueryStreamParameters QueryStreamParameters { get; }
  }
}