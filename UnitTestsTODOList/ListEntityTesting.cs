using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TODOList.Domain.Entity;
using TODOList.Domain.Entity.Identity;

namespace UnitTestsTODOList {
    [TestClass]
    public class ListEntityTesting {
        [TestMethod]
        public void TaskEntityTest() {
            // Arrange
            TodoTask todoTask = new TodoTask {
                TaskID = 1,
                TaskName = "Task Testing",
                Description = "Testing this task",
                Colour = "red",
                ListID = 1
            };

            TodoTask todoTaskLoadFrom = new TodoTask {
                TaskID = 2,
                TaskName = "Task Loading Testing",
                Description = "Testing this method",
                Colour = "chrysochlorous",
                ListID = 1
            };

            TodoTask todoTaskLoadTo = new TodoTask();

            // Act
            todoTaskLoadTo.LoadFrom(todoTaskLoadFrom);
            todoTaskLoadFrom.LoadFrom(new TodoTask());

            // Assert
            Assert.IsNotNull(todoTask);
            Assert.AreEqual(todoTask.TaskID, 1);
            Assert.AreEqual(todoTask.ListID, 1);
            Assert.AreEqual(todoTask.TaskName, "Task Testing");
            Assert.AreEqual(todoTask.Description, "Testing this task");
            Assert.AreEqual(todoTask.Colour, "red");
            Assert.IsNull(todoTask.ParentList);

            Assert.IsNotNull(todoTaskLoadTo);
            Assert.AreEqual(todoTaskLoadTo.TaskName, "Task Loading Testing");
            Assert.AreEqual(todoTaskLoadTo.Description, "Testing this method");
            Assert.AreEqual(todoTaskLoadTo.Colour, "chrysochlorous");
            Assert.IsNull(todoTaskLoadTo.ParentList);

            Assert.IsNull(todoTaskLoadFrom.TaskName);
            Assert.IsNull(todoTaskLoadFrom.Description);
            Assert.IsNull(todoTaskLoadFrom.Colour);
            Assert.IsNull(todoTaskLoadFrom.ParentList);
        }

        [TestMethod]
        public void ListEntityTest() {
            //Arange 
            TodoTask todoTask = new TodoTask {
                TaskID = 1,
                TaskName = "Task Test",
                Description = "Testing another task",
                Colour = "glaucous",
                ListID = 1
            };

            TodoList todoList = new TodoList {
                ListID = 1,
                ListName = "List Test",
                DateCreated = new DateTime(2018, 8, 12, 6, 14, 16),
                LeftPositoned = true,
                TodoTasks = new List<TodoTask>(),
                UserID = 1, 
                User = null
            };

            TodoList todoListLoadFrom = new TodoList {
                ListID = 1,
                ListName = "List Load Test",
                DateCreated = new DateTime(2018, 8, 12, 7, 14, 16),
                LeftPositoned = false,
                TodoTasks = new List<TodoTask>(),
                UserID = 1,
                User = null
            };

            TodoList todoListLoadTo = new TodoList();
            
            // Act
            todoList.TodoTasks.Add(todoTask);
            todoListLoadTo.LoadFrom(todoListLoadFrom);
            todoListLoadFrom.LoadFrom(new TodoList() {
                TodoTasks = new List<TodoTask>()
            });

            IList<TodoTask> testTaskList = new List<TodoTask> {todoTask};

            DateTime testDateCreated = new DateTime(2018, 8, 12, 6, 14, 16);
            DateTime testLoadedDateCreated = new DateTime(2018, 8, 12, 7, 14, 16);

            // Assert
            Assert.IsNotNull(todoList);
            Assert.AreEqual(todoList.ListName, "List Test");
            Assert.AreEqual(todoList.DateCreated, testDateCreated);
            Assert.AreEqual(todoList.LeftPositoned, true);
            CollectionAssert.AreEquivalent(todoList.TodoTasks, testTaskList);
            Assert.AreEqual(todoList.UserID, 1);
            Assert.IsNull(todoList.User, null);

            Assert.IsNotNull(todoListLoadTo);
            Assert.AreEqual(todoListLoadTo.ListName, "List Load Test");
            Assert.AreEqual(todoListLoadTo.DateCreated, testLoadedDateCreated);
            Assert.AreEqual(todoListLoadTo.LeftPositoned, false);
            Assert.IsTrue(todoListLoadTo.TodoTasks.Count == 0);
            Assert.AreEqual(todoListLoadTo.UserID, 1);
            Assert.IsNull(todoListLoadTo.User, null);

            Assert.IsNull(todoListLoadFrom.ListName);
            Assert.IsTrue(todoListLoadFrom.TodoTasks.Count == 0);
            Assert.IsFalse(todoListLoadFrom.LeftPositoned);
        }
    }
}
