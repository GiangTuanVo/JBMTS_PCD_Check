using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace JBMTS_PCD_Check.Model
{
    public class FileHelper
    {
        public static string GetQty(string po)
        {
            string result = string.Empty;
            SqlConnection connection = new SqlConnection("Data Source=10.4.24.114;Initial Catalog=DailyReportF4;User ID=user;Password=user");
            string query = string.Format(@"SELECT CONVERT(int,Min([LNMGA])) FROM [DailyReportF4].[dbo].[MANUFASPCPD_DT_JDAY]  where AUFNR='{0}' and Edate>0", po);
            try
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand(query, connection);
                result = cmd.ExecuteScalar().ToString();
                connection.Close();
            }
            catch (Exception)
            {

            }

            if (String.IsNullOrEmpty(result))
            {
                query = string.Format(@"SELECT CONVERT(int, AMNT - badcnt)  FROM[DailyReportF4].[dbo].[MANUFASPCPD_DT_ASSIST]  where AUFNR = '{0}'", po);
                try
                {
                    connection.Open();
                    SqlCommand cmd = new SqlCommand(query, connection);
                    result = cmd.ExecuteScalar().ToString();
                    connection.Close();
                }
                catch (Exception)
                {

                }
            }
            // [dbo].[MANUFASPCPD_DT_REQ_HED]
            if (String.IsNullOrEmpty(result))
            {
                query = string.Format(@"SELECT CONVERT(int, [GAMNG]) FROM[DailyReportF4].[dbo].[MANUFASPCPD_DT_REQ_HED]  where AUFNR = '{0}'", po);
                try
                {
                    connection.Open();
                    SqlCommand cmd = new SqlCommand(query, connection);
                    result = cmd.ExecuteScalar().ToString();
                    connection.Close();
                }
                catch (Exception)
                {

                }
            }

            if (String.IsNullOrEmpty(result))
            {
                return "0";
            }
            else
            {
                return result;
            }
        }
        public static void FileToTBL(string folder, string fileName, DataSet ds, DataTable tbl)
        {
            int num1 = 0;
            int num2 = 0;
            try
            {
                if (!File.Exists(System.IO.Path.Combine(folder, fileName)))
                    return;
                StreamReader streamReader = new StreamReader(System.IO.Path.Combine(folder, fileName), Encoding.Default);
                while (streamReader.Peek() >= 0)
                {
                    string str = streamReader.ReadLine();
                    if (Operators.CompareString(str, "", false) != 0)
                    {
                        string[] strArray = Strings.Split(str, ",");
                        if (num1 == 0)
                        {
                            num2 = checked(Information.UBound((Array)strArray) + 1);
                            int num3 = Information.UBound((Array)strArray);
                            int index = 0;
                            while (index <= num3)
                            {
                                tbl.Columns.Add(strArray[index], typeof(string));
                                checked { ++index; }
                            }
                        }
                        else
                        {
                            if (checked(Information.UBound((Array)strArray) + 1) != num2)
                            {
                                MessageBox.Show("err15:csv file error \r\n" +
                                    "fileName=" + System.IO.Path.Combine(folder, fileName) +
                                    "line=" + Conversions.ToString(checked(num1 + 1)), "Notification", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                            DataRow row = tbl.NewRow();
                            int num3 = Information.UBound((Array)strArray);
                            int columnIndex = 0;
                            while (columnIndex <= num3)
                            {
                                row[columnIndex] = (object)strArray[columnIndex];
                                checked { ++columnIndex; }
                            }
                            tbl.Rows.Add(row);
                        }
                        checked { ++num1; }
                    }
                }
                streamReader.Close();
                ds.Tables.Add(tbl);
            }
            catch (Exception ex)
            {
                throw (ex);
            }
        }
    }
}
