using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using RestSharp;
using System.Data.SqlClient;
using Newtonsoft.Json;
using System.Timers;


namespace wsIntegracionGPS_BioSalc
{
    public partial class GPS_Biosalc : ServiceBase
    {
        SqlCommand cnn;
        private static Timer aTimer;
        SqlConnection cnx = new SqlConnection("Persist Security Info = False; User ID = servicios; Password = Service.1380; Initial Catalog = Biosalc; Server = 10.1.1.6\\amigodb");

        public GPS_Biosalc()
        {
            InitializeComponent();
            if (!System.Diagnostics.EventLog.SourceExists("MySource"))
            {
                System.Diagnostics.EventLog.CreateEventSource("MySource", "MyNewLog");
            }
            eventLog1.Source = "MySource";
            eventLog1.Log = "MyNewLog";
            SqlDataAdapter adp = new SqlDataAdapter("SELECT VALOR FROM TAB_DADOS  WHERE TABELA IN ('USINAS') AND CAMPO IN ('TIEMPO_GPS') AND  VLR_CHAVE1 IN (1)", cnx);
            DataTable dt = new DataTable();

            adp.Fill(dt);

            eventLog1.WriteEntry("Inicie");
            aTimer = new Timer();
            aTimer.Interval = Convert.ToDouble(dt.DefaultView[0][0].ToString()) * 60000;  //Valor en minutos desde la DB, multiplicado por 60,000( Equivalente a 1 minuto).
            aTimer.Elapsed += new ElapsedEventHandler(aTimerElapsed);
        }

        protected override void OnStart(string[] args)
        {
            aTimer.Enabled = true;

        }

        public void aTimerElapsed(object sender, EventArgs e)
        {
            Integracion();
        }

        protected override void OnStop()
        {
            eventLog1.WriteEntry("Final");
            aTimer.Enabled = false;
        }
        public void Integracion()
        {
            var client = new RestClient("http://rastreo.gps.hn:81");    //Dirección URL de la página.    
            var request = new RestRequest("/token", Method.POST);       // Solicitud por método POST, con los párametros.

            request.AddParameter("grant_type", "password");
            request.AddParameter("username", "cahsawebapi");
            request.AddParameter("password", "fd2633");
            request.AddParameter("client_id", "optimusApp");
            request.AddParameter("client_secret", "MobileAppOptimus");
            request.AddParameter("udid", "0");
            request.AddParameter("gcm_token", "0");

            // execute the request
            request.RequestFormat = DataFormat.Json;            //Convirtiendo la información a formato JSON.
            IRestResponse response = client.Execute(request);   //Ejecutando el Request.
            var content = response.Content;                     //Almacenando el contenido del Request.

            try
            {
                GetToken tk = JsonConvert.DeserializeObject<GetToken>(content.ToString());  //Pasando los valores a las variables de la clase GetToken(),a través del formato JSON.
                Data(tk.token_type, tk.access_token);                                       //ejecutando la función Data y pasando el Token_type y el access_Token.
            }
            catch (Exception ex)
            {
                if (cnx.State == ConnectionState.Closed)
                {
                    cnx.Open();
                    cnn = new SqlCommand("spBitacoraInsert @err", cnx);
                    cnn.Parameters.AddWithValue("@err", response.StatusDescription + "   " + ex.Message + "   " + DateTime.Now.ToLongTimeString());
                    cnn.ExecuteNonQuery();
                    cnx.Close();
                }
                else
                {
                    cnn = new SqlCommand("spBitacoraInsert @err", cnx);
                    cnn.Parameters.AddWithValue("@err", response.StatusDescription + "   " + ex.Message + "   " + DateTime.Now.ToLongTimeString());
                    cnn.ExecuteNonQuery();
                    cnx.Close();
                }
            }

        }

        public void Data(string TokenType, string Token)
        {
            var client = new RestClient("http://rastreo.gps.hn:81");                    //Dirección URL de la página.
            var request = new RestRequest("/api/Devices/ClientDevices", Method.GET);    // Solicitud por método GET, con los párametros por el Header.

            request.AddHeader("Accept", "application/jso");
            request.AddHeader("ContentType", "application/jso");
            request.AddHeader("Authorization", TokenType + " " + Token);


            // execute the request
            request.RequestFormat = DataFormat.Json;                //Convirtiendo el la información a formato JSON.
            IRestResponse response = client.Execute(request);       //Ejecutando el Request.
            var content = response.Content;                         //Almacenando el contenido del Request.

            getData data = JsonConvert.DeserializeObject<getData>(content.ToString());   //Creando un Array de Objetos, clase getData().

            //ciclo
            DetailData detail;
            int isOn = 0, isIdle = 0;

            cnx.Open();
            try
            {
                foreach (var dt in data.Data)   //
                {
                    detail = JsonConvert.DeserializeObject<DetailData>(dt.ToString());  //Convirtiendo cada objeto a los valores de la clase DetailData()

                    if (detail.isOn) { isOn = 1; }
                    if (detail.isIdle) { isIdle = 1; }

                    string sql = "spGPSapiInsert @vei, @fech, @lat, @lon, @vel, @eve, @isOn, @isIdle";
                    cnn = new SqlCommand(sql, cnx);
                    cnn.Parameters.AddWithValue("@vei", detail.description);
                    cnn.Parameters.AddWithValue("@fech", detail.date);
                    cnn.Parameters.AddWithValue("@lat", detail.latitude);
                    cnn.Parameters.AddWithValue("@lon", detail.longitude);
                    cnn.Parameters.AddWithValue("@vel", detail.speed);
                    cnn.Parameters.AddWithValue("@eve", string.Join(", ", detail.events.ToArray()));
                    cnn.Parameters.AddWithValue("@isOn", isOn);
                    cnn.Parameters.AddWithValue("@isIdle", isIdle);
                    try
                    {
                        cnn.ExecuteNonQuery();
                        isOn = 0; isIdle = 0;

                    }
                    catch (Exception ex)
                    {
                        if (cnx.State == ConnectionState.Closed)
                        {
                            cnx.Open();
                            cnn = new SqlCommand("spBitacoraInsert @err", cnx);
                            cnn.Parameters.AddWithValue("@err", response.StatusDescription + "   " + ex.Message + "   " + DateTime.Now.ToLongTimeString());
                            cnn.ExecuteNonQuery();
                            cnx.Close();
                        }
                        else
                        {
                            cnn = new SqlCommand("spBitacoraInsert @err", cnx);
                            cnn.Parameters.AddWithValue("@err", response.StatusDescription + "   " + ex.Message + "   " + DateTime.Now.ToLongTimeString());
                            cnn.ExecuteNonQuery();
                            cnx.Close();
                        }
                        throw;
                    }
                }

                ///Guardado
                if (cnx.State == ConnectionState.Closed)
                {
                    cnx.Open();
                    cnn = new SqlCommand("spBitacoraInsert @err", cnx);
                    cnn.Parameters.AddWithValue("@err", "Registros guardados exitosamente! " + DateTime.Now.ToLongTimeString());
                    cnn.ExecuteNonQuery();
                    cnx.Close();
                }
                else
                {
                    cnn = new SqlCommand("spBitacoraInsert @err", cnx);
                    cnn.Parameters.AddWithValue("@err", "Registros guardados exitosamente! " + DateTime.Now.ToLongTimeString());
                    cnn.ExecuteNonQuery();
                    cnx.Close();
                }
            }
            catch (Exception ex)
            {
                if (cnx.State == ConnectionState.Closed)
                {
                    cnx.Open();
                    cnn = new SqlCommand("spBitacoraInsert @err", cnx);
                    cnn.Parameters.AddWithValue("@err", response.StatusDescription + "   " + ex.Message + "   " + DateTime.Now.ToLongTimeString());
                    cnn.ExecuteNonQuery();
                    cnx.Close();
                }
                else
                {
                    cnn = new SqlCommand("spBitacoraInsert @err", cnx);
                    cnn.Parameters.AddWithValue("@err", response.StatusDescription + "   " + ex.Message + "   " + DateTime.Now.ToLongTimeString());
                    cnn.ExecuteNonQuery();
                    cnx.Close();
                }

            }
            if (cnx.State == ConnectionState.Open)
            {
                cnx.Close();
            }

        }
        //https://www.newtonsoft.com/json/help/html/SerializeObject.htm
        public class GetToken
        {
            public string access_token { get; set; }
            public string token_type { get; set; }
            public int expires_in { get; set; }
            public string refresh_token { get; set; }
            public string client_id { get; set; }
            public string userName { get; set; }
            public string roles { get; set; }
            public string changePassword { get; set; }
            public string companyId { get; set; }
            public string issued { get; set; }
            public string expires { get; set; }
        }
        public class getData
        {
            public IList<Object> Data { get; set; }
        }
        public class DetailData
        {
            public string description { get; set; }
            public string gpsId { get; set; }
            public string date { get; set; }
            public float latitude { get; set; }
            public float longitude { get; set; }
            public float speed { get; set; }
            public bool isOn { get; set; }
            public bool isIdle { get; set; }
            public List<string> events { get; set; }
        }
    }
}
