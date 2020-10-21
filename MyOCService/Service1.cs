using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.ServiceProcess;
using System.Threading;
using System.Timers;

namespace MyOCService
{
    [DataContract]
    public class PostText
    {
        [DataMember]
        internal string ID;
        [DataMember]
        internal string Text;
    }

    [DataContract]
    public class PostURLs
    {
        [DataMember]
        internal string ID;
        [DataMember]
        internal string[] TextURLs;
    }

    [DataContract]
    public class PostPictures
    {
        [DataMember]
        internal string ID;
        [DataMember]
        internal string[] PictureURLs;
    }

    public partial class Service1 : ServiceBase
    {
        public Service1()
        {
            InitializeComponent();
        }

        public System.Timers.Timer timer1 = new System.Timers.Timer();
        public System.Timers.Timer timer2 = new System.Timers.Timer();

        protected override void OnStart(string[] args)
        {
            if (File.Exists(serviceFile)) { File.Delete(serviceFile); }
            if (File.Exists(pathLog)) { File.Delete(pathLog); }
            using (FileStream fs = new FileStream(serviceLive, FileMode.Create))
            {
                fs.Close();
                fs.Dispose();
            }
            timer1.Elapsed += Timer1_Tick;
            timer1.Interval = 1000;
            timer1.Enabled = true;
            timer1.AutoReset = true;
            timer1.Start();
            using (StreamWriter sw = new StreamWriter(pathLog, true, System.Text.Encoding.Default))
            {
                sw.WriteLine("[" + DateTime.Now.ToString() + "] Началась работа службы.");
            }
        }

        Thread threadOne;
        Thread threadTwo;
        Thread threadThree;
        private const string textFile = @"C:\Users\Maxim\source\repos\MyOCProject\PostData\PostText.json";
        private const string urlFile = @"C:\Users\Maxim\source\repos\MyOCProject\PostData\PostURL.json";
        private const string pictureFile = @"C:\Users\Maxim\source\repos\MyOCProject\PostData\PostPicture.json";
        private const string programmFile = @"C:\Users\Maxim\source\repos\MyOCProject\PostData\ProgramRunning.txt";
        private const string serviceFile = @"C:\Users\Maxim\source\repos\MyOCProject\PostData\ServiceRunning.txt";
        private const string pathLog = @"C:\Users\Maxim\source\repos\MyOCProject\PostData\ServiceLog\ServiceLog.txt";
        private const string serviceLive = @"C:\Users\Maxim\source\repos\MyOCProject\PostData\ServiceLog\ServiceIsAlive.txt";

        public Semaphore Sem { get; } = new Semaphore(1, 1);
        private List<PostPictures> TmpPicture { get; set; } = new List<PostPictures>();
        private List<PostURLs> TmpURL { get; set; } = new List<PostURLs>();
        private List<PostText> TmpText { get; set; } = new List<PostText>();

        private void EventScheduler()
        {
            threadOne = new Thread(() => PostTextDeserialization(textFile));
            threadTwo = new Thread(() => PostURLDeserialization(urlFile));
            threadThree = new Thread(() => PostPictureDeserialization(pictureFile));
            using (StreamWriter sw = new StreamWriter(pathLog, true, System.Text.Encoding.Default))
            {
                sw.WriteLine("[" + DateTime.Now.ToString() + "] Началась работа метода инициализирующего считывающие потоки.");
            }
            threadOne.Start();
            threadTwo.Start();
            threadThree.Start();

        }
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void AddPicture()
        {
            if (!TmpPicture.Any()) return;
            List<PostPictures> posts = TmpPicture;
            Sem.WaitOne();
            using (StreamWriter sw = new StreamWriter(pathLog, true, System.Text.Encoding.Default))
            {
                sw.WriteLine("[" + DateTime.Now.ToString() + "] Началась работа метода записи картинок в БД.");
            }
            Sem.Release();
            Models.MyDBEntities1 db = new Models.MyDBEntities1();
            List<Models.TablePicture> table = (from t in db.TablePicture select t).ToList();

            int count = db.TablePicture.Count();
            for (int i = 0; i < count; i++)
            {
                Models.TablePicture tableItem = (from t in db.TablePicture select t).First();
                db.TablePicture.Remove(tableItem);
                db.SaveChanges();
            }
            Sem.WaitOne();
            using (StreamWriter sw = new StreamWriter(pathLog, true, System.Text.Encoding.Default))
            {
                sw.WriteLine("[" + DateTime.Now.ToString() + "] Очистка картинок в БД выполнена.");
            }
            Sem.Release();

            int id = 0;
            foreach (PostPictures item in posts)
            {
                List<string> pictures = item.PictureURLs.ToList();
                foreach (string p in pictures)
                {
                    Models.TablePicture t1 = new Models.TablePicture()
                    {
                        Picture = p,
                        Post_Id = item.ID,
                        Id = id
                    };
                    db.TablePicture.Add(t1);
                    id++;
                    db.SaveChanges();
                }
            }
            db.Dispose();
            Sem.WaitOne();
            using (StreamWriter sw = new StreamWriter(pathLog, true, System.Text.Encoding.Default))
            {
                sw.WriteLine("[" + DateTime.Now.ToString() + "] Метод закончил загружать картинки в БД.");
            }
            Sem.Release();
        }
        private void PostPictureDeserialization(string fileWay)
        {
            if (!File.Exists(fileWay)) return;
            Sem.WaitOne();
            using (StreamWriter sw = new StreamWriter(pathLog, true, System.Text.Encoding.Default))
            {
                sw.WriteLine("[" + DateTime.Now.ToString() + "] Началась работа метода считывающего картинки из файла.");
            }
            Sem.Release();
            DataContractJsonSerializer jsonFormatter = new DataContractJsonSerializer(typeof(PostPictures[]));
            using (FileStream stream = new FileStream(fileWay, FileMode.Open))
            {
                PostPictures[] p = (PostPictures[])jsonFormatter.ReadObject(stream);
                TmpPicture = p.ToList<PostPictures>();
            }
            Sem.WaitOne();
            using (StreamWriter sw = new StreamWriter(pathLog, true, System.Text.Encoding.Default))
            {
                sw.WriteLine("[" + DateTime.Now.ToString() + "] Метод считал картинки из файла.");
            }
            Sem.Release();
            AddPicture();
        }
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void AddUrl()
        {
            if (!TmpURL.Any()) return;
            List<PostURLs> posts = TmpURL;
            Sem.WaitOne();
            using (StreamWriter sw = new StreamWriter(pathLog, true, System.Text.Encoding.Default))
            {
                sw.WriteLine("[" + DateTime.Now.ToString() + "] Началась работа метода записи ссылок в БД.");
            }
            Sem.Release();
            Models.MyDBEntities1 db = new Models.MyDBEntities1();
            List<Models.TableUrl> table = (from t in db.TableUrl select t).ToList();

            int count = db.TableUrl.Count();
            for (int i = 0; i < count; i++)
            {
                Models.TableUrl tableItem = (from t in db.TableUrl select t).First();
                db.TableUrl.Remove(tableItem);
                db.SaveChanges();
            }
            Sem.WaitOne();
            using (StreamWriter sw = new StreamWriter(pathLog, true, System.Text.Encoding.Default))
            {
                sw.WriteLine("[" + DateTime.Now.ToString() + "] Очистка ссылок в БД выполнена.");
            }
            Sem.Release();

            int id = 0;
            foreach (PostURLs item in posts)
            {
                List<string> urls = item.TextURLs.ToList();
                foreach (string u in urls)
                {
                    if (!u.Equals(""))
                    {
                        Models.TableUrl t1 = new Models.TableUrl()
                        {
                            Url = u,
                            Post_Id = item.ID,
                            Id = id
                        };
                        db.TableUrl.Add(t1);
                        id++;
                        db.SaveChanges();
                    }
                }
            }
            db.Dispose();
            Sem.WaitOne();
            using (StreamWriter sw = new StreamWriter(pathLog, true, System.Text.Encoding.Default))
            {
                sw.WriteLine("[" + DateTime.Now.ToString() + "] Метод закончил загружать ссылки в БД.");
            }
            Sem.Release();
        }
        private void PostURLDeserialization(string fileWay)
        {
            if (!File.Exists(fileWay)) return;
            Sem.WaitOne();
            using (StreamWriter sw = new StreamWriter(pathLog, true, System.Text.Encoding.Default))
            {
                sw.WriteLine("[" + DateTime.Now.ToString() + "] Началась работа метода считывающего ссылки из файла.");
            }
            Sem.Release();
            DataContractJsonSerializer jsonFormatter = new DataContractJsonSerializer(typeof(PostURLs[]));
            using (FileStream stream = new FileStream(fileWay, FileMode.Open))
            {
                PostURLs[] p = (PostURLs[])jsonFormatter.ReadObject(stream);
                TmpURL = p.ToList<PostURLs>();
            }
            Sem.WaitOne();
            using (StreamWriter sw = new StreamWriter(pathLog, true, System.Text.Encoding.Default))
            {
                sw.WriteLine("[" + DateTime.Now.ToString() + "] Метод считал ссылки из файла.");
            }
            Sem.Release();
            AddUrl();
        }
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void AddText()
        {
            if (!TmpText.Any()) return;
            List<PostText> posts = TmpText;
            Sem.WaitOne();
            using (StreamWriter sw = new StreamWriter(pathLog, true, System.Text.Encoding.Default))
            {
                sw.WriteLine("[" + DateTime.Now.ToString() + "] Началась работа метода записи текста в БД.");
            }
            Sem.Release();
            Models.MyDBEntities1 db = new Models.MyDBEntities1();
            List<Models.TableText> table = (from t in db.TableText select t).ToList();

            int count = db.TableText.Count();
            for (int i = 0; i < count; i++)
            {
                Models.TableText tableItem = (from t in db.TableText select t).First();
                db.TableText.Remove(tableItem);
                db.SaveChanges();
            }
            Sem.WaitOne();
            using (StreamWriter sw = new StreamWriter(pathLog, true, System.Text.Encoding.Default))
            {
                sw.WriteLine("[" + DateTime.Now.ToString() + "] Очистка текста в БД выполнена.");
            }
            Sem.Release();

            int id = 0;
            foreach (PostText item in posts)
            {
                Models.TableText t1 = new Models.TableText()
                {
                    Text = item.Text,
                    Post_Id = item.ID,
                    Id = id
                };
                db.TableText.Add(t1);
                db.SaveChanges();
                id++;
            }
            db.Dispose();
            Sem.WaitOne();
            using (StreamWriter sw = new StreamWriter(pathLog, true, System.Text.Encoding.Default))
            {
                sw.WriteLine("[" + DateTime.Now.ToString() + "] Метод закончил загружать текст в БД.");
            }
            Sem.Release();
        }
        private void PostTextDeserialization(string fileWay)
        {
            if (!File.Exists(fileWay)) return;
            TmpText.Clear();
            Sem.WaitOne();
            using (StreamWriter sw = new StreamWriter(pathLog, true, System.Text.Encoding.Default))
            {
                sw.WriteLine("[" + DateTime.Now.ToString() + "] Началась работа метода считывающего текст из файла.");
            }
            Sem.Release();
            DataContractJsonSerializer jsonFormatter = new DataContractJsonSerializer(typeof(PostText[]));
            using (FileStream stream = new FileStream(fileWay, FileMode.Open))
            {
                PostText[] p = (PostText[])jsonFormatter.ReadObject(stream);
                TmpText = p.ToList<PostText>();
            }
            Sem.WaitOne();
            using (StreamWriter sw = new StreamWriter(pathLog, true, System.Text.Encoding.Default))
            {
                sw.WriteLine("[" + DateTime.Now.ToString() + "] Метод считал текст из файла.");
            }
            Sem.Release();
            AddText();
        }
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void Timer1_Tick(object sender, EventArgs e)
        {
            //using (StreamWriter sw = new StreamWriter(pathLog, true, System.Text.Encoding.Default))
            //{
            //    sw.WriteLine("[" + DateTime.Now.ToString() + "] Итерация проверки на файл-активатор.");
            //}
            if (File.Exists(serviceFile))
            {
                using (StreamWriter sw = new StreamWriter(pathLog, true, System.Text.Encoding.Default))
                {
                    sw.WriteLine("[" + DateTime.Now.ToString() + "] Демон получил сигнал на работу.");
                }
                EventScheduler();
                timer2.Elapsed += Timer2_Tick;
                timer2.Interval = 1000;
                timer2.Enabled = true;
                timer1.Stop();
                timer2.Start();
            }
        }

        private void Timer2_Tick(object sender, ElapsedEventArgs e)
        {
            if (!threadOne.IsAlive && !threadTwo.IsAlive && !threadThree.IsAlive)
            {
                using (StreamWriter sw = new StreamWriter(pathLog, true, System.Text.Encoding.Default))
                {
                    sw.WriteLine("[" + DateTime.Now.ToString() + "] Демон увидел, что потоки закончили работу.");
                }
                File.Delete(serviceFile);
                using (FileStream fs = new FileStream(programmFile, FileMode.Create))
                {
                    fs.Close();
                    fs.Dispose();
                }
                timer2.Stop();
                timer1.Start();
                using (StreamWriter sw = new StreamWriter(pathLog, true, System.Text.Encoding.Default))
                {
                    sw.WriteLine("[" + DateTime.Now.ToString() + "] Демон вышел в режим ожидания файла-активатора .");
                }
            }
        }

        protected override void OnStop()
        {
            if (File.Exists(serviceFile)) File.Delete(serviceFile);
            using (StreamWriter sw = new StreamWriter(pathLog, true, System.Text.Encoding.Default))
            {
                Models.MyDBEntities1 db = new Models.MyDBEntities1();

                sw.WriteLine("[" + DateTime.Now.ToString() + "] Окончание работы службы.");
                sw.WriteLine("[" + DateTime.Now.ToString() + "] Текстов в БД - " + db.TableText.Count().ToString());
                sw.WriteLine("[" + DateTime.Now.ToString() + "] Ссылок в БД - " + db.TableUrl.Count().ToString());
                sw.WriteLine("[" + DateTime.Now.ToString() + "] Картинок в БД - " + db.TablePicture.Count().ToString());
                db.Dispose();
            }
            if (File.Exists(serviceLive)) { File.Delete(serviceLive); }

        }
    }
}
