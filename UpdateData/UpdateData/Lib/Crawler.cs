using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;

namespace UpdateData
{
    /// <summary>
    /// This class manages multiple threads that download files from the given URLs.
    /// Yes in retrospect Im aware there is a built in thread system inside the WebClient, by the time I learned that the threads were already done.
    /// </summary>
    public class Crawler
    {
        List<string> resStream;
        bool hasMsg;
        string resLock = new string(' ', 1);

        Thread crawlingThread;
        string source, dest, userName, password;
        int lastThreadIndex;
        ArrayList crawlingThreads;

        public Crawler()
        {
            resStream = new List<string>();
            lastThreadIndex = 0;
            hasMsg = false;
            crawlingThreads = new ArrayList();
        }

        public int startCrawl(string source, string dest)
        {
            return startCrawl(source, dest, null, null);
        }

        public int startCrawl(string source, string dest, string userName, string password)
        {
            crawlingThread = new Thread(new ThreadStart(crawl));
            lock (this) // not the best way to transfer this info but its good enough for our needs.
            {
                this.source = source;
                this.dest = dest;
                this.userName = userName;
                this.password = password;
                hasMsg = false;
                crawlingThreads.Add(crawlingThread);
                lastThreadIndex++;
            }
            crawlingThread.Start();
            Thread.Sleep(100);
            return lastThreadIndex;
        }

        private void crawl()
        {
            Thread currThread;
            string currSource, currDest, currUser, currPass;
            int threadIndex;
            int attemptNo = 1;
            bool gotAns = false;

            lock (this)
            {
                currSource = source;
                currDest = dest;
                currUser = userName;
                currPass = password;
                threadIndex = lastThreadIndex;
                currThread = crawlingThread;

            }

            WebClient Client = new WebClient();

            while (gotAns == false && attemptNo <= 3)
            {
                try
                {
                    if (currUser != null)
                        Client.Credentials = new NetworkCredential(currUser, currPass);

                    Client.DownloadFile(currSource, currDest);

                    Console.WriteLine("{0} is saved", currDest);
                    DataProcessor processor = new DataProcessor();
                    Thread.Sleep(100);
                    var res = processor.processToFormattedCSV(currDest, threadIndex);
                    Console.WriteLine("{0} is processed", currDest);



                    lock (resLock)
                    {
                        resStream.Add(string.Format("crawler {0}: Code 0 - downloaded file to {1}", threadIndex, currDest));
                        gotAns = true;
                        hasMsg = true;
                    }
                }
                catch (Exception ex)
                {
                    lock (resLock)
                    {
                        if (ex.InnerException != null)
                            resStream.Add(string.Format("crawler {0}: Code -1 - Encountered error: {1},attempt {2}/3", threadIndex, ex.InnerException.Message, attemptNo));
                        else
                            resStream.Add(string.Format("crawler {0}: Code -1 - Encountered error: {1},attempt {2}/3", threadIndex, ex.Message, attemptNo));
                        attemptNo++;
                        hasMsg = true;
                    }
                }
            }
            lock (crawlingThreads)
                crawlingThreads.Remove(currThread);
        }

        public int getCrawlersCount()
        {
            lock (crawlingThreads)
                return crawlingThreads.Count;
        }

        public string getResMsg()
        {
            string msg;
            lock (resLock)
            {
                if (hasMsg)
                {
                    msg = resStream.First<string>();
                    resStream.RemoveAt(0);
                    if (resStream.Count == 0)
                        hasMsg = false;
                    return msg;
                }
                return null;
            }
        }

        public void killAll()
        {
            lock (crawlingThreads)
                for (int i = 0; i < crawlingThreads.Count; i++)
                {
                    if (((Thread)crawlingThreads[i]).IsAlive == true)
                        ((Thread)crawlingThreads[i]).IsBackground = true;
                }
        }
    }
}