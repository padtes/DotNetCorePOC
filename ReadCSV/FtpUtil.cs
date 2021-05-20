using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ReadCSV
{
    public class FtpUtil
    {
        private static readonly string moduleName = "FtpUtil";
        //Hostname: qm20.siteground.biz Username: Lsg@arubapalmsrealtors.com Password: z#s%)F(913@2A Port: 21
        private static string poc_ftp_host = "qm20.siteground.biz";
        private static string poc_ftp_user = "Lsg@arubapalmsrealtors.com";
        private static string poc_ftp_pwd = "z#s%)F(913@2A";
        private static int poc_ftp_port = 21;

        public static bool DownloadDir()
        {
            //ServicePointManager.Expect100Continue = true;
            //System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12; // | SecurityProtocolType.Ssl3;

            //string downloadFileLocation = "c:\\zunk";
            string ftpUrl = "ftp://" + poc_ftp_host + ":" + poc_ftp_port;

            try
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpUrl);
                request.Credentials = new NetworkCredential(poc_ftp_user, poc_ftp_pwd);
                request.Method = WebRequestMethods.Ftp.ListDirectory;
                request.UsePassive = true;
                var response = (FtpWebResponse)request.GetResponse();
                if (response == null)
                {
                    return false;
                }

                StreamReader streamReader = new StreamReader(response.GetResponseStream());

                List<string> directories = new List<string>();

                string line = streamReader.ReadLine();
                while (!string.IsNullOrEmpty(line))
                {
                    var lineArr = line.Split('/');
                    line = lineArr[lineArr.Count() - 1];
                    directories.Add(line);
                    line = streamReader.ReadLine();
                    Console.WriteLine(line);
                }

                streamReader.Close();

                //to download file
                //request.Method = WebRequestMethods.Ftp.DownloadFile;
                //Stream responseStream = response.GetResponseStream();
                //StreamReader reader = new StreamReader(responseStream);
                //Console.WriteLine(reader.ReadToEnd());

                //Console.WriteLine("Download Complete, status {0}", response.StatusDescription);

                //reader.Close();

                response.Close();


                Logger.WriteInfo(moduleName, "DD", 0, "Ok");
            }
            catch (Exception ex)
            {
                Logger.WriteEx(moduleName, "DownloadDir", 0, ex);
                return false;
            }

            return false;

        }

        public static bool DownloadDirSFTP()
        {
            try
            {
                var connectionInfo = new ConnectionInfo(poc_ftp_host, poc_ftp_port, poc_ftp_user, new PasswordAuthenticationMethod(poc_ftp_user, poc_ftp_pwd));
                using (var client = new SftpClient(connectionInfo))
                {
                    client.Connect();
                    var dirList = client.ListDirectory("");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteEx(moduleName, "DownloadDir", 0, ex);
                return false;
            }
            return true;
        }

    }
}
