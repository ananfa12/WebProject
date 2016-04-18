using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using System.Xml;
using Ionic.Zip;

namespace WebCrawler
{
    /// <summary>
    /// This class contains various methods applied to files in order to bring them to a raw text format (.txt)
    /// </summary>
    class DataProcessor
    {
        int fileStatusFlag;
        public DataProcessor()
        {
        }

        public string processToFormattedCSV(string fileName, int treatmentID)
        {
            string prevFileName = null;
            fileName = process(fileName);

            while (fileName != prevFileName)
            {
                prevFileName = fileName;
                fileName = process(fileName);
                if (fileStatusFlag < 0)
                    return null;
            }

            fileName = formatAsCSV(fileName, treatmentID);
            if (fileStatusFlag < 0)
                return null;
            return fileName;
        }

        public string formatAsCSV(string fileName, int treatmentID)
        {
            try
            {
                string res = fileName.Substring(0, fileName.Length - 3) + "csv";
                StreamReader source = File.OpenText(fileName);
                StreamWriter dest = File.CreateText(res);

                if (treatmentID == 0)
                {
                    source.Close();
                    dest.Close();
                    fileStatusFlag = 1;
                    return fileName;
                }

                dest.WriteLine("Domain,Price,ExpDate,Source,IdInSource,Age,Status");

                if (treatmentID == 1) //NJ
                    formatNamejet(source, dest,"Pre-Release");
                else if (treatmentID == 2) //SN
                {
                    source.ReadLine();
                    source.ReadLine();
                    formatSnapnames(source, dest,"Expiring");
                }
                else if (treatmentID == 7) //SN
                    formatSnapnames(source, dest,"Deleting");
                else if (treatmentID == 12)
                    formatGodaddy(source, dest, "Expiring");
                else if (treatmentID == 121)
                    formatGodaddy(source, dest, "Auction");
                else if (treatmentID == 14)
                    formatDynadotBackorder(source, dest);
                else if (treatmentID == 15)
                    formatDynadotExpired(source, dest);
                else if (treatmentID >= 16 && treatmentID <= 20)
                    formatDropcatch(source, dest, treatmentID - 16);
                else if (treatmentID == 21)
                    formatPool(source, dest);

                source.Close();
                dest.Close();
                File.Delete(fileName);

                fileStatusFlag = 1;
                return res;
            }
            catch (Exception ex)
            {
                fileStatusFlag = -1;
                return ex.Message;
            }
        }

        public void formatNamejet(StreamReader src, StreamWriter dest, string status)
        {
            string[] inLine;
            string outLine;

            while (!src.EndOfStream)
            {
                inLine = src.ReadLine().Split(',');
                outLine = inLine[0] + ",";
                outLine += inLine[2].Substring(1) + ",";
                inLine = inLine[1].Split('-');
                outLine += inLine[1] + "/" + inLine[2] + "/" + inLine[0] + " 00:00,";
                outLine += "NameJet,,," + status;
                dest.WriteLine(outLine);
            }
        }

        public void formatSnapnames(StreamReader src, StreamWriter dest,string status)
        {
            string inLine, outLine;
            int col;
            bool reachedCol;
            src.ReadLine();

            while (!src.EndOfStream)
            {
                inLine = src.ReadLine();
                outLine = "";
                col = 0;
                reachedCol = false;
                while (inLine.Length != 0)
                {
                    if (char.IsWhiteSpace(inLine.First<char>()))
                    {
                        if (col == 4)
                            break;
                        reachedCol = false;
                    }
                    else
                    {
                        if (reachedCol == false)
                        {
                            if (col != 3 && col != 0)
                                outLine += ',';
                            if (col == 3)
                                outLine += ' ';
                            col++;
                            reachedCol = true;
                        }
                        outLine += inLine.First<char>();
                    }
                    inLine = inLine.Substring(1);
                }
                outLine += ",SnapNames,,," + status;
                dest.WriteLine(outLine);
            }
        }

        public void formatGodaddy(StreamReader src, StreamWriter dest, string status)
        {
            string[] inLine;
            string outLine;
            int hour;

            src.ReadLine();
            src.ReadLine();

            while (!src.EndOfStream)
            {
                inLine = src.ReadLine().Split(',');
                outLine = inLine[0] + ",";
                outLine += inLine[4].Substring(1) + ",";

                outLine += inLine[3].Substring(0, 11) + AMPMtoTime(inLine[3].Substring(11)) + ",";
                outLine += "GoDaddy,";
                outLine += inLine[1] + ",";
                outLine += inLine[6];
                outLine += inLine[1] + "," + status;
                dest.WriteLine(outLine);
            }
        }

        public void formatDynadotBackorder(StreamReader src, StreamWriter dest)
        {
            string[] inLine;
            string outLine;
            string time;

            src.ReadLine();

            while (!src.EndOfStream)
            {
                inLine = src.ReadLine().Split(',');
                outLine = inLine[0] + ",";                  //domain
                outLine += inLine[4].Substring(1) + ",";    //price

                inLine = inLine[1].Split(' ');
                time = inLine[1];
                inLine = inLine[0].Split('/');
                outLine += inLine[1] + "/" + inLine[2] + "/" + inLine[0] + " " + time + ",";
                outLine += "DynaDot,,,BackOrder";
                dest.WriteLine(outLine);
            }
        }

        public void formatDynadotExpired(StreamReader src, StreamWriter dest)
        {
            string[] inLine;
            string outLine;
            string time;
            string age;

            src.ReadLine();

            while (!src.EndOfStream)
            {
                inLine = src.ReadLine().Split(',');
                outLine = inLine[0] + ",";
                outLine += inLine[1] + ",";
                age = inLine[10];

                inLine = inLine[4].Split(' ');
                time = inLine[1];
                inLine = inLine[0].Split('/');
                outLine += inLine[1] + "/" + inLine[2] + "/" + inLine[0] + " " + time + ",";
                outLine += "DynaDot,,";
                outLine += age + ",Expiring";
                dest.WriteLine(outLine);
            }
        }

        public void formatDropcatch(StreamReader src, StreamWriter dest, int dayCnt)
        {
            string[] inLine;
            string outLine;
            DateTime fetchDate = DateTime.Today;
            if (dayCnt != 0)
                fetchDate = fetchDate.AddDays(dayCnt);

            src.ReadLine();

            while (!src.EndOfStream)
            {
                inLine = src.ReadLine().Split(',');
                outLine = inLine[0] + ",";
                outLine += inLine[2] + ",";
                outLine += fetchDate.Month + "/" + fetchDate.Day + "/" + fetchDate.Year + " 00:00,";

                outLine += "DropCatch,,,Expiring";
                dest.WriteLine(outLine);
            }
        }

        public void formatPool(StreamReader src, StreamWriter dest)
        {
            string[] inLine;
            string outLine;

            while (!src.EndOfStream)
            {
                inLine = src.ReadLine().Split(',');
                outLine = inLine[0] + ",,";
                inLine = inLine[1].Split(' ');
                outLine += inLine[0] + " " + AMPMtoTime(inLine[1] + inLine[2]) + ",";
                outLine += "Pool,,,Deleting";
                dest.WriteLine(outLine);
            }
        }

        public string AMPMtoTime(string AMPM)
        {
            int hour = int.Parse(AMPM.Substring(0, 2));
            if (AMPM.Substring(6, 2).Equals("PM"))
            {
                if (hour != 12)
                    hour += 12;
            }
            else
                if (hour == 12)
                    hour = 0;
            return string.Format("{0,2}", hour).Replace(' ', '0') + AMPM.Substring(2, 3);
        }

        public string process(string fileName)
        {
            string res = null;
            try
            {
                fileStatusFlag = 0;
                //string extansion = fileName.Split('.').Last<string>();
                if (fileName.ToLower().EndsWith(".zip"))
                    res = unzip(fileName);
                else if (fileName.ToLower().EndsWith(".csv"))
                    res = csvToTxt(fileName);
                else if (fileName.ToLower().EndsWith(".txt"))
                    res = fileName;
                //else if (fileName.ToLower().EndsWith(".xml"))
                //res = xmlToTxt(fileName);

                fileStatusFlag = 1;
                return res;
            }
            catch (Exception e)
            {
                fileStatusFlag = -1;
                return e.Message;
            }
        }

        public string xmlToTxt(string fileName)
        {
            string resFileName = fileName.Substring(0, fileName.Length - 3) + ".txt";
            XmlDocument xml = new XmlDocument();
            StreamWriter output = new StreamWriter(resFileName);
            xml.Load(XmlReader.Create(File.OpenRead(fileName)));

            return resFileName;
        }

        public string unzip(string fileName)
        {
            int fileNameLength;
            string fileList = "";
            string sourceDir;
            string extFileName = null;
            FileStream file;

            fileNameLength = fileName.Split('\\').Last<string>().Length;
            sourceDir = fileName.Substring(0, fileName.Length - fileNameLength);

            int itemNo = 0;
            using (ZipFile zip = ZipFile.Read(fileName))
            {
                foreach (ZipEntry e in zip)
                {
                    if (itemNo == 0)
                        extFileName = sourceDir + fileName.Split('\\').Last<string>().Substring(0, fileNameLength - 4) + "." + e.FileName.Split('.').Last<string>();
                    else
                        extFileName = sourceDir + fileName.Split('\\').Last<string>().Substring(0, fileNameLength - 4) + "_" + itemNo + "." + e.FileName.Split('.').Last<string>();
                    File.Delete(extFileName);
                    e.ExtractExistingFile = ExtractExistingFileAction.OverwriteSilently;
                    file = File.Create(extFileName);
                    e.Extract(file);
                    if (fileList != "")
                        fileList += '\n';

                    fileList += extFileName;
                    file.Close();
                    itemNo++;
                }
            }
            File.Delete(fileName);
            return fileList;
        }

        public string csvToTxt(string fileName)
        {
            string newFile = fileName.Substring(0, fileName.Length - 4) + ".txt";

            File.Delete(newFile);
            File.Copy(fileName, newFile);
            File.Delete(fileName);
            return newFile;
        }

        public int FileStatusFlag { get; set; }
    }

}