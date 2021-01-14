﻿using System;
using System.Collections;
using System.Linq.Expressions;
using System.Text;

namespace Joker.Kafka.Extensions.KSql.Query
{
  public class KSqlVisitor : ExpressionVisitor
  {
    private readonly StringBuilder stringBuilder = new();

    public string BuildKSql(Expression expression)
    {
      stringBuilder.Clear();

      Visit(expression);

      return stringBuilder.ToString();
    }

    public override Expression? Visit(Expression? expression)
    {
      if (expression == null)
        return null;

      //https://docs.ksqldb.io/en/latest/developer-guide/ksqldb-reference/quick-reference/
      switch (expression.NodeType)
      {
        case ExpressionType.Constant:
          VisitConstant((ConstantExpression)expression);
          break;
      }

      return expression;
    }

    protected override Expression VisitConstant(ConstantExpression constantExpression)
    {
      if (constantExpression == null) throw new ArgumentNullException(nameof(constantExpression));

      var value = constantExpression.Value;

      if (value is not string && value is IEnumerable enumerable)
      {
        bool isFirst = true;

        foreach (var constant in enumerable)
        {
          if (isFirst)
            isFirst = false;
          else
          {
            stringBuilder.Append(", ");
          }

          stringBuilder.Append(constant);
        }
      }
      else if (value is string)
      {
        stringBuilder.Append($"'{value}'");
      }
      else
      {
        var stringValue = value != null ? value.ToString() : "NULL";

        stringBuilder.Append(stringValue ?? "Unknown");
      }

      return constantExpression;
    }

    protected void Append(string value)
    {
      stringBuilder.Append(value);
    }
  }
}