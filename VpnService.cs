using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using fchvpn;
using HtmlAgilityPack;
using Models;
using Newtonsoft.Json;
using ObisoftNet.Encoders;
using ObisoftNet.Http;

[assembly: Dependency(nameof(startServiceAndroid),LoadHint.Default)]
namespace fchvpn
{
    
        public class startServiceAndroid : IStartService
        {
            public void StartForegroundServiceCompat()
            {
                var intent = new Intent(MainActivity.Instance, typeof(VpnLocalService));


                if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
                {
                    MainActivity.Instance.StartForegroundService(intent);
                }
                else
                {
                    MainActivity.Instance.StartService(intent);
                }

            }
        }

        [Service]
        public class VpnLocalService : Service
        {
        public override IBinder OnBind(Intent intent)
        {
            return null;
        }




        public const int SERVICE_RUNNING_NOTIFICATION_ID = 10000;
        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            // From shared code or in your PCL  

            try
            {

                var channelid = CreateNotificationChannel();
                string messageBody = "service starting";


                var notificationIntent = new Intent(this, typeof(MainActivity));
                var pendingIntent = PendingIntent.GetActivity(this, 0, notificationIntent, 0);

                var notification = new Notification.Builder(this, channelid)
                .SetContentTitle("Free Chunk Vpn")
                .SetContentText(messageBody)
                .SetSmallIcon(Resource.Drawable.logo)
                .SetOngoing(true)
                .SetContentIntent(pendingIntent)

                .Build();

                StartForeground(SERVICE_RUNNING_NOTIFICATION_ID, notification);
            }
            catch {

            }
            //do you work  
            //new BackgroundStartup();
            startServer();


            return StartCommandResult.Sticky;
        }


        string CreateNotificationChannel()
        {

            string channelid = "10111";

            if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            {
                // Notification channels are new in API 26 (and not a part of the  
                // support library). There is no need to create a notification  
                // channel on older versions of Android.  
                return null;
            }

            var channelName = "FchVpn";
            var channelDescription = "vpn free chunk";
            var channel = new NotificationChannel(channelid, channelName, NotificationImportance.Default)
            {
                Description = channelDescription
            };

            var notificationManager = (NotificationManager)GetSystemService(NotificationService);
            notificationManager.CreateNotificationChannel(channel);
            return channelid;
        }


        public static bool Loaded = false;
        public static bool Faild = false;

        public static HttpServer server;


        public void startServer()
        {
            try
            {
                if (server == null)
                {
                    server = new HttpServer(baseGet);
                    server.Run(7890);
                }
            }
            catch
            {
                
            }

            MainActivity.Instance.RunOnUiThread(() => {

                MainActivity.Instance.progressLoading.Visibility = ViewStates.Invisible;
                MainActivity.Instance.loadingInfo.Text = "Conectado Al Servidor!";

            });
        }

        public HttpSession MakeSessFromData(ChunkData data)
        {
            HttpSession sess = new HttpSession();

            var text = $"sid:{data.sid}\n";
            text += $"user:{data.username}\n";
            text += $"passw:{data.password}\n";
            /*
			Dispatcher.BeginInvokeOnMainThread(()=>{
			   		Toast.Show(text,Toast.ToastLength.Long);
			   	});
			*/
            if (data.sid == "moodle")
            {

                string login = $"{data.host}login/index.php";
                var html = sess.GetString(login);


                var tags = new Dictionary<string, string>();

                string anchor = "";
                string logintoken = "";

                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(html);

                var inputs = doc.DocumentNode.Descendants("input");

                foreach (HtmlNode inp in inputs)
                {
                    if (inp.Attributes["name"].Value == "anchor")
                        anchor = inp.Attributes["value"].Value;
                    if (inp.Attributes["name"].Value == "logintoken")
                        logintoken = inp.Attributes["value"].Value;
                }

                var payload = new Dictionary<string, string>();
                payload.Add("anchor", anchor);
                payload.Add("logintoken", logintoken);
                payload.Add("username", data.username);
                payload.Add("password", data.password);
                payload.Add("rememberusername", "1");

                if (logintoken == "") return null;

                var resp = sess.Post(login, data: payload) as HttpWebResponse;


                if (resp.ResponseUri.ToString() != login)
                {
                    return sess;
                }


            }
            else
            {

                string login = $"{data.host}index.php/{data.sid}/login";
                var html = sess.GetString(login);
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(html);

                string csrftoken = "";

                var inputs = doc.DocumentNode.Descendants("input");
                foreach (HtmlNode inp in inputs)
                {
                    if (inp.Attributes["name"].Value == "csrfToken")
                        csrftoken = inp.Attributes["value"].Value;

                }

                if (csrftoken == "") return null;

                var payload = new Dictionary<string, string>();
                payload.Add("csrfToken", csrftoken);
                payload.Add("source", "");
                payload.Add("username", data.username);
                payload.Add("password", data.password);
                payload.Add("remember", "1");

                login += "/signIn";

                var resp = sess.Post(login, data: payload);

                if (resp.ResponseUri.ToString() != login)
                    return sess;

            }


            return null;
        }

        public ChunkData GetData(string url)
        {
            var sess = new HttpSession();


            string json = sess.GetString(url);

            ChunkData data = JsonConvert.DeserializeObject<ChunkData>(json);
            if (data != null)
            {
                data.username = S6Encoder.Decrypt(data.username);
                data.password = S6Encoder.Decrypt(data.password);
            }


            return data;
        }

        public static Dictionary<string, ChunkData> Datas = new Dictionary<string, ChunkData>();

        public  void baseGet(HttpListenerRequest req, HttpListenerResponse resp, RouteResult result)
        {
            try
            {
                if (result.Url.Contains("chunks"))
                {

                    string key = "obisoftdev2023";
                    string url = result.Url.Replace("127.0.0.1:7890", $"freechunkdl.s3.ydns.eu/get/{key}");



                    ChunkData data = null;

                    if (Datas.TryGetValue(url, out data)) { }
                    else
                    {
                        data = GetData(url);
                    }

                    bool geted = true;
                    if (data != null)
                    {

                        HttpSession sess = null;

                        if (data.session != null)
                        {
                            sess = data.session;
                        }
                        else
                        {
                            sess = MakeSessFromData(data);
                        }

                        if (sess != null)
                        {
                            try
                            {
                                data.session = sess;
                                Datas.Add(url, data);
                            }
                            catch { }


                            resp.ContentLength64 = (long)data.filesize;
                            resp.ContentType = "applicacion/octet-stream";
                            resp.Headers.Set("Content-Disposition", $"attachment; filename=\"{data.filename}\"");
                            resp.Headers.Add("Accept-Ranges", "bytes");

                            int ichunk = 0;
                            Dictionary<string, string> headers = new Dictionary<string, string>();
                            bool part = false;

                            long bmax = -1;
                            double length = 0;

                            try
                            {
                                // get range responses

                                string range = req.Headers.Get("Range").Replace("bytes=", "");

                                long bindex = long.Parse(range.Split('-')[0]);
                                try
                                {
                                    bmax = long.Parse(range.Split('-')[1]);
                                }
                                catch { }

                                if (bindex > 0)
                                {
                                    part = true;

                                    length = data.filesize - bindex;

                                    long rangesize = (long)data.filesize;

                                    if (bmax != -1)
                                    {
                                        length = bmax - bindex;
                                        rangesize = bmax;
                                    }

                                    resp.Headers.Add("Content-Range", $"bytes {bindex}-{rangesize}/{rangesize}");

                                    double chunkbytes = 1024 * 1024 * data.chunksize;

                                    ichunk = (int)Math.Round(bindex / (double)(1024 * 1024 * data.chunksize));

                                    float origin = bindex - (1024 * 1024 * data.chunksize * ichunk);
                                    headers.Add("Range", $"bytes={origin}-");

                                    resp.ContentLength64 = (long)(length);

                                    resp.StatusCode = 206;
                                }


                            }
                            catch
                            {

                            }

                            while (true)
                            {

                                if (!geted)
                                    data = GetData(url);

                                if (geted)
                                    geted = false;


                                long chunkcount = 0;

                                for (int i = ichunk; i < data.chunks.Length; i++)
                                {
                                    ichunk = i;
                                    string chunkurl = data.chunks[i];

                                    if (!part)
                                        headers = null;
                                    else
                                        part = false;

                                    var temp = sess.Get(chunkurl, headers: headers) as HttpWebResponse;


                                    using (Stream stream = temp.GetResponseStream())
                                    {
                                        byte[] bytes = new byte[1024];
                                        int read = 0;
                                        while ((read = stream.Read(bytes, 0, bytes.Length)) > 0)
                                        {
                                            resp.OutputStream.Write(bytes, 0, read);
                                            resp.OutputStream.Flush();

                                            chunkcount += read;

                                            if (bmax != -1)
                                            {
                                                if (chunkcount >= length)
                                                    break;
                                            }
                                        }
                                    }
                                }

                                if (bmax != -1)
                                {
                                    if (chunkcount >= length)
                                        break;
                                }
                                if (data.state == "finish") break;
                            }


                        }
                    }



                }

            }
            catch (Exception ex)
            {
                MainActivity.Instance.RunOnUiThread(() => {

                    Toast.MakeText(MainActivity.Instance,ex.Message,ToastLength.Long);
                
                });   
            }

        }

    }
    
}