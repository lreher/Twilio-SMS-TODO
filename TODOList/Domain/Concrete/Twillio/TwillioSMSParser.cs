using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNet.Identity;
using TODOList.Domain.Abstract;
using TODOList.Domain.Concrete.Owin;
using TODOList.Domain.Entity;
using TODOList.Domain.Entity.Identity;

namespace TODOList.Domain.Concrete.Twillio {
    public static class SMSParsingExtensions {

        /// <summary>
        /// Ensures the user message's intent is to set a new
        /// reminder, accepts any capitalization and variation
        /// in request (reminde me to, remind me that)
        /// </summary>
        /// <param name="message">User SMS message</param>
        /// <param name="reminder">The outputed reminder</param>
        /// <returns>True if intent was to set reminder</returns>
        public static bool IsReminderIntent(this string message, out string reminder) {
            string msg = message.ToLowerInvariant().Trim();
            string rawReminder;
            if (msg.RemoveIfStartsWith(out rawReminder, "remind me")) {
                if (rawReminder.RemoveIfStartsWith(out rawReminder, "to", "that")) {
                    reminder = rawReminder.CapitalizeFirstLetter();
                    return true;
                }
                reminder = rawReminder.CapitalizeFirstLetter();
                return true;
            }
            reminder = message;
            return false;
        }

        /// <summary>
        /// Ensures the user message's intent is to lookup
        /// most recent reminders
        /// </summary>
        /// <param name="message">User SMS message</param>
        /// <returns></returns>
        public static bool IsLookupIntent(this string message) {
            string _;
            return message.ToLowerInvariant().RemoveIfStartsWith(out _, "my reminder", "reminder", "what are my reminder");
        }

        /// <summary>
        /// Ensures the user message's intent is to get usage help
        /// </summary>
        /// <param name="message">User SMS message</param>
        /// <returns></returns>
        public static bool IsHelpIntent(this string message) {
            string _;
            return message.ToLowerInvariant().RemoveIfStartsWith(out _, "how", "how to", "how do");
        }

        /// <summary>
        /// Ensures the user message's intent is to reset password
        /// </summary>
        /// <param name="message">User SMS message</param>
        /// <returns></returns>
        public static bool IsResetPasswordIntent(this string message) {
            string _;
            return message.ToLowerInvariant().RemoveIfStartsWith(out _, "reset password");
        }

        /// <summary>
        /// Takes in a list of potential starting strings,
        /// checks if message starts with any of them and
        /// removes them from message if so
        /// </summary>
        /// <param name="raw">Unprocessed string</param>
        /// <param name="processed">String with removed starting string</param>
        /// <param name="toRemove">Potential strings to remove from start</param>
        /// <returns>True if string removed</returns>
        private static bool RemoveIfStartsWith(this string raw, out string processed, params string[] toRemove) {
            foreach (string remove in toRemove) {
                if (raw.StartsWith(remove)) {
                    if (raw.Length == remove.Length) {
                        processed = raw;
                    } else {
                        processed = raw.Substring(remove.Length + 1);
                    }

                    return true;
                }
            }

            processed = raw;
            return false;
        }

       // Capitalizes first letter of string
       public static string CapitalizeFirstLetter(this string input) {
            return $"{input.Substring(0, 1).ToUpperInvariant()}{input.Substring(1)}";
        }
    }
    public class TwillioSMSParser : AbstractSMSParser {
        private readonly AbstractSMSSender _smsSender;
        private readonly UserManager<ApplicationUser, long> _userManager;
        private readonly AbstractSMSRepository _smsRepository;
        private readonly AbstractTodoListRepository _todoListRepository;
        private readonly AbstractTodoTaskRepository _todoTaskRepository;


        /// <summary>
        /// Removes unessessary information from a user's
        /// phone number such as spaces and area code
        /// </summary>
        /// <param name="number">User's complete phone number</param>
        /// <returns>Simplified phone number</returns>
        public static string SimplifyNumber(string number) {
            string simplifiedNumber = number.Replace(" ", "").Replace("+", "");
            if (simplifiedNumber.StartsWith("61")) {
                simplifiedNumber = simplifiedNumber.Substring(2);
            }
            return simplifiedNumber;
        }

        /// <summary>
        /// Accepts a user SMS message, verifies its intent, 
        /// performs necessary repository related actions
        /// and parses output message to SMS sender class
        /// </summary>
        /// <param name="number">User's complete phone number</param>
        /// <param name="message">User's SMS message</param>
        /// <returns></returns>
        public override async Task ProcessSMSAsync(string number, string message) {
            try {
                // Generates SMS entity for storage
                await _smsRepository.SaveAsync(new Entity.SMS() {
                    Message = message,
                    NumberFrom = number,
                    NumberTo = ConfigurationManager.AppSettings["Twilio:OutgoingNumber"]
                });

                string simplifiedNumber = SimplifyNumber(number);
                ApplicationUser user = await _userManager.FindByNameAsync(simplifiedNumber);

                // Generates new user if message is sent from unknown number
                if (user == null) {
                    string password = Guid.NewGuid().ToString().Substring(0, 6).ToLowerInvariant();
                    IdentityResult ir = await _userManager.CreateAsync(new ApplicationUser() {
                        UserName = simplifiedNumber,
                        PhoneNumber = number,
                        PhoneNumberConfirmed = true,
                        EmailConfirmed = true,
                        Email = $"{simplifiedNumber}@foreverly.cloud",
                        SecurityStamp = $"{Guid.NewGuid():N}"
                    }, password);

                    await _smsSender.SendSMSAsync(number,
                        $"Your account on foreverly.cloud has been created!\r\nUsername: {simplifiedNumber}\r\nPassword: {password}");
                    user = await _userManager.FindByNameAsync(simplifiedNumber);
                }

                string reminder = null;
                if (message.IsReminderIntent(out reminder)) {
                    // User created new reminder, generates new list (if required)
                    // and adds reminder to it as a task
                    TodoList tl = user.TodoLists?.FirstOrDefault(x => x.ListName.Equals("Reminders"));

                    if (tl == null) {
                        tl = new TodoList {
                            ListName = "Reminders",
                            DateCreated = DateTime.Now,
                            UserID = user.Id,
                            LeftPositoned = true
                        };
                        await _todoListRepository.SaveAsync(tl);
                    }

                    TodoTask tt = new TodoTask {
                        TaskName = $"Reminder {(tl.TodoTasks?.Count ?? 0) + 1}",
                        Description = reminder,
                        Colour = "black",
                        ListID = tl.ListID
                    };

                    await _todoTaskRepository.SaveAsync(tt);

                    await _smsSender.SendSMSAsync(number, $"We've created your reminder under {tt.TaskName}");
                } else if (message.IsLookupIntent()) {
                    // User wishes to recieve last 5 reminders,
                    // obtains reminders from DB and outputs them to user
                    TodoList tl = user.TodoLists?.FirstOrDefault(x => x.ListName.Equals("Reminders"));
                    int remindersCount = tl?.TodoTasks?.Count ?? 0;

                    if (remindersCount == 0) {
                        await _smsSender.SendSMSAsync(number, "You have no reminders");
                    } else {
                        int remindersToSend = Math.Min(remindersCount, 5);

                        if (remindersToSend == 1) {
                            await _smsSender.SendSMSAsync(number, $"Your last reminder was:");
                        } else {
                            await _smsSender.SendSMSAsync(number, $"Your last {remindersToSend} reminders were:");
                        }

                        IEnumerable<TodoTask> localReminders =
                                tl.TodoTasks.OrderByDescending(x => x.TaskID).Take(remindersToSend);

                        foreach (TodoTask tt in localReminders) {
                            await _smsSender.SendSMSAsync(number, $"{tt.TaskName}: {tt.Description}");
                        }
                    }
                } else if (message.IsHelpIntent()) {
                    // User requires usage instructions
                    await _smsSender.SendSMSAsync(number, $"Say \"remind me to ...\" to set a reminder and \"reminders\" to get your last 5 reminders");
                } else if (message.IsResetPasswordIntent()) {
                    // User requires password reset
                    string password = Guid.NewGuid().ToString().Substring(0, 6).ToLowerInvariant();
                    
                    await ((ApplicationUserManager)_userManager).ChangePasswordAsync(user.Id, password);

                    await _smsSender.SendSMSAsync(number, $"Your new account details\r\nUsername: {user.UserName}\r\nPassword: {password}");
                } else {
                    // Recieved invalid message
                    await _smsSender.SendSMSAsync(number, $"I'm sorry, I don't understand that, type \"how ...\" for valid commands");
                }
            } catch {
                // Error occured
                await _smsSender.SendSMSAsync(number, $"Something went wrong, we were unable to complete your action");
            }

        }

        public TwillioSMSParser(AbstractSMSSender smsSender,
            UserManager<ApplicationUser, long> userManager,
            AbstractSMSRepository smsRepository,
            AbstractTodoListRepository todoListRepository,
            AbstractTodoTaskRepository todoTaskRepository) {
            _smsSender = smsSender;
            _userManager = userManager;
            _smsRepository = smsRepository;
            _todoListRepository = todoListRepository;
            _todoTaskRepository = todoTaskRepository;
        }
    }
}