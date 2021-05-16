using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;
using System.IO;
using System.Diagnostics;

namespace ResRegV1cons
{
    class ResAreBusy : Exception { }
    class ResIdInvalid : Exception { }
    class UnRecommended : Exception { }
    class ResIsBusy : Exception { }
    class ResWasFree : Exception { }

    static class SetUp
    {
        public static string Path; //путь к файлу, сохраняющему модель

        //Создаёт новую пустую модель
        private static void CreateModel()
        {
            Console.WriteLine("Укажите количество ресурсов:");
            try
            {
                int resourceCount = Convert.ToInt32(Console.ReadLine());
                for(int i = 0; i < resourceCount; i++)
                {
                    double breakProbability = Program.rnd.NextDouble() / 100; //TODO: могут быть очень большие вероятности, уменьшить
                    int repairTimeSec = Program.rnd.Next(1, 30);
                    List<UserRequest> queue = new List<UserRequest>();
                    Resource r = new Resource(RESOURCE_STATE.FREE, breakProbability, repairTimeSec, queue);
                    Model.vRes_s.Add(r);
                }
            }
            catch
            {
                Console.WriteLine("Введено некорректное число!");
                CreateModel();
            }
        }

        //Считывает модель из файла
        private static void GetModel()
        {
            Console.WriteLine("Обновить файл?");
            if (Console.ReadLine().ToUpper() == "Y") CreateModel();
            else
            {
                using (var reader = new StreamReader(SetUp.Path))
                {
                    string line;
                    line = reader.ReadLine();
                    string[] p = line.Split(new[] { ' ' }); //ExpirationQueue
                    Console.WriteLine("Очередь запросов:");
                    for(int i = 0; i < Convert.ToInt32(p[0]); i++)
                    {
                        int time = Convert.ToInt32(p[1 + 4 * i]);
                        int id = Convert.ToInt32(p[2 + 4 * i]);
                        int resourceId = Convert.ToInt32(p[3 + 4 * i]);
                        int maxWaitTime = Convert.ToInt32(p[4 + 4 * i]);
                        UserRequest request = new UserRequest(resourceId, maxWaitTime);
                        Program.expirationQueue.Add(new Tuple<int, UserRequest>(time, request));
                        Console.WriteLine("Запрос id = " + id + "; resourceId = " + resourceId + "; maxWaitTime = " + maxWaitTime + "; remainingTime = " + time);
                    }

                    while ((line = reader.ReadLine()) != null) //Ресурсы
                    {
                        string[] parts = line.Split(new[] { ' ' });
                        RESOURCE_STATE state = (RESOURCE_STATE)Enum.Parse(typeof(RESOURCE_STATE), parts[0], true);
                        double breakProbability = Convert.ToDouble(parts[1]);
                        int repairTime = Convert.ToInt32(parts[2]);
                        List<UserRequest> queue = new List<UserRequest>();
                        int c = Convert.ToInt32(parts[3]);
                        for(int j = 0; j < c; j++)
                        {
                            int id = Convert.ToInt32(parts[4 + 3 * j]);
                            int resourceId = Convert.ToInt32(parts[5 + 3 * j]);
                            int maxWaitTime = Convert.ToInt32(parts[6 + 3 * j]);
                            UserRequest request = new UserRequest(resourceId, maxWaitTime);
                            queue.Add(request);
                        }
                        Resource r = new Resource(state, breakProbability, repairTime, queue);
                        Model.vRes_s.Add(r);
                    }
                }

            }
        }
        public static bool On()
        {
            Model.vRes_s = new List<Resource>();
            try
            {
                if (File.Exists(Directory.GetCurrentDirectory() + @"\Resmod00"))
                {
                    Console.WriteLine("Использовать существующий стандартный файл Resmod00?");
                    if (Console.ReadLine().ToUpper() == "Y")
                    {
                        Path = Directory.GetCurrentDirectory() + @"\Resmod00";
                        GetModel();
                        return true;
                    }
                }
                else
                {
                    Console.WriteLine("Создать стандартный файл?");
                    if (Console.ReadLine().ToUpper() == "Y")
                    {
                        Path = Directory.GetCurrentDirectory() + @"\Resmod00";
                        CreateModel();
                        return true;
                    }
                };
                Console.WriteLine("Введите полный адрес нестандартного файла:");
                Path = Console.ReadLine();
                if (File.Exists(Path))
                {
                    GetModel();
                    return true;
                }
                else
                {
                    CreateModel();
                    return true;
                }
            }
            catch (IOException) { Console.WriteLine("Файл не открылся."); return false; }
            catch (Exception) { Console.WriteLine("Ошибка ввода-вывода."); return false; }
        }
    }
    static class Model
    {
        public static int curId;
        public static List<Resource> vRes_s; //Модель набора ресурсов

        public static void Occupy(string cn)
        {
            if ((Convert.ToInt16(cn) > vRes_s.Count) | (Convert.ToInt16(cn) <= 0)) throw new ResIdInvalid();
            int resourceId = Convert.ToInt16(cn);

            UserRequest request = new UserRequest(resourceId, 30); //TODO: это надо вводить
            vRes_s[resourceId - 1].vReqQueue.Add(request);

            if (vRes_s[resourceId - 1].state == RESOURCE_STATE.FREE)
            {
                vRes_s[resourceId - 1].state = RESOURCE_STATE.BUSY;
                Console.WriteLine("Ресурс занят.");
            }
            else
            {
                Program.expirationQueue.Add(new Tuple<int, UserRequest>(request.maxWaitTimeSec, request));
                Console.WriteLine("Запрос на ресурс добавлен в очередь.");
            }
        }

        public static void Free(string cn)
        {
            if ((Convert.ToInt16(cn) > vRes_s.Count) | (Convert.ToInt16(cn) <= 0)) throw new ResIdInvalid();
            int resourceId = Convert.ToInt16(cn);

            if (vRes_s[resourceId - 1].state == RESOURCE_STATE.FREE) throw new ResWasFree();
            if(vRes_s[resourceId - 1].state == RESOURCE_STATE.BROKEN)
            {
                if (vRes_s[resourceId - 1].vReqQueue.Count == 0) throw new ResWasFree();

                Program.deleteRequestFromExpirationQueue(vRes_s[resourceId - 1].vReqQueue[0]);
                vRes_s[resourceId - 1].vReqQueue.RemoveAt(0);
            }

            if(vRes_s[resourceId - 1].state == RESOURCE_STATE.BUSY)
            {
                vRes_s[resourceId - 1].state = RESOURCE_STATE.FREE;
                Console.WriteLine("Ресурс освобождён");

                Program.deleteRequestFromExpirationQueue(vRes_s[resourceId - 1].vReqQueue[0]);
                vRes_s[resourceId - 1].vReqQueue.RemoveAt(0);

                if (vRes_s[resourceId - 1].vReqQueue.Count != 0)
                {
                    Console.WriteLine("Запрос из очереди занял ресурс");
                    vRes_s[resourceId - 1].state = RESOURCE_STATE.BUSY;
                }
            }
        }

        public static string Request()
        {
            for (int i = 0; i < vRes_s.Count; i++)
            {
                if (vRes_s[i].state == RESOURCE_STATE.FREE) return Convert.ToString(i + 1);
            }
            throw new ResAreBusy();
        }

        //Пишет модель в файл
        public static void WriteToFile()
        {
            using (StreamWriter writetext = new StreamWriter(SetUp.Path))
            {
                string str = "";
                str += Program.expirationQueue.Count;
                for (int j = 0; j < Program.expirationQueue.Count; j++) //Все запросы в очереди в виде пар "id maxWaitSec"
                {
                    str += " " + Program.expirationQueue[j].Item1.ToString() + " " + Program.expirationQueue[j].Item2.id + " " + Program.expirationQueue[j].Item2.resourceId + " " + Program.expirationQueue[j].Item2.maxWaitTimeSec;
                }
                writetext.WriteLine(str);

                for (int i = 0; i < vRes_s.Count; i++) //Цикл по всем ресурсам
                {
                    writetext.WriteLine(vRes_s[i].ToString());
                }
            }
        }
    }

    class Program
    {
        public static Random rnd;
        public static bool isPaused;

        static void Main(string[] args)
        {
            rnd = new Random();
            expirationQueue = new List<Tuple<int, UserRequest>>();
            expirationTimer = new System.Timers.Timer();
            expirationTimer.AutoReset = true;
            expirationTimer.Elapsed += RequestExpired;
            stopwatch = new Stopwatch();

            string Command;
            while (!SetUp.On()) ;

            Console.WriteLine("Ресурсы в модели:");
            for(int i = 0; i < Model.vRes_s.Count; i++)
            {
                Console.WriteLine(Model.vRes_s[i].ToString());
            }

            Command = "Command";
            isPaused = false;

            do
            {
                Model.WriteToFile();
                
                if (isPaused) continue;

                Console.WriteLine("Введите команду:");
                Command = Console.ReadLine();
                Command = Command.ToUpper();
                try
                {
                    if (Command == "REQUEST")
                    {
                        pauseAll();
                        Console.WriteLine(Model.Request());
                        resumeAll();
                    }

                    if (Command == "OCCUPY")
                    {
                        pauseAll();
                        Console.WriteLine("Введите номер ресурса:");
                        Model.Occupy(Console.ReadLine());

                        resortExpirationQueue(true);
                        stopwatch.Start();
                    }

                    if (Command == "FREE")
                    {
                        pauseAll();
                        Console.WriteLine("Введите номер ресурса:");
                        Model.Free(Console.ReadLine());

                        resortExpirationQueue(false);
                        stopwatch.Start();
                    }
                }
                catch (OverflowException) { Console.WriteLine("Такого ресурса нет."); }
                catch (FormatException) { Console.WriteLine("Такого ресурса нет."); }
                catch (ResIdInvalid) { Console.WriteLine("Такого ресурса нет."); }
                catch (ResWasFree) { Console.WriteLine("Ресурс был свободен."); }
                catch (ResAreBusy) { Console.WriteLine("Все ресурсы заняты."); }
                catch (ResIsBusy) { Console.WriteLine("Ресурс уже занят."); }

                resumeAll();
            }
            while (Command != "");
        }

        public static void resortExpirationQueue(bool lastIsNew)
        {
            stopwatch.Stop();
            int elapsedSec = stopwatch.Elapsed.Seconds;
            stopwatch.Reset();
            if (expirationQueue.Count == 0) return;

            //TODO: сначала вычитать, потом добавлять новый,  потом вычитать

            for (int i = 0; i < expirationQueue.Count - 1; i++)
            {
                expirationQueue[i] = new Tuple<int, UserRequest>(expirationQueue[i].Item1 - elapsedSec, expirationQueue[i].Item2);
                if (expirationQueue[i].Item1 < 0) expirationQueue[i] = new Tuple<int, UserRequest>(0, expirationQueue[i].Item2);
            }
            if (!lastIsNew)
            {
                expirationQueue[expirationQueue.Count - 1] = new Tuple<int, UserRequest>(expirationQueue[expirationQueue.Count - 1].Item1 - elapsedSec, expirationQueue[expirationQueue.Count - 1].Item2);
                if (expirationQueue[expirationQueue.Count - 1].Item1 < 0) expirationQueue[expirationQueue.Count - 1] = new Tuple<int, UserRequest>(0, expirationQueue[expirationQueue.Count - 1].Item2);
            } //TODO: костыль

            expirationQueue.Sort((x, y) => x.Item1.CompareTo(y.Item1));
            expirationTimer.Stop();

            /*if (expirationQueue[0].Item1 == 0)
            {
                deleteRequestFromExpirationQueue(expirationQueue[0].Item2);
            }*/
            expirationTimer.Interval = expirationQueue[0].Item1 * 1000; //TODO: проверить
            expirationTimer.Start();

            /*Console.WriteLine("Expiration Queue");
            for(int i = 0; i < expirationQueue.Count; i++)
            {
                Console.WriteLine("id = " + expirationQueue[i].Item2.id + "; resourceId = " + expirationQueue[i].Item2.resourceId + "; time_remains " + expirationQueue[i].Item1);
            }*/
        }

        public static void deleteRequestFromExpirationQueue(UserRequest request)
        {
            for (int i = 0; i < Program.expirationQueue.Count; i++) //Удаляем запрос из очереди на истечение
            {
                if (Program.expirationQueue[i].Item2 == Model.vRes_s[request.resourceId - 1].vReqQueue[0])
                {
                    Program.expirationQueue.RemoveAt(i);
                    break;
                }
            }
            resortExpirationQueue(false);
            stopwatch.Start();
        }

        private static void pauseAll()
        { 
            Console.WriteLine("Paused");
            isPaused = true;
            expirationTimer.Stop();
            stopwatch.Stop();

            for(int i = 0; i < Model.vRes_s.Count; i++)
            {
                Model.vRes_s[i].stopwatch.Stop();
                Model.vRes_s[i].timer.Enabled = false;
            }
        }

        private static void resumeAll()
        {
            for (int i = 0; i < Model.vRes_s.Count; i++)
            {
                Model.vRes_s[i].timer.Enabled = true;
                Model.vRes_s[i].timer.Interval = (Model.vRes_s[i].repairTimeSec - Model.vRes_s[i].stopwatch.Elapsed.Seconds) * 1000;
            }

            if (expirationQueue.Count > 0)
            {
                int elapsedSec = stopwatch.Elapsed.Seconds;
                stopwatch.Reset();
                for (int i = 0; i < expirationQueue.Count - 1; i++)
                {
                    expirationQueue[i] = new Tuple<int, UserRequest>(expirationQueue[i].Item1 - elapsedSec, expirationQueue[i].Item2);
                    if(expirationQueue[i].Item1 < 0) expirationQueue[i] = new Tuple<int, UserRequest>(0, expirationQueue[i].Item2); //TODO: костыль
                }

                /*if(expirationQueue[0].Item1 == 0)
                {
                    deleteRequestFromExpirationQueue(expirationQueue[0].Item2);
                }*/
                expirationTimer.Interval = expirationQueue[0].Item1 * 1000; //TODO: проверить
                expirationTimer.Start();
                stopwatch.Start();
            }

            isPaused = false;
            Console.WriteLine("Resumed");
        }

        private static void RequestExpired(Object source, System.Timers.ElapsedEventArgs e)
        {
            pauseAll();
            Console.WriteLine("Запрос истёк: id = " + Program.expirationQueue[0].Item2.id + " ; ресурс, на который он был в очереди: " + Program.expirationQueue[0].Item2.resourceId);
            /*Console.WriteLine("Продлить запрос ещё на " + 30 + " секунд? Y/N");
            if (Console.ReadLine().ToUpper() == "Y")
            {
                Program.expirationQueue[0] = new Tuple<int, UserRequest>(30, Program.expirationQueue[0].Item2);
            }
            else
            {
                Program.expirationQueue.RemoveAt(0);
            }*/
            Program.resortExpirationQueue(false);
            Program.stopwatch.Start();
            resumeAll();
        }
    }
}
