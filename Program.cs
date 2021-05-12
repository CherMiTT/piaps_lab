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
                    Resource r = new Resource(RESOURCE_STATE.FREE, breakProbability, repairTimeSec, 0, queue);
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
                    while ((line = reader.ReadLine()) != null)
                    {
                        string[] parts = line.Split(new[] { ' ' });
                        RESOURCE_STATE state = (RESOURCE_STATE)Enum.Parse(typeof(RESOURCE_STATE), parts[0], true);
                        double breakProbability = Convert.ToDouble(parts[1]);
                        int repairTime = Convert.ToInt32(parts[2]);
                        int curTimeSec = Convert.ToInt32(parts[3]);
                        List<UserRequest> queue = new List<UserRequest>();
                        int c = Convert.ToInt32(parts[4]);
                        for(int j = 0; j < c; j++)
                        {
                            int resourceId = Convert.ToInt32(parts[5 + 2 * j]);
                            int maxWaitTime = Convert.ToInt32(parts[6 + 2 * j]);
                            UserRequest request = new UserRequest(resourceId, maxWaitTime);
                            queue.Add(request);
                        }
                        Resource r = new Resource(state, breakProbability, repairTime, curTimeSec, queue);
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
        public static List<Resource> vRes_s; //Модель набора ресурсов

        public static void Occupy(string cn)
        {
            if ((Convert.ToInt16(cn) > vRes_s.Count) | (Convert.ToInt16(cn) < 0)) throw new ResIdInvalid();
            int resourceId = Convert.ToInt16(cn);

            UserRequest request = new UserRequest(resourceId, 60); //TODO: это надо вводить
            vRes_s[resourceId - 1].vReqQueue.Add(request);

            if (vRes_s[resourceId - 1].state == RESOURCE_STATE.FREE)
            {
                vRes_s[resourceId - 1].state = RESOURCE_STATE.BUSY;
                Console.WriteLine("Ресурс занят.");
            }
            else
            {
                Console.WriteLine("Запрос на ресурс добавлен в очередь.");
            }
        }

        public static void Free(string cn)
        {
            if ((Convert.ToInt16(cn) > vRes_s.Count) | (Convert.ToInt16(cn) < 0)) throw new ResIdInvalid();
            int resourceId = Convert.ToInt16(cn);

            if (vRes_s[resourceId - 1].state == RESOURCE_STATE.FREE) throw new ResWasFree();
            if(vRes_s[resourceId - 1].state == RESOURCE_STATE.BROKEN)
            {
                if (vRes_s[resourceId - 1].vReqQueue.Count == 0) throw new ResWasFree();

                vRes_s[resourceId - 1].vReqQueue.RemoveAt(0);
            }

            if(vRes_s[resourceId - 1].state == RESOURCE_STATE.BUSY)
            {
                vRes_s[resourceId - 1].state = RESOURCE_STATE.FREE;
                Console.WriteLine("Ресурс освобождён");
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
                for (int i = 0; i < vRes_s.Count; i++) //Цикл по всем ресурсам
                {
                    writetext.WriteLine(vRes_s[i].ToString());
                }
            }
        }
    }

    enum RESOURCE_STATE
    {
        FREE = 1,
        BUSY = 2,
        BROKEN = 3
    }


    class Resource
    {
        public RESOURCE_STATE state; //Состояние ресурса
        public double breakProbability {get;} //Вероятность поломки ресурса [0; 1]
        public int repairTimeSec { get; } //Время, нужное починки ресурса в секундах
        public int curTimeSec { get; } //Внутренний счётчик времени
        public List<UserRequest> vReqQueue; //Очередь запросов к этому ресурсу
        public System.Timers.Timer timer;
        public Stopwatch stopwatch; //Нужно для корректной паузы таймера

        public Resource(RESOURCE_STATE curState, double breakProbability, int repairTimeSec, int curTimeSec, List<UserRequest> vReqQueue)
        {
            state = curState;
            this.breakProbability = breakProbability;
            this.repairTimeSec = repairTimeSec;
            this.curTimeSec = curTimeSec;
            this.vReqQueue = vReqQueue;

            timer = new System.Timers.Timer();
            //TODO: проверить логику
            if (state == RESOURCE_STATE.FREE || state == RESOURCE_STATE.BUSY) //Если ресурс не сломан
            {
                timer.Interval = 1000; //Запустить ежесекундный таймер (для проверки, сломается ли ресурс)
                timer.Elapsed += OnTickEvent;
            } 
            else //Если ресурс сломан
            {
                timer.Interval = (repairTimeSec - curTimeSec) * 1000;
                timer.Elapsed += OnRepaired;
            }
            timer.AutoReset = true;
            timer.Enabled = true; //Запускаем таймер

            stopwatch = new Stopwatch();
        }
        
        // Метод, вызываемый каждую секунду. Проверяет, сломается ли ресурс.
        private void OnTickEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            double randomNumber = Program.rnd.NextDouble();
            if(randomNumber <= breakProbability)
            {
                timer.Enabled = false;
                Console.WriteLine("Ресурс сломался, время починки = " + repairTimeSec + " секунд");
                if(state == RESOURCE_STATE.BUSY)
                {
                    Console.WriteLine("Запрос перемещён в очередь.");
                }
                state = RESOURCE_STATE.BROKEN;

                timer.Elapsed -= OnTickEvent;
                timer.Elapsed += OnRepaired;
                stopwatch.Reset();
                stopwatch.Start();
                timer.Enabled = true;
                timer.Interval = repairTimeSec * 1000;
            }
        }

        // Метод, вызываемый, когда сломанный ресурс починен.
        private void OnRepaired(Object source, System.Timers.ElapsedEventArgs e)
        {
            stopwatch.Stop();
            stopwatch.Reset();

            timer.Enabled = false;
            Console.WriteLine("Ресурс починен");
            state = RESOURCE_STATE.FREE;

            if(vReqQueue.Count != 0)
            {
                Console.WriteLine("Запрос занял ресурс");
                state = RESOURCE_STATE.BUSY;
            }

            timer.Elapsed -= OnRepaired;
            timer.Elapsed += OnTickEvent;
            timer.Enabled = true;
            timer.Interval = 1000;
        }

        override public string ToString()
        {
            string str = state.ToString();
            str += " " + breakProbability;
            str += " " + repairTimeSec;
            str += " " + curTimeSec;
            str += " " + vReqQueue.Count;
            for (int j = 0; j < vReqQueue.Count; j++) //Все запросы в очереди в виде пар "id maxWaitSec"
            {
                str += " " + vReqQueue[j].resourceId + " " + vReqQueue[j].maxWaitTimeSec;
            }
            return str;
        }
    }

    class UserRequest
    {
        public int resourceId { get; }
        public int maxWaitTimeSec { get; }

        public UserRequest(int resourceId, int maxWaitTimeSec)
        {
            this.resourceId = resourceId;
            this.maxWaitTimeSec = maxWaitTimeSec;
        }
    }

    class Program
    {
        public static Random rnd;
        public static bool isPaused;

        static void Main(string[] args)
        {
            rnd = new Random();
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
                    }

                    if (Command == "FREE")
                    {
                        pauseAll();
                        Console.WriteLine("Введите номер ресурса:");
                        Model.Free(Console.ReadLine());
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

        private static void pauseAll()
        {
            Console.WriteLine("Paused");
            Program.isPaused = true;
            for(int i = 0; i < Model.vRes_s.Count; i++)
            {
                Model.vRes_s[i].stopwatch.Stop();
                Model.vRes_s[i].timer.Enabled = false;
                Console.WriteLine("Ресурс " + i + " остановлен, stopwatch = " + Model.vRes_s[i].stopwatch.Elapsed);
            }
        }

        private static void resumeAll()
        {
            for (int i = 0; i < Model.vRes_s.Count; i++)
            {
                Model.vRes_s[i].timer.Enabled = true;
                Model.vRes_s[i].timer.Interval = Model.vRes_s[i].repairTimeSec - Model.vRes_s[i].stopwatch.Elapsed.Seconds;
                Console.WriteLine("Ресурс " + i + " возобновлён, время ожидания = " + (Model.vRes_s[i].repairTimeSec - Model.vRes_s[i].stopwatch.Elapsed.Seconds));
            }
            Program.isPaused = false;
            Console.WriteLine("Resumed");
        }
    }
}
