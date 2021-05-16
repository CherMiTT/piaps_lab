using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ResRegV1cons
{
    class ExpirationQueue
    {
        public static List<Tuple<int, UserRequest>> expirationQueue; //Первый элемент - сколько времени запросу ещё осталось выполняться
        public static System.Timers.Timer expirationTimer;
        public static Stopwatch stopwatch;
    

    }
}
