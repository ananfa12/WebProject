using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace UpdateData
{
    public static class GooglePR
    {
        public static void GetGPR()
        {

        }

        public static bool GetPage(DomainsEntities db, string urlStr, int n, int minPR)
        {

            try
            {
                var t1 = DateTime.Now;
                var test = true;
                WebClient webClient = new WebClient();
                var p = Convert.ToString((n - 1) * 25);

                var url = string.Format(urlStr, p);
                string page = webClient.DownloadString(url);

                HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(page);

                List<List<string>> table = doc.DocumentNode.SelectSingleNode("//table[@class='base1']") // responsive
                            .Descendants("tr")
                            .Skip(1)
                            .Where(tr => tr.Elements("td").Count() > 1)
                            .Select(tr => tr.Elements("td").Select(td => td.FirstChild.InnerText.Trim()).ToList())
                            .ToList();
                var s = string.Empty;

                foreach (var item in table)
                {
                    s = string.Format("{0}{1},{2}\n", s, item[0], item[1]);
                    if (int.Parse(item[1]) < minPR)
                    {
                        test = false;
                        break;
                    }
                    else
                    {
                        WriteToDB(db, item[0].ToLower(), item[1], item[2], item[4], item[6]);
                    }
                }
                db.SaveChanges();
                var tm = Math.Floor(DateTime.Now.Subtract(t1).TotalMilliseconds).ToString();
                Console.WriteLine("{0} is processed in {1} ms", url, tm.ToString());
                return test;
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message + '\n');
                return false;
            }

        }

        private static void WriteToDB(DomainsEntities db, string dom, string prStr, string blStr, string yStr, string dmoz)
        {
            var test = db.tbGooglePRs.Where(i => i.Domain == dom).FirstOrDefault();
            if (test == null)
            {

                try
                {
                    var tb = new tbGooglePR();
                    tb.Domain = dom;

                    bool res = false;
                    int temp;

                    res = int.TryParse(prStr, out temp);
                    if (res) tb.GooglePR = Convert.ToInt32(temp);

                    res = int.TryParse(yStr, out temp);
                    if (res)
                    {
                        tb.Year = Convert.ToInt32(temp);
                        tb.Archive = true;
                    }

                    res = int.TryParse(blStr, out temp);
                    if (res && temp != 0) tb.BackLinks = Convert.ToInt32(temp);

                    if (dmoz == "Yes") tb.Dmoz = true;

                    db.tbGooglePRs.Add(tb);
                }
                catch (Exception ex)
                {

                }
            }

        }


    }

}