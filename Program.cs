using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.IO;

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
                Random rnd = new Random();
                for(int i = 0; i < resourceCount; i++)
                {
                    double breakProbability = rnd.NextDouble(); //TODO: могут быть очень большие вероятности, уменьшить
                    int repairTimeSec = rnd.Next(1, 30);
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
        private static void GetModel() //TODO: переделать
        {
            Console.WriteLine("Обновить файл?");
            if (Console.ReadLine().ToUpper() == "Y") CreateModel();
            else
            {
                //Model.vRes_s = File.ReadAllLines(Path);
                //TOOD: считывать из файла
            }
        }
        public static bool On()
        {
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
        public static List<Resource> vRes_s;//Модель набора ресурсов
        public static void Occupy(string cn)
        {
            if ((Convert.ToInt16(cn) > vRes_s.Length) | (Convert.ToInt16(cn) < 0)) throw new ResIdInvalid();
            if (vRes_s[Convert.ToInt16(cn) - 1] == "B") throw new ResIsBusy();
            vRes_s[Convert.ToInt16(cn) - 1] = "B";
        }

        public static void Free(string cn)
        {
            if ((Convert.ToInt16(cn) > vRes_s.Length) | (Convert.ToInt16(cn) < 0)) throw new ResIdInvalid();
            if (vRes_s[Convert.ToInt16(cn) - 1] == "F") throw new ResWasFree();
            vRes_s[Convert.ToInt16(cn) - 1] = "F";
        }

        public static string Request()
        {
            for (int i = 0; i < vRes_s.Length; i++)
            {
                if (vRes_s[i] == "F") return Convert.ToString(i + 1);
            }
            throw new ResAreBusy();
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
        private double breakProbability; //Вероятность поломки ресурса [0; 1]
        private int repairTimeSec; //Время, нужное починки ресурса в секундах
        private int curTimeSec; //Внутренний счётчик времени
        public List<UserRequest> vReqQueue; //Очередь запросов к этому ресурсу
        private Timer timer;

        public Resource(RESOURCE_STATE curState, double breakProbability, int repairTimeSec, int curTimeSec, List<UserRequest> vReqQueue)
        {
            state = curState;
            this.breakProbability = breakProbability;
            this.repairTimeSec = repairTimeSec;
            this.curTimeSec = curTimeSec;
            this.vReqQueue = vReqQueue;

            timer = new Timer();
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
        }
        
        // Метод, вызываемый каждую секунду. Проверяет, сломается ли ресурс.
        private void OnTickEvent(Object source, System.Timers.ElapsedEventArgs e)
        {

        }

        // Метод, вызываемый, когда сломанный ресурс починен.
        private void OnRepaired(Object source, System.Timers.ElapsedEventArgs e)
        {

        }
    }

    class UserRequest
    {
        public int resourceId;
        public int maxWaitTimeSec;
    }

    class Program
    {
        //private static Timer aTimer;

        static void Main(string[] args)
        {
            string Command;
            while (!SetUp.On()) ;


            //aTimer = new System.Timers.Timer();
            //aTimer.Interval = 2000;
            // Hook up the Elapsed event for the timer. 
            //aTimer.Elapsed += OnTimedEvent;
            // Have the timer fire repeated events (true is the default)
            //aTimer.AutoReset = true;
            // Start the timer
            //aTimer.Enabled = true;

            do
            {
                File.WriteAllLines(SetUp.Path, Model.vRes_s);//сохранение модели
                Console.WriteLine("Введите команду:");
                Command = Console.ReadLine();
                Command = Command.ToUpper();
                try
                {
                    if (Command == "REQUEST") Console.WriteLine(Model.Request());
                    if (Command == "OCCUPY")
                    {
                        Console.WriteLine("Введите номер ресурса:");
                        Model.Occupy(Console.ReadLine());
                        Console.WriteLine("Ресурс стал занятым.");
                    };
                    if (Command == "FREE")
                    {
                        Console.WriteLine("Введите номер ресурса:");
                        Model.Free(Console.ReadLine());
                        Console.WriteLine("Ресурс освобождён.");
                    };
                }
                catch (OverflowException) { Console.WriteLine("Такого ресурса нет."); }
                catch (FormatException) { Console.WriteLine("Такого ресурса нет."); }
                catch (ResIdInvalid) { Console.WriteLine("Такого ресурса нет."); }
                catch (ResWasFree) { Console.WriteLine("Ресурс был свободен."); }
                catch (ResAreBusy) { Console.WriteLine("Все ресурсы заняты."); }
                catch (ResIsBusy) { Console.WriteLine("ресурс уже занят."); }
            }
            while (Command != "");
        }

        /*private static void OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            Console.WriteLine("\nThe Elapsed event was raised at {0}; found not occupied: {1}", e.SignalTime, Model.Request()); ;
        }*/

    }
}
