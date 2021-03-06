﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FreneticScript.TagHandlers;
using FreneticScript.TagHandlers.Objects;

namespace FreneticScript.CommandSystem.QueueCmds
{
    /// <summary>
    /// A command to allow running scripts from the script folder.
    /// </summary>
    public class RunCommand : AbstractCommand
    {
        // TODO: Meta!
        // @Waitable

        /// <summary>
        /// Constructs the run command.
        /// </summary>
        public RunCommand()
        {
            Name = "run";
            Arguments = "<script to run>";
            Description = "Runs a script file.";
            // TODO: DEFINITION ARGS
            IsFlow = true;
            Waitable = true;
            Asyncable = true;
            MinimumArguments = 1;
            MaximumArguments = 1;
            ObjectTypes = new List<Func<TemplateObject, TemplateObject>>()
            {
                TextTag.For
            };
        }

        /// <summary>
        /// The first event fired in a sequence of three.
        /// <para/>Fires when a a script is going to be ran, cancellable.
        /// <para/>Contains the name of the script only.
        /// <para/>Second: <see cref="OnScriptRanEvent"/>.
        /// <para/>Third: <see cref="OnScriptRanPostEvent"/>.
        /// </summary>
        public FreneticScriptEventHandler<ScriptRanPreEventArgs> OnScriptRanPreEvent = new FreneticScriptEventHandler<ScriptRanPreEventArgs>();

        /// <summary>
        /// The second event fired in a sequence of three.
        /// <para/>Fires when a a script is about to be ran, cancellable.
        /// <para/>Contains a validly constructed <see cref="CommandScript"/> object.
        /// <para/>First: <see cref="OnScriptRanPreEvent"/>.
        /// <para/>Third: <see cref="OnScriptRanPostEvent"/>.
        /// </summary>
        public FreneticScriptEventHandler<ScriptRanEventArgs> OnScriptRanEvent = new FreneticScriptEventHandler<ScriptRanEventArgs>();

        /// <summary>
        /// The third event fired in a sequence of three.
        /// <para/>Fires when a a script has been ran, monitor-only.
        /// <para/>First: <see cref="OnScriptRanPreEvent"/>.
        /// <para/>Second: <see cref="OnScriptRanEvent"/>.
        /// </summary>
        public FreneticScriptEventHandler<ScriptRanPostEventArgs> OnScriptRanPostEvent = new FreneticScriptEventHandler<ScriptRanPostEventArgs>();

        /// <summary>
        /// Executes the run command.
        /// </summary>
        /// <param name="queue">The command queue involved.</param>
        /// <param name="entry">The command details to be ran.</param>
        public override void Execute(CommandQueue queue, CommandEntry entry)
        {
            string fname = entry.GetArgument(queue, 0).ToLowerFast();
            ScriptRanPreEventArgs args = new ScriptRanPreEventArgs();
            args.ScriptName = fname;
            if (OnScriptRanPreEvent != null)
            {
                OnScriptRanPreEvent.Fire(args);
            }
            if (args.Cancelled)
            {
                entry.Bad(queue, "Script running cancelled via the ScriptRanPreEvent.");
                if (entry.WaitFor && queue.WaitingOn == entry)
                {
                    queue.WaitingOn = null;
                }
                return;
            }
            CommandScript script = queue.CommandSystem.GetScript(args.ScriptName);
            if (script != null)
            {
                ScriptRanEventArgs args2 = new ScriptRanEventArgs();
                args2.Script = script;
                if (OnScriptRanEvent != null)
                {
                    OnScriptRanEvent.Fire(args2);
                }
                if (args2.Cancelled)
                {
                    entry.Bad(queue, "Script running cancelled via the ScriptRanEvent.");
                    if (entry.WaitFor && queue.WaitingOn == entry)
                    {
                        queue.WaitingOn = null;
                    }
                    return;
                }
                if (script == null)
                {
                    entry.Bad(queue, "Script running nullified via the ScriptRanEvent.");
                    if (entry.WaitFor && queue.WaitingOn == entry)
                    {
                        queue.WaitingOn = null;
                    }
                    return;
                }
                script = args2.Script;
                if (entry.ShouldShowGood(queue))
                {
                    entry.Good(queue, "Running '<{text_color[emphasis]}>" + TagParser.Escape(fname) + "<{text_color[base]}>'...");
                }
                CommandQueue nqueue;
                Dictionary<string, ObjectHolder> vars = new Dictionary<string, ObjectHolder>();
                queue.CommandSystem.ExecuteScript(script, ref vars, out nqueue);
                if (entry.WaitFor && queue.WaitingOn == entry)
                {
                    if (!nqueue.Running)
                    {
                        queue.WaitingOn = null;
                    }
                    else
                    {
                        EntryFinisher fin = new EntryFinisher() { Entry = entry, Queue = queue };
                        nqueue.Complete += fin.Complete;
                    }
                }
                ScriptRanPostEventArgs args4 = new ScriptRanPostEventArgs();
                args4.Script = script;
                if (OnScriptRanPostEvent != null)
                {
                    OnScriptRanPostEvent.Fire(args4);
                }
                // TODO: queue.SetVariable("run_variables", new MapTag(nqueue.LowestVariables)); // TODO: use the ^= syntax here.
            }
            else
            {
                queue.HandleError(entry, "Cannot run script '<{text_color[emphasis]}>" + TagParser.Escape(fname) + "<{text_color[base]}>': file does not exist!");
                if (entry.WaitFor && queue.WaitingOn == entry)
                { 
                    queue.WaitingOn = null;
                }
            }
        }
    }


    /// <summary>
    /// A mini-class used for the callback for &amp;waitable commands.
    /// </summary>
    public class EntryFinisher
    {
        /// <summary>
        /// The entry being waited on.
        /// </summary>
        public CommandEntry Entry;

        /// <summary>
        /// The relevant queue.
        /// </summary>
        public CommandQueue Queue;

        /// <summary>
        /// Add this function as a callback to complete entry.
        /// </summary>
        public void Complete(object sender, CommandQueueEventArgs args)
        {
            if (Queue.WaitingOn == Entry)
            {
                Queue.WaitingOn = null;
            }
        }
    }

    /// <summary>
    /// Fires when a a script is going to be ran, cancellable.
    /// </summary>
    public class ScriptRanPreEventArgs: EventArgs
    {
        /// <summary>
        /// The name of the script requested to be run.
        /// </summary>
        public string ScriptName;

        /// <summary>
        /// Whether the script should be prevented from running.
        /// </summary>
        public bool Cancelled = false;
    }

    /// <summary>
    /// Fires when a a script is about to be ran, cancellable.
    /// </summary>
    public class ScriptRanEventArgs : EventArgs
    {
        /// <summary>
        /// The script that will be ran.
        /// Do not edit.
        /// </summary>
        public CommandScript Script;

        /// <summary>
        /// Whether the script should be prevented from running.
        /// </summary>
        public bool Cancelled = false;
    }
    
    /// <summary>
    /// Fires when a a script has been ran, monitor-only.
    /// </summary>
    public class ScriptRanPostEventArgs : EventArgs
    {
        /// <summary>
        /// The script that was ran.
        /// Do not edit.
        /// </summary>
        public CommandScript Script;
    }
}
