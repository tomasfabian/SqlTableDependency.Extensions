﻿using Kafka.DotNet.ksqlDB.Extensions.KSql.RestApi;

namespace Kafka.DotNet.ksqlDB.Tests.Extensions.KSql.RestApi
{
  internal class AggregationsKsqlDbQueryStreamProvider : TestableKSqlDbQueryStreamProvider
  {
    public AggregationsKsqlDbQueryStreamProvider(IHttpClientFactory httpClientFactory)
      : base(httpClientFactory)
    {
      QueryResponse =
        "{\"queryId\":\"cadfd47e-748d-44a5-9c25-0e88e2f57875\",\"columnNames\":[\"KSQL_COL_0\"],\"columnTypes\":[\"BIGINT\"]}\r\n[0]\r\n[1]";
    }
  }
}