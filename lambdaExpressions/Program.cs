﻿using System;

namespace lambdaExpressions
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var test = new Table<User>();
            var me = "Me";
            //test.CustomWhere(x => x.Id != Guid.Empty || x.Name == me);
            //test.CustomWhere(x => x.Name.Contains("Boo"));
            test.CustomWhere(x => x.Age > 10);
            //test.CustomWhere(x => x.Name.Contains("Boo") && x.Age > 10 || x.Name == me);
            Console.WriteLine("replacement Query: {0}", test.Query);
        }
    }
}