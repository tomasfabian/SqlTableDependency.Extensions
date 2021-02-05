﻿using System;
using System.Linq.Expressions;
using System.Text;
using FluentAssertions;
using Kafka.DotNet.ksqlDB.KSql.Query.Functions;
using Functions = Kafka.DotNet.ksqlDB.KSql.Query.Functions;
using Kafka.DotNet.ksqlDB.KSql.Query.Visitors;
using Kafka.DotNet.ksqlDB.Tests.Pocos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UnitTests;

namespace Kafka.DotNet.ksqlDB.Tests.Extensions.KSql.Query.Visitors
{
  [TestClass]
  public class KSqlFunctionVisitorTests : TestBase
  {
    private KSqlFunctionVisitor ClassUnderTest { get; set; }

    private StringBuilder StringBuilder { get; set; }

    [TestInitialize]
    public override void TestInitialize()
    {
      base.TestInitialize();

      StringBuilder = new StringBuilder();
      ClassUnderTest = new KSqlFunctionVisitor(StringBuilder, useTableAlias: false);
    }

    #region Trim

    [TestMethod]
    public void Trim_BuildKSql_PrintsTrimFunction()
    {
      //Arrange
      Expression<Func<Tweet, string>> expression = c => K.Functions.Trim(c.Message);

      //Act
      var query = ClassUnderTest.BuildKSql(expression);

      //Assert
      query.Should().BeEquivalentTo($"TRIM({nameof(Tweet.Message)})");
    }

    #endregion

    #region LPad
    
    [TestMethod]
    public void LPad_BuildKSql_PrintsLPadFunction()
    {
      //Arrange
      Expression<Func<Tweet, string>> expression = c => K.Functions.LPad(c.Message, 8, "x");

      //Act
      var query = ClassUnderTest.BuildKSql(expression);

      //Assert
      query.Should().BeEquivalentTo($"LPAD({nameof(Tweet.Message)}, 8, 'x')");
    }

    #endregion

    #region RPad
    
    [TestMethod]
    public void RPad_BuildKSql_PrintsRPadFunction()
    {
      //Arrange
      Expression<Func<Tweet, string>> expression = c => K.Functions.RPad(c.Message, 8, "x");

      //Act
      var query = ClassUnderTest.BuildKSql(expression);

      //Assert
      query.Should().BeEquivalentTo($"RPAD({nameof(Tweet.Message)}, 8, 'x')");
    }

    #endregion

    #region Substring
    
    [TestMethod]
    public void Substring_BuildKSql_PrintsSubstringFunction()
    {
      //Arrange
      Expression<Func<Tweet, string>> expression = c => K.Functions.Substring(c.Message, 2, 3);

      //Act
      var query = ClassUnderTest.BuildKSql(expression);

      //Assert
      query.Should().BeEquivalentTo($"Substring({nameof(Tweet.Message)}, 2, 3)");
    }

    #endregion

    #region Like

    [TestMethod]
    public void Like_BuildKSql_PrintsLikeCondition()
    {
      //Arrange
      Expression<Func<Tweet, bool>> likeExpression = c => ksqlDB.KSql.Query.Functions.KSql.Functions.Like(c.Message, "santa%");

      //Act
      var query = ClassUnderTest.BuildKSql(likeExpression);

      //Assert
      query.Should().BeEquivalentTo($"{nameof(Tweet.Message)} LIKE 'santa%'");
    }

    [TestMethod]
    public void LikeToLower_BuildKSql_PrintsLikeCondition()
    {
      //Arrange
      Expression<Func<Tweet, bool>> likeExpression = c => ksqlDB.KSql.Query.Functions.KSql.Functions.Like(c.Message.ToLower(), "%santa%".ToLower());

      //Act
      var query = ClassUnderTest.BuildKSql(likeExpression);

      //Assert
      query.Should().BeEquivalentTo($"LCASE({nameof(Tweet.Message)}) LIKE LCASE('%santa%')");
    }

    #endregion
  }
}