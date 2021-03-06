﻿using System;
using Joker.Domain;

namespace Sample.Domain.Models
{
  public class Device : DomainEntity
  {
    public string Name { get; set; }

    public DateTime LastOnline { get; set; }
  }
}