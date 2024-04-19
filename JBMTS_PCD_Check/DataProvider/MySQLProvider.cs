using MySql.Data.MySqlClient;
using System.Configuration;
using System.Data;
using System.Linq;

namespace JBMTO_PCD_Check.Provider
{
    public class MySQLProvider
    {
        private static string connStr = ConfigurationManager.AppSettings["CheckPCD_JigbushConnection"];
        private static MySQLProvider instance;
        public static MySQLProvider Instance
        {
            get
            {
                if (instance == null) instance = new MySQLProvider(); return instance;
            }
            set
            {
                instance = value;
            }
        }

        public DataTable ExecuteQuery(string query, object[] parameter = null)
        {
            DataTable data = new DataTable();

            using (MySqlConnection connection = new MySqlConnection(connStr))
            {
                connection.Open();

                MySqlCommand command = new MySqlCommand(query, connection);

                if (parameter != null)
                {
                    string[] listPara = query.Split(' ');
                    int i = 0;
                    foreach (string item in listPara)
                    {
                        if (item.Contains('@'))
                        {
                            command.Parameters.AddWithValue(item, parameter[i]);
                            i++;
                        }
                    }
                }

                MySqlDataAdapter adapter = new MySqlDataAdapter(command);

                adapter.Fill(data);

                connection.Close();
            }

            return data;
        }

        public int ExecuteNonQuery(string query, object[] parameter = null)
        {
            int data = 0;

            using (MySqlConnection connection = new MySqlConnection(connStr))
            {
                connection.Open();

                MySqlCommand command = new MySqlCommand(query, connection);

                if (parameter != null)
                {
                    string[] listPara = query.Split(' ');
                    int i = 0;
                    foreach (string item in listPara)
                    {
                        if (item.Contains('@'))
                        {
                            command.Parameters.AddWithValue(item, parameter[i]);
                            i++;
                        }
                    }
                }

                data = command.ExecuteNonQuery();

                connection.Close();
            }

            return data;
        }

        public object ExecuteScalar(string query, object[] parameter = null)
        {
            object data = 0;

            using (MySqlConnection connection = new MySqlConnection(connStr))
            {
                connection.Open();

                MySqlCommand command = new MySqlCommand(query, connection);

                if (parameter != null)
                {
                    string[] listPara = query.Split(' ');
                    int i = 0;
                    foreach (string item in listPara)
                    {
                        if (item.Contains('@'))
                        {
                            command.Parameters.AddWithValue(item, parameter[i]);
                            i++;
                        }
                    }
                }

                data = command.ExecuteScalar();

                connection.Close();
            }

            return data;
        }
    }
}