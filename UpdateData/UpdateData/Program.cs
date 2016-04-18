using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Threading;
using System.Collections;
using EntityFramework.BulkInsert.Extensions;

namespace UpdateData
{
    class Program
    {
        private static string defaultSource;
        private static string defaultResFolder;
        private static string gprSource;
        private static string alexaSource;
        //private static Timer crawlerStateTimer; // prev
        private static Crawler crawler;
        private static ArrayList files;
        private static List<int> treatmentIDs;
        //private static int currentFile; //prev

        static void Main(string[] args)
        {
            crawler = new Crawler();
            files = new ArrayList();
            treatmentIDs = new List<int>();

            defaultSource = ConfigurationManager.AppSettings["InputFile"];
            defaultResFolder = ConfigurationManager.AppSettings["OutputFolder"];
            gprSource = ConfigurationManager.AppSettings["GprSrcFile"];
            alexaSource = ConfigurationManager.AppSettings["Alexa"];

            int userInput = 0;
            do
            {
                userInput = DisplayMenu();
                switch (userInput)
                {
                    case 1:
                        // 1-download and create csv files
                        Console.WriteLine("********** Downloading src files ...");
                        CreateSrcFiles();
                        break;
                    case 2:
                        // 2-recreate src domain table
                        Console.WriteLine("********** Creating domains table ...");
                        ReCreateDomTable();
                        break;
                    case 3:
                        // 3-import GPR, BL, Y 
                        Console.WriteLine("********** Importing GPR etc ...");
                        ImportGPR();
                        break;
                    case 4:
                        // 4-update Alexa top 1M
                        Console.WriteLine("********** Importing Snapnames domain from site ...");
                        GetAlexa();
                        break;
                    case 5:
                        // 5-create domains master table                      
                        Console.WriteLine("********** Create domains master table ...");
                        CreateMasterTable();
                        break;
                    case 6: 
                        // eng words
                        EngWords();
                        break;
                    case 7:
                        // 7-execute all
                        Console.WriteLine("********** Execute all ...");
                        ExecuteAll();
                        break;
                    case 0:
                        // exit
                        break;
                    default:
                        Console.WriteLine("The key is not supported.");
                        break;
                }
            } while (userInput != 0);
            //
            //Console.ReadLine();
        }

        static public int DisplayMenu()
        {
            Console.WriteLine("1 - download and create csv files");
            Console.WriteLine("2 - recreate src domain table");
            Console.WriteLine("3 - import GPR, BL, Y ");
            Console.WriteLine("4 - Importe Snapnames domain from site - edit h");
            Console.WriteLine("5 - create domains master table");
            Console.WriteLine("6 - english words");
            Console.WriteLine("7 - execute all");
            Console.WriteLine("0 - exit");
            var result = Console.ReadLine();
            return Convert.ToInt32(result);
        }

        private static void ExecuteAll()
        {
            var t1 = DateTime.Now;
            CreateSrcFiles();
            ReCreateDomTable();
            ImportGPR();
            GetAlexa();
            CreateMasterTable();
            EngWords();
            var t2 = Math.Round((decimal)DateTime.Now.Subtract(t1).TotalSeconds).ToString();
            Console.WriteLine("New Domains table is created and prepared in {0} sec", t2);
        }


        private static void CreateSrcFiles()
        {
            var t1 = DateTime.Now;
            StreamReader src = null;
            try
            {
                src = new StreamReader(File.OpenRead(defaultSource));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to access source file: " + ex.Message);
                return;
            }
            string destFolder;
            string[] sourceData;
            string addr = "", dest = "";

            destFolder = defaultResFolder;

            int fileNo = 0;
            while (src.EndOfStream == false)
            {
                try
                {
                    sourceData = src.ReadLine().Split(',');
                    DateTime fetchDate = DateTime.Today;
                    switch (int.Parse(sourceData[1]))
                    {
                        case 16:
                            addr = sourceData[2] + fetchDate.Year + "-" + fetchDate.Month + "-" + fetchDate.Day;
                            dest = destFolder + "\\" + sourceData[0] + ".zip";
                            break;
                        case 17:
                            fetchDate = fetchDate.AddDays(1);
                            addr = sourceData[2] + fetchDate.Year + "-" + fetchDate.Month + "-" + fetchDate.Day;
                            dest = destFolder + "\\" + sourceData[0] + ".zip";
                            break;
                        case 18:
                            fetchDate = fetchDate.AddDays(2);
                            addr = sourceData[2] + fetchDate.Year + "-" + fetchDate.Month + "-" + fetchDate.Day;
                            dest = destFolder + "\\" + sourceData[0] + ".zip";
                            break;
                        case 19:
                            fetchDate = fetchDate.AddDays(3);
                            addr = sourceData[2] + fetchDate.Year + "-" + fetchDate.Month + "-" + fetchDate.Day;
                            dest = destFolder + "\\" + sourceData[0] + ".zip";
                            break;
                        case 20:
                            fetchDate = fetchDate.AddDays(4);
                            addr = sourceData[2] + fetchDate.Year + "-" + fetchDate.Month + "-" + fetchDate.Day;
                            dest = destFolder + "\\" + sourceData[0] + ".zip";
                            break;
                        default:
                            addr = sourceData[2];
                            dest = destFolder + "\\" + sourceData[0] + "." + sourceData[2].Split('.').Last<string>();
                            break;
                    }

                    if (int.Parse(sourceData[3]) != 1)
                    {

                        sourceData[4] = null;
                        sourceData[5] = null;
                    }
                    initCrawlerSimple(addr, dest, sourceData[4], sourceData[5], int.Parse(sourceData[1]));
                    //initCrawler(addr, dest, sourceData[4], sourceData[5], int.Parse(sourceData[1]));
                    fileNo++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to read file " + fileNo);
                }
            }

            var t2 = Math.Round((decimal)DateTime.Now.Subtract(t1).TotalSeconds).ToString();
            Console.WriteLine("{0} files imported in {1} sec", fileNo.ToString(),t2);
            src.Close();
            //this.Refresh();

            
        }

        private static void ReCreateDomTable()
        {
            var t1 = DateTime.Now;
            var cs = new CsvBulkCopyDataIntoSqlServer();
            var folder = ConfigurationManager.AppSettings["OutputFolder"];
            var files = Directory.EnumerateFiles(folder, "*.csv").ToArray();
            var counter = 0;
            for (var i = 0; i < files.Length; i++)
            {
                var tt1 = DateTime.Now;
                var f = new FileInfo(files[i]);
                var cnt = cs.LoadCsvDataIntoSqlServer(f.Name, i == 0);
                counter += cnt;
                var dt = Math.Round((decimal)DateTime.Now.Subtract(tt1).TotalSeconds).ToString();
                Console.WriteLine("{0}: {1} records are imported in {2} sec", f.Name, cnt.ToString(), dt.ToString());
            }
            var t2 = Math.Round((decimal)DateTime.Now.Subtract(t1).TotalSeconds).ToString();
            Console.WriteLine("New table ({0} records) is created in {1} sec", counter.ToString(), t2);

        }




        private static Trie trie;
        private static Words result;
        private static void EngWords(){
            var t1 = DateTime.Now;
            var file = ConfigurationManager.AppSettings["WordsFile"];
            trie = new Trie(File.ReadAllLines(file));
            var t = new List<string>();
            result = new Words(t);
            result.Tree = trie;
            var p = 10000;
            using (var db = new DomainsEntities())
            {
                db.Database.CommandTimeout = 0;
                var com = "TRUNCATE TABLE [dbo].[tbSplit]";
                db.Database.ExecuteSqlCommand(com);

                var m = db.tbNewDomains.Count() / p;
                int i = 0;
                while (SplitNames(i++) && i < m + 1)
                {
                    GC.Collect(0, GCCollectionMode.Forced, true);
                }

                var t2 = Math.Round((decimal)DateTime.Now.Subtract(t1).TotalSeconds).ToString();
                Console.WriteLine("Names split is executed in {0} sec", t2);

                // prepare keywords params
                com = "EXEC [dbo].[CreateDomainsTableKeys]";
                db.Database.ExecuteSqlCommand(com);
                t2 = Math.Round((decimal)DateTime.Now.Subtract(t1).TotalSeconds).ToString();
                Console.WriteLine("Keywords searches are filled {0} sec", t2);

            }

            

            
        }
        private static bool SplitNames(int chunkNum)
        {
            using (var db = new DomainsEntities())
            {
               
                db.Configuration.AutoDetectChangesEnabled = false;
                db.Configuration.ValidateOnSaveEnabled = false;
                var cnt = 0;
                var p = 10000;
                //var m = db.tbDomainsFromSrcs.Count() / p;
                
                var t0 = DateTime.Now;
                var list = db.tbNewDomains.Where(i => i.Id >= chunkNum * p && i.Id < (chunkNum + 1) * p);
                //var t1 = Math.Round((decimal)DateTime.Now.Subtract(t0).TotalSeconds).ToString();
                //Console.WriteLine("list is created ({0})", t1);
                //t0 = DateTime.Now;

                //
                    var dlist = new List<tbSplit>();
                    foreach (var d in list)
                    {
                        result.FindAndSplit(d.Name);
                        var dn = new tbSplit();
                        dn.DomID = d.Id;
                        dn.NameShown = result.FindBestIncSeparators();
                        dn.NameWords = result.SplitKeywords(dn.NameShown);
                        dn.WordCount = result.BestItemCount;
                        dlist.Add(dn);
                        cnt++;
                    }
                    try
                    {
                        //db.tbSplits.AddRange(dlist);
                        db.BulkInsert(dlist);
                        //t1 = Math.Round((decimal)DateTime.Now.Subtract(t0).TotalSeconds).ToString();
                        //Console.WriteLine("List is inserted ({0})", t1);
                        //t0 = DateTime.Now;

                        db.SaveChanges();
                        //t1 = Math.Round((decimal)DateTime.Now.Subtract(t0).TotalSeconds).ToString();
                        //Console.WriteLine("List is saved ({0})", t1);
                        //t0 = DateTime.Now;
                    }
                    catch (Exception ex)
                    {
                        var x = 1;
                    }
                var t1 = Math.Round((decimal)DateTime.Now.Subtract(t0).TotalSeconds).ToString();
                Console.WriteLine("{0}-th chunk is processed ({1})", (chunkNum + 1).ToString(), t1);
            }
            return true;
        }



        private static void CreateMasterTable()
        {
            using (var db = new DomainsEntities())
            {
                var t1 = DateTime.Now;
                db.Database.CommandTimeout = 0;
                var cnt = db.Database.SqlQuery<Int32>("EXEC [dbo].[CreateDomainsTable]").Single();
                var t2 = Math.Round((decimal)DateTime.Now.Subtract(t1).TotalSeconds).ToString();
                Console.WriteLine("New table ({0} records) is normalized in {1} sec", cnt.ToString(), t2);
            }
        }


        private static void ImportGPR()
        {
            var t1 = DateTime.Now;
            string urlStr;
          
            var delay = 1000 * int.Parse(ConfigurationManager.AppSettings["AverageDelay"]);
            var p = int.Parse(ConfigurationManager.AppSettings["MinPR"]);
            bool test = true;
            var n = 1;
            var m = 0;
            using (var db = new DomainsEntities())
            {
                var com = "TRUNCATE TABLE [dbo].[tbGooglePR]";
                db.Database.ExecuteSqlCommand(com);
                using (StreamReader reader = new StreamReader(gprSource))
                {
                    while ((urlStr = reader.ReadLine()) != null)
                    {
                        n = 1;
                        while (test)
                        {
                            test = GooglePR.GetPage(db, urlStr, n, p);
                            Random r = new Random();
                            int sec = r.Next(delay);
                            Thread.Sleep(sec);
                            n++;
                        }
                        m++;
                    }
                }
                var cnt = db.tbGooglePRs.Count();
                var tm1 = Math.Floor(DateTime.Now.Subtract(t1).TotalSeconds).ToString();
                Console.WriteLine("Completed all in {0} sec ({1} sources, {2} records)", tm1, Convert.ToString(m), cnt.ToString());
            }
        }

        private static void GetAlexa()
        {
            var t1 = DateTime.Now;
            WebClient Client = new WebClient();
            var d = new DirectoryInfo(defaultResFolder).Parent.FullName;
            var curDest = string.Format("{0}\\alexa.zip", d);
            Client.DownloadFile(alexaSource, curDest);
            DataProcessor processor = new DataProcessor();
            var res = processor.unzip(curDest);
            var bc = new CsvBulkCopyDataIntoSqlServer();
            bc.UpdateAlexaTable(res,true);
            var tm1 = Math.Floor(DateTime.Now.Subtract(t1).TotalSeconds).ToString();
            Console.WriteLine("Snapnames domains importined from site to DB in {0} sec", tm1);
        }
        

        private static void initCrawlerSimple(string source, string dest, string user, string pass, int treatmentID)
        {
            var t1 = DateTime.Now;
            WebClient Client = new WebClient();
            if (user != null)
                Client.Credentials = new NetworkCredential(user, pass);

            Client.DownloadFile(source, dest);
            //var t2 = Math.Round((decimal)DateTime.Now.Subtract(t1).Seconds).ToString();
            //Console.WriteLine("{0} is saved ({1} sec)", dest, t2);
            //t1 = DateTime.Now;
            DataProcessor processor = new DataProcessor();
            var res = processor.processToFormattedCSV(dest, treatmentID);
            var t2 = Math.Round((decimal)DateTime.Now.Subtract(t1).TotalSeconds).ToString();
            Console.WriteLine("{0} is processed ({1} sec)", dest,t2);
            //Thread.Sleep(100);
            //Console.WriteLine("Fetching file from " + source);

        }


        

    }




    //public class MyTimer
    //{
    //    public event EventHandler Tick;

    //    private Timer m_timer;

    //    public void Start()
    //    {
    //        m_timer = new Timer(OnTick, null, 0, 1);
    //        m_timer.InitializeLifetimeService();
    //    }

    //    public void OnTick(object state)
    //    {
    //        var tick = this.Tick;
    //        if (tick != null)tick();
    //    }
    //}
}
