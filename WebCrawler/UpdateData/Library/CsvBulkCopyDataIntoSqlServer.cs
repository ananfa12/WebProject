using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WebCrawler
{
    public class CsvBulkCopyDataIntoSqlServer
    {
        protected const string _truncateLiveTableCommandText = @"TRUNCATE TABLE tbDomainsFromSrc";
        protected const int _batchSize = 100000;
        private string _connectionString = ConfigurationManager.AppSettings["SQLServer"];
        private string _outputFolder = ConfigurationManager.AppSettings["OutputFolder"];

        public void UpdateAlexaTable(string fileName, bool withTruncate)
        {
            using (var textFieldParser = new TextFieldParser(fileName))
            {
                    textFieldParser.TextFieldType = FieldType.Delimited;
                    textFieldParser.Delimiters = new[] { "," };
                    textFieldParser.HasFieldsEnclosedInQuotes = true;

                    var dataTable = new DataTable("tbAlexa");

                    // Add the columns in the temp table
                    dataTable.Columns.Add("Id");
                    dataTable.Columns.Add("Domain");

                    using (var sqlConnection = new SqlConnection(_connectionString))
                    {
                        sqlConnection.Open();
                        if (withTruncate)
                        {
                            var _truncateCommand = @"TRUNCATE TABLE Domains";
                            // Truncate the Alexa table
                            using (var sqlCommand = new SqlCommand(_truncateCommand, sqlConnection))
                            {
                                sqlCommand.ExecuteNonQuery();
                            }
                        }

                        // Create the bulk copy object
                        var sqlBulkCopy = new SqlBulkCopy(sqlConnection)
                        {
                            DestinationTableName = "Domains"
                        };
                        sqlBulkCopy.ColumnMappings.Add("Id", "Id");
                        sqlBulkCopy.ColumnMappings.Add("Domain", "Domain");
                        var createdCount = 0;
                        while (!textFieldParser.EndOfData)
                        {
                            string[] temp = textFieldParser.ReadFields();
                            dataTable.Rows.Add(temp);
                            createdCount++;

                            if (createdCount % _batchSize == 0 && createdCount > 0)
                            {
                                InsertDataTable(sqlBulkCopy, sqlConnection, dataTable);
                                //break;
                            }
                        }

                        // Don't forget to send the last batch under 100,000
                        InsertDataTable(sqlBulkCopy, sqlConnection, dataTable);
                        sqlConnection.Close();
                    }



            }
           
        }

        public int LoadCsvDataIntoSqlServer(string fileName, bool withTruncate)
        {
            using (var db = new DomainsEntities())
            {
                var srcs = db.tbSrcs.ToDictionary(g => g.Name,g => g.Id);
                var sts = db.tbStatus.ToDictionary(g => g.Name, g => g.Id);

            var createdCount = 0;
            var fn = string.Format("{0}\\{1}", _outputFolder, fileName);
            using (var textFieldParser = new TextFieldParser(fn))
            {
                textFieldParser.TextFieldType = FieldType.Delimited;
                textFieldParser.Delimiters = new[] { "," };
                textFieldParser.HasFieldsEnclosedInQuotes = true;

                var dataTable = new DataTable("tbDomainsFromSrc");

                // Add the columns in the temp table
                dataTable.Columns.Add("Domain");
                dataTable.Columns.Add("Price");
                dataTable.Columns.Add("ExpDate");//, typeof(DateTime));
                dataTable.Columns.Add("Source",typeof(Int32));
                dataTable.Columns.Add("IdInSource");
                dataTable.Columns.Add("Age");
                dataTable.Columns.Add("Status",typeof(Int32));
                dataTable.Columns.Add("SrcFile");
                //
                dataTable.Columns.Add("Name");
                dataTable.Columns.Add("Tld");
                dataTable.Columns.Add("Length");
                

                using (var sqlConnection = new SqlConnection(_connectionString))
                {
                    sqlConnection.Open();

                    if (withTruncate)
                    {
                        // Truncate the live table
                        using (var sqlCommand = new SqlCommand(_truncateLiveTableCommandText, sqlConnection))
                        {
                            sqlCommand.ExecuteNonQuery();
                        }
                    }

                    // Create the bulk copy object
                    var sqlBulkCopy = new SqlBulkCopy(sqlConnection)
                    {
                        DestinationTableName = "tbDomainsFromSrc"
                    };

                    // Setup the column mappings, anything ommitted is skipped
                    sqlBulkCopy.ColumnMappings.Add("Domain", "Domain");
                    sqlBulkCopy.ColumnMappings.Add("Price", "Price");
                    sqlBulkCopy.ColumnMappings.Add("ExpDate", "ExpDate");
                    sqlBulkCopy.ColumnMappings.Add("Source", "Source");
                    sqlBulkCopy.ColumnMappings.Add("IdInSource", "IdInSource");
                    sqlBulkCopy.ColumnMappings.Add("Age", "Age");
                    sqlBulkCopy.ColumnMappings.Add("SrcFile", "SrcFile");
                    sqlBulkCopy.ColumnMappings.Add("Status", "Status");
                    // 
                    sqlBulkCopy.ColumnMappings.Add("Name", "Name");
                    sqlBulkCopy.ColumnMappings.Add("Tld", "Tld");
                    sqlBulkCopy.ColumnMappings.Add("Length", "Length");
                    

                    // Loop through the CSV and load each set of 100,000 records into a DataTable
                    // Then send it to the LiveTable
                    while (!textFieldParser.EndOfData)
                    {
                        string[] temp = textFieldParser.ReadFields();
                        if (createdCount > 0)
                        {
                            temp[0] = temp[0].ToLower();
                            if (temp.Length > 7)
                            {
                                temp = MergeItemsInArray(temp, 1);// for price with comma
                            }

                            // add src
                            temp = temp.AddItemToArray(fileName.Replace(".csv", ""));
                            // prepare date
                            temp[2] = GetDateValue(temp[2]);

                            // src & status
                            temp[3] = Convert.ToString(srcs[temp[3]]);
                            temp[6] = Convert.ToString(sts[temp[6]]);

                            // 3 calc fields
                            var name = temp[0].Split('.')[0];
                            var tld = temp[0].Substring(temp[0].IndexOf('.') + 1);
                            temp = temp.AddItemToArray(name);
                            temp = temp.AddItemToArray(tld);
                            temp = temp.AddItemToArray(name.Length.ToString());

                            //temp[temp.Length] = fileName;
                            dataTable.Rows.Add(temp);
                            //dataTable.Rows.Add(textFieldParser.ReadFields());
                        }
                        createdCount++;

                        if (createdCount % _batchSize == 0 && createdCount > 0)
                        {
                            InsertDataTable(sqlBulkCopy, sqlConnection, dataTable);
                            //break;
                        }
                    }

                    // Don't forget to send the last batch under 100,000
                    InsertDataTable(sqlBulkCopy, sqlConnection, dataTable);
                    sqlConnection.Close();
                    return createdCount; // dataTable.Rows.Count;
                }
            }
            }
        }

        private string GetDateValue(string p)
        {
            DateTime dateTime;
            if (DateTime.TryParse(p, out dateTime))
            {
                return ConvertToUnixTimestamp(dateTime).ToString();
            }
            else
            {
                return "error";
            }
        }

        // merge and remove comma
        private string[] MergeItemsInArray(string[] original, int n)
        {
            string[] finalArray = new string[original.Length - 1];
            for (int i = 0; i < original.Length - 1; i++)
            {
                if (i < n - 1)
                {
                    finalArray[i] = original[i];
                }
                else if (i == n)
                {
                    finalArray[i] = string.Format("{0}{1}", original[i], original[i + 1]).Replace(",", "");
                }
                else
                {
                    finalArray[i] = original[i + 1];
                }

            }
            return finalArray;
        }


        protected void InsertDataTable(SqlBulkCopy sqlBulkCopy, SqlConnection sqlConnection, DataTable dataTable)
        {
            sqlBulkCopy.WriteToServer(dataTable);
            dataTable.Rows.Clear();
        }

        static double ConvertToUnixTimestamp(DateTime date)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            TimeSpan diff = date - origin;
            return Math.Floor(diff.TotalSeconds);
        }

    }


    public static class Extensions
    {
        public static T[] AddItemToArray<T>(this T[] original, T itemToAdd)
        {
            T[] finalArray = new T[original.Length + 1];
            for (int i = 0; i < original.Length; i++)
            {
                finalArray[i] = original[i];
            }
            finalArray[finalArray.Length - 1] = itemToAdd;
            return finalArray;
        }


    }
}