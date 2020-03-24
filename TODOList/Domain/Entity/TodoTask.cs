using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TODOList.Domain.Entity.Identity;

namespace TODOList.Domain.Entity {
    public class TodoTask {
        public long? TaskID { get; set; }
        public string TaskName { get; set; }
        public string Description { get; set; }
        public string Colour { get; set; }

        // The ID of the List who owns the TodoTask
        public long? ListID { get; set; }
        public virtual TodoList ParentList { get; set; }

        /// <summary>
        /// Sets this TodoTask's properties from
        /// another TodoTask object
        /// </summary>
        /// <param name="other">The TodoTask object loaded from</param>
        public void LoadFrom(TodoTask other) {
            this.ListID = other.ListID;
            this.TaskName = other.TaskName;
            this.Description = other.Description;
            this.Colour = other.Colour;
        }
    }
}