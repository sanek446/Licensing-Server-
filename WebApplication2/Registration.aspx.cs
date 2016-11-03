using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data;
using System.Configuration;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Net.Mail;
using System.Timers;
using System.ServiceModel;
using System.ServiceModel.Description;

namespace WebApplication2
{
    
    public partial class Registration : System.Web.UI.Page
    {
        string serverConn = "Data Source=ALEXANDR;Initial Catalog=SerialNumbers;Integrated Security = True";//??? local DB
        SqlConnection connection;
        string SQLCommand;
        private static System.Timers.Timer aTimer;

        protected void Page_Load(object sender, EventArgs e)
        {
            //=====TIMER
            // Create a timer with a 25 second interval.
             aTimer = new System.Timers.Timer(25000); //???

            // Hook up the Elapsed event for the timer.
              aTimer.Elapsed += new ElapsedEventHandler(ResetAllOnTime);

            // Set the Interval to 2 seconds (2000 milliseconds).
              aTimer.Interval = 25000; //???
              aTimer.Enabled = true;

              GC.KeepAlive(aTimer);

            //======WCF
            // We did not separate contract from implementation.
            // Therefor service and contract are the same in this example.
            try
            {
                Type serviceType = typeof(LicensingService);
                ServiceHost host = new ServiceHost(serviceType, new Uri[] { new Uri("http://localhost:8080/") }); //???

                // Add behavior for our MEX endpoint
                ServiceMetadataBehavior behavior = new ServiceMetadataBehavior();
                behavior.HttpGetEnabled = true;
                host.Description.Behaviors.Add(behavior);

                // Create basicHttpBinding endpoint at http://localhost:8080/LicensingService/ //???
                host.AddServiceEndpoint(serviceType, new BasicHttpBinding(), "LicensingService");
                                
                host.Open();
            }
            catch { }
        }

        public void RegisterBtn_Click(Object sender, EventArgs e)
        {
            string newSerialNumber = "";
            bool newTrial = false;
            string newPassword = "";
            bool newCanUse = false;

            Label1.ForeColor = System.Drawing.Color.Red;

            if (CheckFields() == false)
                return;

            //connect to SQL and check e-mail
            SQLCommand = "SELECT * FROM dbo.Numbers WHERE email = '" + email.Text + "'";
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(serverConn);
            using (connection = new SqlConnection(builder.ConnectionString))
            {
                try
                {
                    connection.Open();

                    SqlCommand cmd = new SqlCommand();
                    SqlDataReader reader;

                    cmd.CommandText = SQLCommand;
                    cmd.CommandType = CommandType.Text;
                    cmd.Connection = connection;
                    reader = cmd.ExecuteReader();

                    if (reader.HasRows == true)
                    {
                        Label1.Text = "E-mail already in use";
                        return;
                    }                  
                    reader.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    Label1.Text = "Some error. Contact your administrator.";
                    return;
                }
            }

            //connect to SQL and check serial number
            SQLCommand = "SELECT * FROM dbo.Numbers WHERE serialnumber = '" + serialnumber.Text + "'";            
                      using (connection = new SqlConnection(builder.ConnectionString))
                      {
                          try
                          {
                              connection.Open();

                              SqlCommand cmd = new SqlCommand();
                              SqlDataReader reader;

                              cmd.CommandText = SQLCommand;
                              cmd.CommandType = CommandType.Text;
                              cmd.Connection = connection;

                              reader = cmd.ExecuteReader();

                    if (reader.HasRows == false)
                    {
                        Label1.Text = "Serial number does not exist";
                        return;
                    }
                       while (reader.Read())
                       {
                        newSerialNumber = reader[0].ToString();                        
                        newCanUse = (bool) reader[1];
                        newTrial = (bool) reader[5];
                        }
                       reader.Close();
                   }
                          catch (Exception ex)
                          {
                            Console.WriteLine(ex.ToString());
                            Label1.Text = "Some error. Contact your administrator.";
                            return;
                }
                      }

            if (newCanUse == false)
            {
                Label1.Text = "This serial number cannot be used";
                return;
            }

            //update DB
            DateTime ExpirationDate;
            if (newTrial == true)
                ExpirationDate = DateTime.Now.AddDays(30);
            else
                ExpirationDate = DateTime.Parse("31/12/9999");

            SQLCommand = "UPDATE dbo.Numbers SET Email = '" + email.Text + "', Password = '" + CryptPassword(password.Text) + 
                "', ActivationDate = '" + DateTime.Now.ToString("yyyy-MM-dd") + "', ExpirationDate = '" + ExpirationDate.ToString("yyyy-MM-dd") + 
                "' WHERE SerialNumber = '" + serialnumber.Text + "'";

            if (UpdateBD(SQLCommand) == true)
            {
                Label1.ForeColor = System.Drawing.Color.Green;
                Label1.Text = "Serial number registered successfully";
            }
            else
            {
                Label1.ForeColor = System.Drawing.Color.Red;
                Label1.Text = "Some error. Contact your administrator.";
                return;
            }
        }

        public void UnRegisterBtn_Click(Object sender, EventArgs e)
        {
            string newSerialNumber = "";
            bool newTrial = false;
            string newPassword = "";
            bool newCanUse = false;

            Label1.ForeColor = System.Drawing.Color.Red;

            if (CheckFields() == false)
                return;

            //connect to SQL and check serial number
            SQLCommand = "SELECT * FROM dbo.Numbers WHERE serialnumber = '" + serialnumber.Text + "' AND email='" + email.Text + "'";
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(serverConn);
            using (connection = new SqlConnection(builder.ConnectionString))
            {
                try
                {
                    connection.Open();

                    SqlCommand cmd = new SqlCommand();
                    SqlDataReader reader;

                    cmd.CommandText = SQLCommand;
                    cmd.CommandType = CommandType.Text;
                    cmd.Connection = connection;

                    reader = cmd.ExecuteReader();

                    if (reader.HasRows == false)
                    {
                        Label1.Text = "Serial number or E-mail incorrect";
                        return;
                    }
                    while (reader.Read())
                    {
                        newSerialNumber = reader[0].ToString();
                        newCanUse = (bool) reader[1];
                        newPassword = reader[3].ToString();
                        newTrial = (bool) reader[5];
                    }
                    reader.Close();

                    if (newPassword != CryptPassword(password.Text))
                    {
                        Label1.Text = "Password is incorrect";
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    Label1.Text = "Some error. Contact your administrator.";
                    return;
                }
            }

            if (newCanUse == false)
            {
                Label1.Text = "This serial number cannot be used";
                return;
            }

            //update DB
            SQLCommand = "UPDATE dbo.Numbers SET Email = '', Password = '', ActivationDate = NULL, ExpirationDate = NULL WHERE SerialNumber = '" + serialnumber.Text + "'";
            if (UpdateBD(SQLCommand) == true)
                {
                Label1.ForeColor = System.Drawing.Color.Green;
                Label1.Text = "Serial number unregistered successfully";
                }
            else
                {
                Label1.ForeColor = System.Drawing.Color.Red;
                Label1.Text = "Some error. Contact your administrator.";
                return;
                }            
        }

        public void ForgotPasswordBtn_Click(Object sender, EventArgs e)
        {            
            string emailForRestore = "";
            emailForRestore = email.Text;

            string adminEmail = "alexandrli.mint@gmail.com"; //???
            string adminPassword = "101039almaty"; //???
            string newPassword = "";

            newPassword = GeneratePassword(5);

            //connect to SQL and check serial number
            SQLCommand = "SELECT * FROM dbo.Numbers WHERE serialnumber = '" + serialnumber.Text + "' AND email='" + email.Text + "'";
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(serverConn);
            using (connection = new SqlConnection(builder.ConnectionString))
            {
                try
                {
                    connection.Open();

                    SqlCommand cmd = new SqlCommand();
                    SqlDataReader reader;

                    cmd.CommandText = SQLCommand;
                    cmd.CommandType = CommandType.Text;
                    cmd.Connection = connection;

                    reader = cmd.ExecuteReader();

                    if (reader.HasRows == false)
                    {
                        Label1.ForeColor = System.Drawing.Color.Red;
                        Label1.Text = "Serial number or E-mail incorrect";
                        return;
                    }                   
                    reader.Close();                    
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    Label1.ForeColor = System.Drawing.Color.Red;
                    Label1.Text = "Some error. Contact your administrator.";
                    return;
                }
            }
            
            //update DB
            SQLCommand = "UPDATE dbo.Numbers SET Password = '" + CryptPassword(newPassword) + "' WHERE SerialNumber = '" + serialnumber.Text + "'";
            if (UpdateBD(SQLCommand) == true)
            {
                Label1.ForeColor = System.Drawing.Color.Green;
                Label1.Text = "Check your e-mail for new password";
            }
            else
            {
                Label1.ForeColor = System.Drawing.Color.Red;
                Label1.Text = "Some error. Contact your administrator.";
                return;
            }
            
            System.Net.Mail.MailMessage mail = new System.Net.Mail.MailMessage();
            mail.To.Add(emailForRestore);
            mail.From = new MailAddress(adminEmail, "RFQ2GO Administrator", System.Text.Encoding.UTF8);
            mail.Subject = "Password change";
            mail.SubjectEncoding = System.Text.Encoding.UTF8;
            mail.Body = "Your new password is: " + newPassword;
            mail.BodyEncoding = System.Text.Encoding.UTF8;
            mail.IsBodyHtml = true;
            mail.Priority = MailPriority.High;
            SmtpClient client = new SmtpClient();
            client.Credentials = new System.Net.NetworkCredential(adminEmail, adminPassword);
            client.Port = 587;
            client.Host = "smtp.gmail.com";
            client.EnableSsl = true;
            try
            {
                client.Send(mail);             
            }
            catch (Exception ex)
            {              
            }
        }

        private string GeneratePassword(int passwordLength)
        {
            string allowedChars = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNOPQRSTUVWXYZ0123456789!@$?_-";
            char[] chars = new char[passwordLength];
            Random rd = new Random();

            for (int i = 0; i < passwordLength; i++)
            {
                chars[i] = allowedChars[rd.Next(0, allowedChars.Length)];
            }

            return new string(chars);           
        }
                
        public void ResetAllOnTime(object source, ElapsedEventArgs e)
        {
            //connect to SQL and reset all records
            SQLCommand = "UPDATE dbo.Numbers SET Active = 'False' WHERE Active = 'True'";

            if (UpdateBD(SQLCommand) == true)
            {
                Label1.ForeColor = System.Drawing.Color.Green;
                Label1.Text = "Reset all successfully at: " + DateTime.Now.ToString();
            }
            else
            {
                Label1.ForeColor = System.Drawing.Color.Red;
                Label1.Text = "Some error. Contact your administrator.";                
            }
        }

        public bool CheckFields()
        {
            Label1.ForeColor = System.Drawing.Color.Red;
            //check all fields
            if (serialnumber.Text == "")
            {
                Label1.Text = "Please fill serial number";
                return false;
            }
            if (email.Text == "")
            {
                Label1.Text = "Please fill email";
                return false;
            }
            if (password.Text == "")
            {
                Label1.Text = "Please fill password";
                return false;
            }
            if (confpassword.Text == "")
            {
                Label1.Text = "Please fill confirmation password";
                return false;
            }

            //check if password and confpassword are equal
            if (password.Text != confpassword.Text)
            {
                Label1.Text = "Password and confirmation password are not equal";
                return false;
            }

            return true;
        }

        public bool UpdateBD(string SQLCommand)
        {            
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(serverConn);
            using (connection = new SqlConnection(builder.ConnectionString))
            {
                try
                {
                    connection.Open();

                    SqlCommand cmd = new SqlCommand();
                    SqlDataReader reader;

                    cmd.CommandText = SQLCommand;
                    cmd.CommandType = CommandType.Text;
                    cmd.Connection = connection;

                    reader = cmd.ExecuteReader();
                    reader.Close();

                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());                    
                    return false;
                }
            }            
        }

        public string CryptPassword(string password)
        {            
            // step 1, calculate MD5 hash from input password
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(password);

            byte[] hash = md5.ComputeHash(inputBytes);

            // step 2, convert byte array to hex string
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < hash.Length; i++)            
                sb.Append(hash[i].ToString("X2"));
            
            return sb.ToString();            
        }
    }


    [ServiceContract]
    class LicensingService
    {
        string serverConn = "Data Source=ALEXANDR;Initial Catalog=SerialNumbers;Integrated Security = True";//??? local DB
        SqlConnection connection;
        string SQLCommand;
        
        [OperationContract]
        bool WCFLogout(string email, string password)
        {
            //connect to SQL and reset all records
            SQLCommand = "UPDATE dbo.Numbers SET Active = 'False' WHERE email = '" + email + "' AND password = '" + password + "'";
            return UpdateBD(SQLCommand);
        }

        [OperationContract]
        //return values isOK, CanUse, Trial, NumberOfDays, Active
        Tuple<bool, bool, bool, int, bool> WCFLogin(string email, string password)
        {
            bool tupleCanUse = true;
            bool tupleTrial = false;
            int tupleNumberOfDays = 0;
            bool tupleActive = false;

            //connect to SQL and check serial number
            SQLCommand = "SELECT * FROM dbo.Numbers WHERE email = '" + email + "' AND password = '" + password + "'";
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(serverConn);
            using (connection = new SqlConnection(builder.ConnectionString))
            {
                try
                {
                    connection.Open();

                    SqlCommand cmd = new SqlCommand();
                    SqlDataReader reader;

                    cmd.CommandText = SQLCommand;
                    cmd.CommandType = CommandType.Text;
                    cmd.Connection = connection;

                    reader = cmd.ExecuteReader();

                    if (reader.HasRows == false)
                        return Tuple.Create(false, false, false, 0, false);
                    
                    while (reader.Read())
                    {                        
                        tupleCanUse = (bool)reader[1];
                        tupleTrial = (bool)reader[5];
                        tupleNumberOfDays = (int) ((DateTime) reader[6] - (DateTime) reader[4]).TotalDays;
                        tupleActive = (bool)reader[7];
                    }
                    reader.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());                    
                }
            }

            //connect to SQL and reset all records
            SQLCommand = "UPDATE dbo.Numbers SET Active = 'True' WHERE email = '" + email + "' AND password = '" + password + "'";
            if (UpdateBD(SQLCommand) == true)
            {
                return Tuple.Create(true, tupleCanUse, tupleTrial, tupleNumberOfDays, tupleActive);
            }
            else
                return Tuple.Create(false, false, false, 0, false);
        }
        

        [OperationContract]
        bool WCFChangePassword(string email, string oldPassword, string newPassword)
        {
            //connect to SQL and check serial number
            SQLCommand = "SELECT * FROM dbo.Numbers WHERE password = '" + oldPassword + "' AND email='" + email + "'";
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(serverConn);
            using (connection = new SqlConnection(builder.ConnectionString))
            {
                try
                {
                    connection.Open();

                    SqlCommand cmd = new SqlCommand();
                    SqlDataReader reader;

                    cmd.CommandText = SQLCommand;
                    cmd.CommandType = CommandType.Text;
                    cmd.Connection = connection;

                    reader = cmd.ExecuteReader();

                    if (reader.HasRows == false)                    
                        return false;
                    
                    reader.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());                    
                    return false;
                }
            }

            //update DB
            SQLCommand = "UPDATE dbo.Numbers SET Password = '" + newPassword + "' WHERE email = '" + email + "'";
            return UpdateBD(SQLCommand);                
        }

        [OperationContract]
        bool WCFChangeEmail(string oldEmail, string newEmail, string password)
        {
            //connect to SQL and check email and password
            SQLCommand = "SELECT * FROM dbo.Numbers WHERE password = '" + password + "' AND email='" + oldEmail + "'";
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(serverConn);
            using (connection = new SqlConnection(builder.ConnectionString))
            {
                try
                {
                    connection.Open();

                    SqlCommand cmd = new SqlCommand();
                    SqlDataReader reader;

                    cmd.CommandText = SQLCommand;
                    cmd.CommandType = CommandType.Text;
                    cmd.Connection = connection;

                    reader = cmd.ExecuteReader();

                    if (reader.HasRows == false)
                        return false;

                    reader.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    return false;
                }
            }

            //connect to SQL and check e-mail
            SQLCommand = "SELECT * FROM dbo.Numbers WHERE email = '" + newEmail + "'";
            using (connection = new SqlConnection(builder.ConnectionString))
            {
                try
                {
                    connection.Open();

                    SqlCommand cmd = new SqlCommand();
                    SqlDataReader reader;

                    cmd.CommandText = SQLCommand;
                    cmd.CommandType = CommandType.Text;
                    cmd.Connection = connection;
                    reader = cmd.ExecuteReader();

                    if (reader.HasRows == true)
                        return false;

                    reader.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    return false;
                }
            }

            //update DB
            SQLCommand = "UPDATE dbo.Numbers SET Email = '" + newEmail + "' WHERE email = '" + oldEmail + "'";
            return UpdateBD(SQLCommand);            
        }
        
        bool UpdateBD(string SQLCommand)
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(serverConn);
            using (connection = new SqlConnection(builder.ConnectionString))
            {
                try
                {
                    connection.Open();

                    SqlCommand cmd = new SqlCommand();
                    SqlDataReader reader;

                    cmd.CommandText = SQLCommand;
                    cmd.CommandType = CommandType.Text;
                    cmd.Connection = connection;

                    reader = cmd.ExecuteReader();

                    if (reader.RecordsAffected == 0)
                        return false;

                    reader.Close();

                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    return false;
                }
            }
        }

    }
}