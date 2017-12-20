﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace TaskBase
{
    public abstract class CMTaskFactoryBase
    {
        public List<string> SupportedTasks { get; set; } = new List<string>();

        public abstract CMTaskBase CreateTask(int cmSystemId, int cmFeatureId, int cmTaskId);

        /// <summary>
        /// Gets a configuration UI that can be used to configure the tasks that are supplied by this task factory.
        /// It is optional for the task factory to implement this. A blank panel will be used by default.
        /// </summary>
        /// <returns></returns>
        public virtual UserControl GetConfigurationUI()
        {
            // mcbtodo: flesh out the way that task factories will store their configuration data. It is assumed to be of a different structure for each tsk factory
            // mcbtodo: If there is a way to store it in the db, I'd like to do that.. and just feed some sort of base class type back and forth to the config ui
            // mcbtodo: This will make it easier on the task factory development in that it doesn't need to read a file and go through a deserializiation process
            return new UserControl();
        }
    }
}