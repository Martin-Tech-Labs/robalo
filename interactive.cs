#!/usr/bin/env dotnet


#:package SimpleBase@5.6.4

using System.Globalization;
using System.Security.Cryptography;
using SimpleBase;
var utc = DateTimeOffset.UtcNow.ToString("o");


int[] array = [1, 2, 3, 4, 5, 6];

var checkpoint = 1;
var checkpointAsIndex = checkpoint - 1;

var z = array[(checkpointAsIndex + 1)..^1];

Console.WriteLine(z.Length);
