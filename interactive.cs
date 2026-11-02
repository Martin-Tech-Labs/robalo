#!/usr/bin/env dotnet


#:package SimpleBase@5.6.4

using System.Globalization;
using System.Security.Cryptography;
using SimpleBase;
var utc = DateTimeOffset.UtcNow.ToString("o");


List<int> list = new();
Console.WriteLine(list[^1]?);