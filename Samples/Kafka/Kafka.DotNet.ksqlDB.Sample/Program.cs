﻿using Kafka.DotNet.ksqlDB.KSql.Linq;
using Kafka.DotNet.ksqlDB.KSql.Query.Context;
using Kafka.DotNet.ksqlDB.KSql.Query.Functions;
using Kafka.DotNet.ksqlDB.KSql.Query.Windows;
using Kafka.DotNet.ksqlDB.Sample.Models;
using Kafka.DotNet.ksqlDB.Sample.Models.Movies;
using Kafka.DotNet.ksqlDB.Sample.Observers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using K = Kafka.DotNet.ksqlDB.KSql.Query.Functions.KSql;

namespace Kafka.DotNet.ksqlDB.Sample
{

  public static class Program
  {
    public static async Task Main(string[] args)
    {
      var ksqlDbUrl = @"http:\\localhost:8088";
      var contextOptions = new KSqlDBContextOptions(ksqlDbUrl);

      await using var context = new KSqlDBContext(contextOptions);

      using var disposable = context.CreateQueryStream<Tweet>()
        .Where(p => p.Message != "Hello world" || p.Id == 1)
        .Where(c => K.Functions.Like(c.Message.ToLower(), "%ALL%".ToLower()))
        .Where(p => p.RowTime >= 1510923225000) //AND RowTime >= 1510923225000
        .Select(l => new { l.Id, l.Message, l.RowTime })
        .Take(2) // LIMIT 2    
        .ToObservable() // client side processing starts here lazily after subscription
        .ObserveOn(TaskPoolScheduler.Default)
        .Subscribe(onNext: tweetMessage =>
        {
          Console.WriteLine($"{nameof(Tweet)}: {tweetMessage.Id} - {tweetMessage.Message}");
          Console.WriteLine();
        }, onError: error => { Console.WriteLine($"Exception: {error.Message}"); }, onCompleted: () => Console.WriteLine("Completed"));

      Console.WriteLine("Press any key to stop the subscription");

      Console.ReadKey();

      Console.WriteLine("Subscription completed");
    }

    private static IDisposable ClientSideBatching(KSqlDBContext context)
    {
      var disposable = context.CreateQueryStream<Tweet>()
        .ToObservable()
        .Buffer(TimeSpan.FromMilliseconds(250), 100)
        .Where(c => c.Count > 0)
        //.ObserveOn(System.Reactive.Concurrency.DispatcherScheduler.Current)
        .Subscribe(tweets =>
        {
          foreach (var tweet in tweets)
          {
            Console.WriteLine(tweet.Message);
          }
        });

      return disposable;
    }

    private static IDisposable JoinTables(KSqlDBContext context)
    {
      var query = context.CreateQueryStream<Movie>()
        .Join(
        //.LeftJoin(
          Source.Of<Lead_Actor>(nameof(Lead_Actor)),
          movie => movie.Title,
          actor => actor.Title,
          (movie, actor) => new
          {
            movie.Id,
            Title = movie.Title,
            movie.Release_Year,
            ActorName = K.Functions.RPad(K.Functions.LPad(actor.Actor_Name.ToUpper(), 15, "*"), 25, "^"),
            ActorTitle = actor.Title,
            Substr = K.Functions.Substring(actor.Title, 2, 4)
          }
        );

      var joinQueryString = query.ToQueryString();

      return query
        .Subscribe(c => { Console.WriteLine($"{c.Id}: {c.ActorName} - {c.Title} - {c.ActorTitle}"); }, exception => { Console.WriteLine(exception.Message); });
    }

    private static IDisposable Window(KSqlDBContext context)
    {
      var subscription1 = context.CreateQueryStream<Tweet>()
        .GroupBy(c => c.Id)
        .WindowedBy(new TimeWindows(Duration.OfSeconds(5)).WithGracePeriod(Duration.OfHours(2)))
        .Select(g => new { g.WindowStart, g.WindowEnd, Id = g.Key, Count = g.Count() })
        .Subscribe(c => { Console.WriteLine($"{c.Id}: {c.Count}: {c.WindowStart}: {c.WindowEnd}"); }, exception => { Console.WriteLine(exception.Message); });

      var query = context.CreateQueryStream<Tweet>()
        .GroupBy(c => c.Id)
        .WindowedBy(new HoppingWindows(Duration.OfSeconds(5)).WithAdvanceBy(Duration.OfSeconds(4))
          .WithRetention(Duration.OfDays(7)))
        .Select(g => new { Id = g.Key, Count = g.Count() });

      var hoppingWindowQueryString = query.ToQueryString();

      var subscription2 = query
        .Subscribe(c => { Console.WriteLine($"{c.Id}: {c.Count}"); }, exception => { Console.WriteLine(exception.Message); });

      return new CompositeDisposable { subscription1, subscription2 };
    }

    private static async Task AsyncEnumerable(KSqlDBContext context)
    {
      var cts = new CancellationTokenSource();
      var asyncTweetsEnumerable = context.CreateQueryStream<Tweet>().ToAsyncEnumerable();

      await foreach (var tweet in asyncTweetsEnumerable.WithCancellation(cts.Token))
      {
        Console.WriteLine(tweet.Message);
        cts.Cancel();
      }
    }

    private static IDisposable KQueryWithObserver(string ksqlDbUrl)
    {
      var contextOptions = new KSqlDBContextOptions(ksqlDbUrl);
      var context = new KSqlDBContext(contextOptions);

      var subscription = context.CreateQueryStream<Tweet>()
        .Where(p => p.Message != "Hello world" && p.Id != 1)
        .Take(2)
        .Subscribe(new TweetsObserver());

      return subscription;
    }

    private static IDisposable ToObservableExample(string ksqlDbUrl)
    {
      var contextOptions = new KSqlDBContextOptions(ksqlDbUrl);
      var context = new KSqlDBContext(contextOptions);

      var subscriptions = context.CreateQueryStream<Tweet>()
        .ToObservable()
        .Delay(TimeSpan.FromSeconds(2)) // IObservable extensions
        .Subscribe(new TweetsObserver());

      return subscriptions;
    }

    private static async Task ToQueryStringExample(string ksqlDbUrl)
    {
      var contextOptions = new KSqlDBContextOptions(ksqlDbUrl);
      await using var context = new KSqlDBContext(contextOptions);

      var ksql = context.CreateQueryStream<Person>().ToQueryString();

      //prints SELECT * FROM People EMIT CHANGES;
      Console.WriteLine(ksql);
    }

    private static async Task GroupBy()
    {
      var ksqlDbUrl = @"http:\\localhost:8088";
      var contextOptions = new KSqlDBContextOptions(ksqlDbUrl);

      contextOptions.QueryStreamParameters["auto.offset.reset"] = "latest";
      await using var context = new KSqlDBContext(contextOptions);

      context.CreateQueryStream<Tweet>()
        .GroupBy(c => c.Id)
        .Select(g => new { Id = g.Key, Count = g.Count() })
        .Subscribe(count =>
        {
          Console.WriteLine($"{count.Id} Count: {count.Count}");
          Console.WriteLine();
        }, error => { Console.WriteLine($"Exception: {error.Message}"); }, () => Console.WriteLine("Completed"));


      context.CreateQueryStream<Tweet>()
        .GroupBy(c => c.Id)
        .Select(g => g.Count())
        .Subscribe(count =>
        {
          Console.WriteLine($"Count: {count}");
          Console.WriteLine();
        }, error => { Console.WriteLine($"Exception: {error.Message}"); }, () => Console.WriteLine("Completed"));

      context.CreateQueryStream<Tweet>()
        .GroupBy(c => c.Id)
        .Select(g => new { Count = g.Count() })
        .Subscribe(count =>
        {
          Console.WriteLine($"Count: {count}");
          Console.WriteLine();
        }, error => { Console.WriteLine($"Exception: {error.Message}"); }, () => Console.WriteLine("Completed"));

      //Sum
      var subscription = context.CreateQueryStream<Tweet>()
        .GroupBy(c => c.Id)
        //.Select(g => g.Sum(c => c.Id))
        .Select(g => new { Id = g.Key, MySum = g.Sum(c => c.Id) })
        .Subscribe(sum =>
        {
          Console.WriteLine($"{sum}");
          Console.WriteLine();
        }, error => { Console.WriteLine($"Exception: {error.Message}"); }, () => Console.WriteLine("Completed"));
    }

    private static IDisposable NotNull(KSqlDBContext context)
    {
      return context.CreateQueryStream<Click>()
        .Where(c => c.IP_ADDRESS != null)
        .Select(c => new { c.IP_ADDRESS, c.URL, c.TIMESTAMP })
        .Subscribe(message => Console.WriteLine(message), error => { Console.WriteLine($"Exception: {error.Message}"); });
    }

    private static IDisposable DynamicFunctionCall(KSqlDBContext context)
    {
      var ifNullQueryString = context.CreateQueryStream<Tweet>()
        .Select(c => new { c.Id, c.Amount, Col = K.F.Dynamic("IFNULL(Message, 'n/a')") as string })
        .ToQueryString();

      return context.CreateQueryStream<Tweet>()
        .Select(c => K.Functions.Dynamic("ARRAY_DISTINCT(ARRAY[1, 1, 2, 3, 1, 2])") as int[])
        .Subscribe(
          message => Console.WriteLine($"{message[0]} - {message[^1]}"),
          error => Console.WriteLine($"Exception: {error.Message}"));
    }

    private static IDisposable Having(KSqlDBContext context)
    {
      return     
        //https://kafka-tutorials.confluent.io/finding-distinct-events/ksql.html
        context.CreateQueryStream<Click>()
        .GroupBy(c => new { c.IP_ADDRESS, c.URL, c.TIMESTAMP })
        .WindowedBy(new TimeWindows(Duration.OfMinutes(2)))
        .Having(c => c.Count(g => c.Key.IP_ADDRESS) == 1)
        .Select(g => new { g.Key.IP_ADDRESS, g.Key.URL, g.Key.TIMESTAMP })
        .Take(3)
        .Subscribe(onNext: message =>
        {
          Console.WriteLine($"{nameof(Click)}: {message}");
          Console.WriteLine($"{nameof(Click)}: {message.URL} - {message.TIMESTAMP}");
        }, onError: error => { Console.WriteLine($"Exception: {error.Message}"); }, onCompleted: () => Console.WriteLine("Completed"));
    }

    private static IDisposable TopKDistinct(KSqlDBContext context)
    {
      return context.CreateQueryStream<Tweet>()
        .GroupBy(c => c.Id)
        .Select(g => new { Id = g.Key, TopK = g.TopKDistinct(c => c.Amount, 2) })
        // .Select(g => new { Id = g.Key, TopK = g.TopK(c => c.Amount, 2) })
        .Subscribe(onNext: tweetMessage =>
        {
          var tops = string.Join(',', tweetMessage.TopK);
          Console.WriteLine($"{nameof(Tweet)} Tops: {tops}");
          Console.WriteLine($"{nameof(Tweet)}: {tweetMessage}");
          Console.WriteLine($"{nameof(Tweet)}: {tweetMessage.TopK[0]} - {tweetMessage.TopK[^1]}");

          Console.WriteLine($"TopKs Array Length: {tops.Length}");
          Console.WriteLine();
        }, onError: error => { Console.WriteLine($"Exception: {error.Message}"); }, onCompleted: () => Console.WriteLine("Completed"));
    }

    private static void ScalarFunctions(KSqlDBContext context)
    {
      context.CreateQueryStream<Tweet>()
        .Select(c => new
        {
          Abs = K.Functions.Abs(c.Amount), Ceil = K.Functions.Ceil(c.Amount), Floor = K.Functions.Floor(c.Amount),
          Random = K.Functions.Random(), Sign = K.Functions.Sign(c.Amount)
        })
        .ToQueryString();
    }

    private static IDisposable LatestByOffset(KSqlDBContext context)
    {
      var query = context.CreateQueryStream<Tweet>()
        .GroupBy(c => c.Id)
        .Select(g => new {Id = g.Key, EarliestByOffset = g.EarliestByOffset(c => c.Amount, 2)})
        .ToQueryString();

      return context.CreateQueryStream<Tweet>()
        .GroupBy(c => c.Id)
        //.Select(g => new { Id = g.Key, Earliest = g.EarliestByOffset(c => c.Message) })
        //.Select(g => new { Id = g.Key, Earliest = g.EarliestByOffsetAllowNulls(c => c.Message) })
        //.Select(g => new { Id = g.Key, Earliest = g.LatestByOffset(c => c.Message) })
        .Select(g => new { Id = g.Key, Earliest = g.LatestByOffsetAllowNulls(c => c.Message) })
        .Take(2) // LIMIT 2    
        .Subscribe(onNext: tweetMessage =>
        {
          Console.WriteLine($"{nameof(Tweet)}: {tweetMessage}");
          Console.WriteLine();
        }, onError: error => { Console.WriteLine($"Exception: {error.Message}"); }, onCompleted: () => Console.WriteLine("Completed"));
    }

    private static IDisposable CollectSet(KSqlDBContext context)
    {
      var subscription = context.CreateQueryStream<Tweet>()
        .GroupBy(c => c.Id)
        .Select(g => new { Id = g.Key, Array = g.CollectSet(c => c.Message) })
        //.Select(g => new { Id = g.Key, Array = g.CollectList(c => c.Message) })
        .Subscribe(c =>
        {
          Console.WriteLine($"{c.Id}:");
          foreach (var value in c.Array)
          {
            Console.WriteLine($"  {value}");
          }
        }, exception => { Console.WriteLine(exception.Message); });

      return subscription;
    }

    private static IDisposable Arrays(KSqlDBContext context)
    {
      var subscription =
        context.CreateQueryStream<Tweet>()
          .Select(_ => new {FirstItem = new[] {1, 2, 3}[1]})
          .Subscribe(onNext: c => { Console.WriteLine($"Array first value: {c}"); },
            onError: error => { Console.WriteLine($"Exception: {error.Message}"); });

      var arrayLengthQuery = context.CreateQueryStream<Tweet>()
        .Select(_ => new[] {1, 2, 3}.Length)
        .ToQueryString();

      return subscription;
    }

    private static IDisposable CountDistinct(KSqlDBContext context)
    {
      var subscription = context.CreateQueryStream<Tweet>()
        .GroupBy(c => c.Id)
        // .Select(g => new { Id = g.Key, Count = g.CountDistinct(c => c.Message) })
        .Select(g => new { Id = g.Key, Count = g.LongCountDistinct(c => c.Message) })
        .Subscribe(c =>
        {
          Console.WriteLine($"{c.Id} - {c.Count}");
        }, exception => { Console.WriteLine(exception.Message); });

      return subscription;
    }

    private static IDisposable NestedTypes(KSqlDBContext context)
    {
      var disposable = 
        context.CreateQueryStream<Tweet>()
          .Select(c => new
          {
            MapValue = new Dictionary<string, Dictionary<string, int>>
            {
              { "a", new Dictionary<string, int> { { "a", 1 }, { "b", 2 } } },
              { "b", new Dictionary<string, int> { { "c", 3 }, { "d", 4 } } },
            }["a"]
          })
          .Subscribe(
            message => Console.WriteLine($"{message.MapValue}"),
            error => Console.WriteLine($"Exception: {error.Message}"));

      return disposable;
    }
  }
}