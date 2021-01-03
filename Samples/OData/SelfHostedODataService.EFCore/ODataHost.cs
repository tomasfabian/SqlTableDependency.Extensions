﻿using System.Diagnostics;
using Joker.OData.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sample.DataCore.EFCore;
using Serilog;

namespace SelfHostedODataService.EFCore
{
  public class ODataHost : ODataHost<ODataStartupWithPubSub>
  {
    protected override void OnConfigureWebHostBuilder(IWebHostBuilder webHostBuilder)
    {
      webHostBuilder
        .UseSerilog();

      base.OnConfigureWebHostBuilder(webHostBuilder);
    }

    protected override void OnHostBuilt(IHost host)
    {
      if (Debugger.IsAttached)
      {
        using var scope = host.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<SampleDbContextCore>();

        dbContext.Database.Migrate();
      }
    }
  }
}