﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FreneticScript.TagHandlers;
using FreneticScript.TagHandlers.Objects;

namespace FreneticScript.CommandSystem.QueueCmds
{
    // <--[command]
    // @Name call
    // @Arguments <function to call> [<variable>:<value> ...]
    // @Short Runs a function.
    // @Updated 2014/06/23
    // @Authors mcmonkey
    // @Group Queue
    // @Minium 1
    // @Maximum -1
    // @Description
    // Activates a function created by the <@link command function>function<@/link> command.
    // Note that 'injected' function calls do not take variable inputs (they use the current queue's variables),
    // and do not output detemrinations!
    // TODO: Explain more!
    // @Example
    // // This example calls the function 'helloworld'.
    // call helloworld
    // @Example
    // // This example calls the function 'outputme' with variable 'text' set to 'hello world'.
    // call outputme "text:hello world"
    // @Example
    // TODO: More examples!
    // @Tags
    // <{var[call_determinations]}> returns what the called function determined, if anything (Only for 'run' mode).
    // @BlockVars
    // call_determinations ListTag
    // -->
    // TODO: Make insert_function entirely its own separate command.
    class CallCommand : AbstractCommand
    {
        public CallCommand()
        {
            Name = "call";
            Arguments = "<function to call> [<variable>:<value> ...]";
            Description = "Runs a function.";
            IsFlow = true;
            Asyncable = true;
            MinimumArguments = 1;
            MaximumArguments = -1;
        }

        public override void Execute(CommandEntry entry)
        {
            string fname = entry.GetArgument(0);
            fname = fname.ToLowerInvariant();
            CommandScript script = entry.Queue.CommandSystem.GetFunction(fname);
            if (script == null)
            {
                entry.Error("Cannot call function '<{text_color.emphasis}>" + TagParser.Escape(fname) + "<{text_color.base}>': it does not exist!");
                return;
            }
            if (entry.ShouldShowGood())
            {
                entry.Good("Calling '<{text_color.emphasis}>" + TagParser.Escape(fname) + "<{text_color.base}>'...");
            }
            CommandQueue queue;
            Dictionary<string, TemplateObject> variables = new Dictionary<string, TemplateObject>();
            for (int i = 1; i < entry.Arguments.Count; i++)
            {
                string str = entry.GetArgument(i);
                if (!str.Contains(':'))
                {
                    entry.Error("Invalid variable input!");
                    return;
                }
                string[] split = str.Split(new char[] { ':' }, 2);
                variables.Add(split[0], new TextTag(split[1]));
            }
            entry.Queue.CommandSystem.ExecuteScript(script, variables, out queue, DebugMode.MINIMAL);
            if (!queue.Running)
            {
                entry.Finished = true;
            }
            else
            {
                EntryFinisher fin = new EntryFinisher() { Entry = entry };
                queue.Complete += new EventHandler<CommandQueueEventArgs>(fin.Complete);
            }
            ListTag list = new ListTag(queue.Determinations);
            entry.Queue.SetVariable("call_determinations", list);
        }
    }
}
