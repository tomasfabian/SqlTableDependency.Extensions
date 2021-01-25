﻿using FluentAssertions;
using Kafka.DotNet.ksqlDB.Extensions.KSql.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UnitTests;

namespace Kafka.DotNet.ksqlDB.Tests.Extensions.KSql.Linq
{
  [TestClass]
  public class InterceptStreamNameTests : TestBase
  {
    [TestMethod]
    public void InterceptStreamName()
    {
      //Arrange
      var dependencies = new TestKStreamSetDependencies();
      var query = new PeopleQueryStream(dependencies);

      //Act
      var ksql = query.ToQueryString();

      //Assert
      ksql.Should().BeEquivalentTo("SELECT * FROM People EMIT CHANGES;");
    }
  }
}