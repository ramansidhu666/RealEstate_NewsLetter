using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace RealEstate_NewsLetter
{
    class Program
    {

        static string qry = "";
        static string OrderNo = "";
        static string Email = "";
        static string NewsLetterUrl = "";
        static string Name = "";
        static string FirstImage = "";
        static string SecondImage = "";
        static int NewsLetterId = 0;
        static int UserId = 0;
        static bool IsNewsLetterSendOrNOt = true;

        static void Main(string[] args)
        {
            //for send newsletter to rajpal clients
            SqlConnection rajpal_conn = new SqlConnection("Data Source=74.208.69.145;Initial Catalog=Rajpal_Realtors;User ID=sa;Password=!nd!@123");
            Execute_NewsLetter(rajpal_conn);

            //for send newsletter to davinder garcha clients
            //SqlConnection garcha_conn = new SqlConnection("Data Source=74.208.69.145;Initial Catalog=Davinder_Garcha;User ID=sa;Password=!nd!@123");
            //Execute_NewsLetter(garcha_conn);
        }

        public static void Execute_NewsLetter(SqlConnection conn)
        {
            qry = "";
            qry = "select top(100) *  from AdminClient where IsEmailSend=0 or IsEmailSend is null";
            DataTable dt = GetdataTable(qry, conn);

            foreach (DataRow row in dt.Rows)
            {
                if (row["EmailId"] != System.DBNull.Value && row["EmailId"] != "")
                {

                    var Email = row["EmailId"].ToString();
                    var Name = row["Name"].ToString();
                    var AdminId = Convert.ToInt32(row["ID"]);


                    qry = "";
                    qry = "select *  from NewsLetter where IsActive=1";
                    DataTable NewsLetter_dt = GetdataTable(qry, conn);
                    var todayDate = DateTime.Now;
                    if (NewsLetter_dt != null)
                    {
                        foreach (DataRow NewsLetter in NewsLetter_dt.Rows)
                        {
                            var NewsLetterPath = NewsLetter["Image"].ToString();

                            if (NewsLetter["second_image"].ToString() != null && NewsLetter["second_image"].ToString() != "")
                            {
                                NewsLetterPath += "," + NewsLetter["second_image"].ToString();
                            }
                            var NewsLetterDate = NewsLetter["fwd_date"].ToString();
                            //var SelectedUsers = NewsLetter["SelectedUsers"].ToString();
                            var OrderNo = NewsLetter["OrderNo"].ToString();
                            var diffi = todayDate - Convert.ToDateTime(NewsLetterDate);

                            //this functionality set bqs gmail provide only 100 emails free in one day. 
                            if (diffi.Days < 15 && diffi.Days >= 0)
                            {
                                SendNewsLetter(Email, NewsLetterPath);
                                //Update in Users table for send conform emails.
                                if (AdminId != 0 && AdminId != null)
                                {
                                    qry = "";
                                    qry = "Update AdminClient set IsEmailSend=1 where ID=" + AdminId + " ";
                                    ExecuteNonQuery(qry, conn);
                                }
                                //End


                                if (diffi.Days == 14)
                                {
                                    int OdrNo = 0;
                                    if (OrderNo != "")
                                    {
                                        OdrNo = Convert.ToInt32(OrderNo);
                                        OdrNo++;
                                    }
                                    //update the old orderNo.
                                    qry = "";
                                    qry = "Update NewsLetter set IsActive=null where IsActive=1";
                                    ExecuteNonQuery(qry, conn);

                                    //Set the new orderNo.
                                    qry = "";
                                    qry = "Update NewsLetter set IsActive=1 where OrderNo=" + OdrNo + "";
                                    ExecuteNonQuery(qry, conn);
                                }
                            }
                        }
                    }

                }

            }

        }

        public static void ReadyToNextNewsLetter(SqlConnection con)
        {


            int OdrNo = 0;
            if (OrderNo != "")
            {
                OdrNo = Convert.ToInt32(OrderNo);
                OdrNo++;
            }
            //Set the new orderNo.
            qry = "";
            qry = "Update NewsLetter set IsActive=1 where OrderNo=" + OdrNo + "";
            ExecuteNonQuery(qry, con);

        }

        public static DataTable GetdataTable(string qry, SqlConnection con)
        {
            DataTable dt = new DataTable();
            SqlDataAdapter NewsLetter_Adp = new SqlDataAdapter(qry, con);
            NewsLetter_Adp.Fill(dt);

            return dt;

        }

        public static string ExecuteNonQuery(string QStr, SqlConnection con)
        {
            string ErrorMessage = "";
            SqlCommand cmd = null;
            try
            {

                if (con.State == ConnectionState.Open)
                {
                    con.Close();
                }
                con.Open();
                cmd = new SqlCommand(QStr, con);
                cmd.CommandType = CommandType.Text;
                cmd.ExecuteNonQuery();
                con.Close();

                ErrorMessage = "";
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("The DELETE statement conflicted with the REFERENCE constraint"))
                {
                    ErrorMessage = "FK";
                }
            }
            finally
            {
                if (cmd != null)
                {
                    cmd.Dispose();
                }
                if (con != null)
                {
                    con.Close();
                }

            }

            return ErrorMessage;
        }

        public static bool SendNewsLetter(string Email, string Path)
        {
            //Email = "Only4agentss@gmail.com";
            //Send mail
            bool status = false;
            MailMessage mail = new MailMessage();
            var FirstImg = "";
            var SecondImg = "";
            if (Path.IndexOf(',') > -1)
            {
                var splitedpath = Path.Split(',');
                FirstImg = splitedpath[0].ToString();
                SecondImg = splitedpath[1].ToString();
            }
            else
            {
                FirstImg = Path;
            }


            try
            {
                string FromEmailID = ConfigurationManager.AppSettings["FromEmailID"];
                string FromEmailPassword = ConfigurationManager.AppSettings["FromEmailPassword"];

                SmtpClient smtpClient = new SmtpClient(ConfigurationManager.AppSettings["SmtpServer"]);
                int _Port = Convert.ToInt32(ConfigurationManager.AppSettings["Port"].ToString());
                Boolean _UseDefaultCredentials = Convert.ToBoolean(ConfigurationManager.AppSettings["UseDefaultCredentials"].ToString());
                Boolean _EnableSsl = Convert.ToBoolean(ConfigurationManager.AppSettings["EnableSsl"].ToString());
                mail.To.Add(new MailAddress(Email));
                mail.From = new MailAddress(FromEmailID);
                mail.Subject = "NewsLetter";
                string msgbody = "";

                using (StreamReader reader = new StreamReader(@"C:\Sites\Paramsites\RealEstate_NewsLetter\RealEstate_NewsLetter\Templates\SixNewLetter.html"))
                {
                    msgbody = reader.ReadToEnd();
                    msgbody = msgbody.Replace("{FirstImg}", FirstImg);
                    msgbody = msgbody.Replace("{SecondImg}", SecondImg);
                }

                mail.BodyEncoding = System.Text.Encoding.UTF8;
                mail.SubjectEncoding = System.Text.Encoding.UTF8;
                System.Net.Mail.AlternateView plainView = System.Net.Mail.AlternateView.CreateAlternateViewFromString(System.Text.RegularExpressions.Regex.Replace(msgbody, @"<(.|\n)*?>", string.Empty), null, "text/plain");
                System.Net.Mail.AlternateView htmlView = System.Net.Mail.AlternateView.CreateAlternateViewFromString(msgbody, null, "text/html");

                mail.AlternateViews.Add(plainView);
                mail.AlternateViews.Add(htmlView);
                // mail.Body = msgbody;
                mail.IsBodyHtml = true;
                SmtpClient smtp = new SmtpClient();
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtp.Host = "smtp.gmail.com";
                smtp.Port = _Port;
                smtp.Credentials = new System.Net.NetworkCredential(FromEmailID, FromEmailPassword);// Enter senders User name and password
                smtp.EnableSsl = _EnableSsl;
                smtp.Send(mail);
                status = true;

            }
            catch
            {
                status = false;

            }

            return status;
        }
    }
}
