using System;
using System.IO;
using System.Collections.Generic;
using System.IO.Compression;
using Microsoft.Extensions.Configuration;
using System.Linq;

namespace AdresdataLib
{
    public class AdresDataVerwerker
    {
        private string ZipPath;
        private string ExtractPath;
        private string ResultPath;
        private string OutputFileName;
        private string ProvincieInfo;
        private string Straatnaam;
        private string Gemeentenaam;
        private string StraatnaamIDgemeenteID;
        private string Provincies;

        private Dictionary<string, Dictionary<string, SortedSet<string>>> data = new Dictionary<string, Dictionary<string, SortedSet<string>>>();

        public AdresDataVerwerker()
        {
            var builder = new ConfigurationBuilder().AddJsonFile(@$"C:\Users\Piete\Documents\Hogent\Semester 2\Programmeren gevorderd\Solutions\EvaluatieOefeningen\AdresInfoFileIO\AdresDataVerwerker\adresfiles.json");
            var config = builder.Build();
            ProvincieInfo = config["provincieInfo"];
            Straatnaam = config["straatnaam"];
            Gemeentenaam = config["gemeentenaam"];
            StraatnaamIDgemeenteID = config["straatnaamIDgemeenteID"];
            Provincies = config["provincies"];
            ExtractPath = config["pathExtract"];
            ResultPath = config["pathResult"];
            ZipPath = config["pathzip"];
        }
        private class ProvincieGemeente
        {
            public ProvincieGemeente(int gemeenteID, int provincieID, string provincieNaam)
            {
                GemeenteID = gemeenteID;
                ProvincieID = provincieID;
                ProvincieNaam = provincieNaam;
            }

            public int GemeenteID { get; private set; }
            public int ProvincieID { get; private set; }
            public string ProvincieNaam { get; private set; }

            public override string ToString()
            {
                return $"{GemeenteID},{ProvincieID},{ProvincieNaam}";
            }

        }
        public void UnzipFile()
        {
            using (ZipArchive archive = ZipFile.Open(ZipPath, ZipArchiveMode.Read))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    //     Extract to directory.
                    entry.ExtractToFile(ExtractPath + "\\" + entry.Name);
                }
            }
        }
        public void leesFiles()
        {
            Console.WriteLine("start reading files");
            //lees provincieIDs
            HashSet<int> provincieIDs = new HashSet<int>();
            string line;
            using (StreamReader p = new StreamReader(Path.Combine(ExtractPath, Provincies)))
            {
                while ((line = p.ReadLine()) != null)
                {
                    string[] x = line.Split(',');
                    foreach (string a in x)
                    {
                        provincieIDs.Add(int.Parse(a));
                    }
                }
                p.Close();
            }
            Dictionary<int, ProvincieGemeente> gemeenteProvincieLink = new Dictionary<int, ProvincieGemeente>();//key is gemeenteID, value is gemeenteid,provid en provnaam
            using (StreamReader gpl = new StreamReader(Path.Combine(ExtractPath, ProvincieInfo)))
            {
                gpl.ReadLine();
                while ((line = gpl.ReadLine()) != null)
                {
                    string[] x = line.Trim().Split(';');
                    int gemeenteID = int.Parse(x[0]);
                    int provincieID = int.Parse(x[1]);
                    string taal = x[2];
                    if (taal == "fr") continue;
                    string provNaam = x[3];
                    if (provincieIDs.Contains(provincieID)) gemeenteProvincieLink.Add(gemeenteID, (new ProvincieGemeente(gemeenteID, provincieID, provNaam)));
                }
                gpl.Close();
            }
            // gemeentenamen + gemeenteid
            Dictionary<int, string> gemeentenNaam = new Dictionary<int, string>();
            using (StreamReader g = new StreamReader(Path.Combine(ExtractPath, Gemeentenaam)))
            {
                g.ReadLine();
                while ((line = g.ReadLine()) != null)
                {
                    string[] x = line.Trim().Split(';');
                    string taal = x[2];
                    if (taal == "fr") continue;
                    int gemeenteID = int.Parse(x[1]);
                    string gemeenteNaam = x[3];
                    if (gemeenteProvincieLink.ContainsKey(gemeenteID))
                        if (!gemeentenNaam.ContainsKey(gemeenteID))
                            gemeentenNaam.Add(gemeenteID, gemeenteNaam);
                }
                g.Close();
            }
            Dictionary<int, int> straatnaamGemeenteLink = new Dictionary<int, int>();
            using (StreamReader sg = new StreamReader(Path.Combine(ExtractPath, StraatnaamIDgemeenteID)))
            {
                sg.ReadLine();
                while ((line = sg.ReadLine()) != null)
                {
                    string[] x = line.Trim().Split(';');
                    int straatID = int.Parse(x[0]);
                    int gemeenteID = int.Parse(x[1]);
                    if (!straatnaamGemeenteLink.ContainsKey(straatID))
                        straatnaamGemeenteLink.Add(straatID, gemeenteID);
                }
                sg.Close();
            }
            //straatnamen lezen
            Dictionary<int, string> gemeentenstraatnamen = new Dictionary<int, string>();
            using (StreamReader g = new StreamReader(Path.Combine(ExtractPath, Straatnaam)))
            {
                g.ReadLine();
                while ((line = g.ReadLine()) != null)
                {
                    string[] x = line.Trim().Split(';');
                    int straatID = int.Parse(x[0]);
                    if (straatID == -9) continue;
                    string straatNaam = x[1];
                    if (straatnaamGemeenteLink.ContainsKey(straatID))
                    {
                        gemeentenstraatnamen.Add(straatID, straatNaam);
                    }
                }
                g.Close();
            }
            //maak de overkoepelende dictionary
            foreach (int c_id in gemeenteProvincieLink.Keys)
            {
                string provincie = gemeenteProvincieLink[c_id].ProvincieNaam;
                string gemeente = gemeentenNaam[c_id];
                if (!data.ContainsKey(provincie))
                {
                    data.Add(provincie, new Dictionary<string, SortedSet<string>>());
                }
                if (!data[provincie].ContainsKey(gemeente))
                    data[provincie].Add(gemeente, new SortedSet<string>());
            }
            foreach (int s_id in gemeentenstraatnamen.Keys)
            {
                int c_id = straatnaamGemeenteLink[s_id];
                if (gemeenteProvincieLink.ContainsKey(c_id) && gemeentenNaam.ContainsKey(c_id))//
                {
                    string provincie = gemeenteProvincieLink[c_id].ProvincieNaam;
                    string gemeente = gemeentenNaam[c_id];
                    string straat = gemeentenstraatnamen[s_id];
                    if (data.ContainsKey(provincie))
                    {
                        if (data[provincie].ContainsKey(gemeente))
                        {
                            data[provincie][gemeente].Add(straat);
                        }
                    }
                }
            }

        }
        public void ToonDict()
        {
            foreach (var pair in data)
            {
                Console.WriteLine("Provincie: " + pair.Key);
                foreach (var innerpair in pair.Value)
                {
                    Console.WriteLine("Gemeente" + innerpair.Key);
                    Console.WriteLine();
                    foreach (var straat in innerpair.Value)
                    {
                        Console.WriteLine(straat.ToString());
                    }
                }
            }
        }
        public void SchrijfGemeentes()
        {

            DirectoryInfo di = new DirectoryInfo(ResultPath);
            foreach (var pNaam in data)
            {
                di.CreateSubdirectory(pNaam.Key);
                foreach (var gNaam in pNaam.Value)
                {
                    string gemeentePath = @$"{ ResultPath}" + $"{pNaam.Key}";
                    using (StreamWriter sw = File.CreateText(Path.Combine(gemeentePath, $"{gNaam.Key}.txt")))
                    {
                        foreach (var straat in gNaam.Value)
                        {
                            sw.WriteLine(straat.ToString());
                        }
                        sw.Close();
                    }
                }
            }
        }
        public void SchrijfAdresFile()
        {
            var adressen = data.SelectMany(n => n.Value.SelectMany(o => o.Value.Select(s => n.Key +"," + o.Key + "," + s))).ToList();
            DirectoryInfo di = new DirectoryInfo(ResultPath);
            using (StreamWriter sw = File.CreateText(Path.Combine(ResultPath, "adresInfo.txt")))
            {
                foreach (var adres in adressen)
                {
                    sw.WriteLine(adres);

                }
                sw.Close();
            }
        }
        public void CreateDir(string path, string dir)
        {
            DirectoryInfo di = new DirectoryInfo(path);
            string p = Path.Combine(path, dir);
            if (Directory.Exists(p))
            {
                //verwijder dir en subdir
                Directory.Delete(p, true);
            }
            di.CreateSubdirectory(dir);
            di = new DirectoryInfo(p);
            di.CreateSubdirectory("extract");
            di.CreateSubdirectory("results");
        }
        public void clearFolder(string folder)
        {
            string[] dirs = Directory.GetDirectories(folder);

            foreach (string fi in Directory.GetFiles(folder))
            {
                Console.WriteLine($"deleting {fi}");
                File.Delete(fi);
            }

            foreach (string di in dirs)
            {
                clearFolder(di);
                Console.WriteLine($"deleting folder {di}");
                Directory.Delete(di);
            }
        }

    }
}

