using MySqlConnector;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HPC_Finish
{
    internal class DBConnection
    {
        //MySql Connection
        MySqlConnection conn = null;

        //General Data
        public string dataBase = "";
        public string query = "";
        string msg = "";
        int error = 0;

        private void connectBD()
        {
            try
            {
                //Open Connection
                conn = new MySqlConnection(dataBase);
                conn.Open();

                error = 0;
            }
            catch (Exception ex)
            {
                //Response
                msg = "Error al conectar a la Base de Datos.";
                error = 1;

                //Log
                File.AppendAllText(Directory.GetCurrentDirectory() + "\\errorLog.txt", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + ",Error al conectar a la Base de Datos:" + ex.Message + "\n");
            }
        }

        private void disconnectBD()
        {
            try
            {
                if (error == 0)
                {
                    //Close Connection
                    conn.Close();

                    //Control
                    error = 0;
                }
            }
            catch (Exception ex)
            {
                //Response
                msg = "Error al desconectar a la Base de Datos.";
                error = 1;

                //Log
                File.AppendAllText(Directory.GetCurrentDirectory() + "\\errorLog.txt", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + ",Error al desconectar a la Base de Datos:" + ex.Message + "\n");
            }
        }

        public MySqlDataAdapter getData(out string result, out int errors)
        {
            //Reader MySQL
            MySqlDataAdapter reader = new MySqlDataAdapter();

            //Connect
            connectBD();

            if (error == 0)
            {
                try
                {
                    //Execute Query
                    var command = new MySqlCommand(query, conn);
                    reader.SelectCommand = command;
                    error = 0;
                }
                catch (Exception ex)
                {
                    //Response
                    msg = "Error al consultar la Base de Datos.";
                    error = 1;

                    //Log
                    File.AppendAllText(Directory.GetCurrentDirectory() + "\\errorLog.txt", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + ",Error al consultar la Base de Datos:" + ex.Message + "\n");
                }
            }

            //Disconneect
            disconnectBD();

            //Response
            result = msg;
            errors = error;

            //Reader
            return reader;
        }

        public MySqlDataReader setData(out string result, out int errors)
        {
            //Reader MySQL
            MySqlDataReader reader = null;

            //Connect
            connectBD();

            if (error == 0)
            {
                try
                {
                    //Execute Query
                    var command = new MySqlCommand(query, conn);
                    reader = command.ExecuteReader();
                    error = 0;
                }
                catch (Exception ex)
                {
                    //Response
                    msg = "Error al consultar la Base de Datos.";
                    error = 1;

                    //Log
                    File.AppendAllText(Directory.GetCurrentDirectory() + "\\errorLog.txt", DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + ",Error al consultar la Base de Datos:" + ex.Message + "\n");
                }
            }

            //Disconnect
            disconnectBD();

            //Response
            result = msg;
            errors = error;

            //Reader
            return reader;
        }


    }
}