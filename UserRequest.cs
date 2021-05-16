using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResRegV1cons
{
    class UserRequest
    {
        public int resourceId { get; }
        public int maxWaitTimeSec { get; }
        public int id { get; }

        public UserRequest(int resourceId, int maxWaitTimeSec)
        {
            this.resourceId = resourceId;
            this.maxWaitTimeSec = maxWaitTimeSec;
            this.id = ++Model.curId;
        }
    }
}
