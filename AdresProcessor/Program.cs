using AdresdataLib;
using System;
using Microsoft.Extensions.Configuration;

namespace AdresProcessor {
    class Program {
        static void Main(string[] args) {
            string dataPath = @"C:\Users\Piete\Documents\Hogent\Semester 2\Programmeren gevorderd\zipdata";
            AdresDataVerwerker a = new AdresDataVerwerker();
            a.CreateDir(dataPath, "temp");
            a.UnzipFile();
            a.leesFiles();
            a.SchrijfGemeentes();
            a.SchrijfAdresFile();
            Console.WriteLine("Bestanden aangemaakt");

            //a.clearFolder(dataPath);

        }
    }
}
