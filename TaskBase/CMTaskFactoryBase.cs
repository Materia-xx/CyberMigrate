using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskBase
{
    public abstract class CMTaskFactoryBase
    {
        public List<string> SupportedTasks { get; set; } = new List<string>();

        public abstract CMTaskBase CreateTask(int cmSystemId, int cmFeatureId, int cmTaskId);
    }
}
