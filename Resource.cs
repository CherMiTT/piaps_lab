using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ResRegV1cons
{
    enum RESOURCE_STATE
    {
        FREE = 1,
        BUSY = 2,
        BROKEN = 3
    }

    class Resource
    {
        public RESOURCE_STATE state; //Состояние ресурса
        public double breakProbability { get; } //Вероятность поломки ресурса [0; 1]
        public int repairTimeSec { get; } //Время, нужное починки ресурса в секундах
        public List<UserRequest> vReqQueue; //Очередь запросов к этому ресурсу
        public System.Timers.Timer timer;
        public Stopwatch stopwatch; //Нужно для корректной паузы таймера

        public Resource(RESOURCE_STATE curState, double breakProbability, int repairTimeSec, List<UserRequest> vReqQueue)
        {
            state = curState;
            this.breakProbability = breakProbability;
            this.repairTimeSec = repairTimeSec;
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
                timer.Interval = repairTimeSec * 1000;
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
            if (randomNumber <= breakProbability)
            {
                timer.Enabled = false;
                Console.WriteLine("Ресурс " + (Model.vRes_s.IndexOf(this) + 1) + " сломался, время починки = " + repairTimeSec + " секунд");
                if (state == RESOURCE_STATE.BUSY)
                {
                    Program.expirationQueue.Add(new Tuple<int, UserRequest>(vReqQueue[0].maxWaitTimeSec, vReqQueue[0]));
                    Console.WriteLine("Запрос перемещён в очередь.");
                    Program.resortExpirationQueue(true);
                    Program.stopwatch.Start();
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
            Console.WriteLine("Ресурс " + (Model.vRes_s.IndexOf(this) + 1) + " починен");
            state = RESOURCE_STATE.FREE;

            if (vReqQueue.Count != 0)
            {
                Console.WriteLine("Запрос занял ресурс " + (Model.vRes_s.IndexOf(this) + 1));
                state = RESOURCE_STATE.BUSY;
                Program.deleteRequestFromExpirationQueue(vReqQueue[0]);
                Program.resortExpirationQueue(false);
                Program.stopwatch.Start();
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
            str += " " + vReqQueue.Count;
            for (int j = 0; j < vReqQueue.Count; j++) //Все запросы в очереди в виде пар "id maxWaitSec"
            {
                str += " " + vReqQueue[j].id + " " + vReqQueue[j].resourceId + " " + vReqQueue[j].maxWaitTimeSec;
            }
            return str;
        }
    }
}
