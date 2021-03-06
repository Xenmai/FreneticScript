﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FreneticScript.CommandSystem.QueueCmds;
using FreneticScript.CommandSystem.CommonCmds;
using FreneticScript.TagHandlers;
using FreneticScript.CommandSystem.CommandEvents;

namespace FreneticScript.CommandSystem
{
    /// <summary>
    /// Handles all FreneticScript command systems. The entry point to FreneticScript.
    /// </summary>
    public class Commands
    {
        // <--[definition]
        // @Word argument
        // @Group commands
        // @Description The word 'argument', when used in a command description, refers to the any input value
        // outside the command itself.
        // Generally a command is formatted like:
        // /command <required argument> 'literal_argument'/'option2' ['optional literal'] [optional argument] [optional argument]
        // A required argument is an input that *must* be included, while an optional argument is something you
        // can choose whether or not to fill in. (Generally, if not included, they will receive default values
        // or just not be used, depending on the specific command and argument in question.) A literal argument
        // is one the should be input exactly as-is. In the example above, "literal_argument" or "option2" must
        // be typed in exactly, or the command will fail.
        // An optional literal is similar to a literal, except it is not required.
        // A / between two arguments, EG "<required argument>/'literal_argument'" means you may pick either
        // "literal_argument" as input, or you can fill in the required argument there, but not both.
        // -->

        /// <summary>
        /// A full dictionary of all registered commands.
        /// </summary>
        public Dictionary<string, AbstractCommand> RegisteredCommands;

        /// <summary>
        /// A full list of all registered commands.
        /// </summary>
        public List<AbstractCommand> RegisteredCommandList;

        /// <summary>
        /// All command queues currently running.
        /// </summary>
        public List<CommandQueue> Queues;

        /// <summary>
        /// The tag handling system.
        /// </summary>
        public TagParser TagSystem;

        /// <summary>
        /// The output system.
        /// </summary>
        public Outputter Output;

        /// <summary>
        /// The AbstractCommand for the invalid command-command.
        /// </summary>
        public DebugOutputInvalidCommand DebugInvalidCommand;

        /// <summary>
        /// The AbstractCommand for the var-set command.
        /// </summary>
        public DebugVarSetCommand DebugVarSetCommand;
        
        /// <summary>
        /// All functions this command system has loaded.
        /// </summary>
        public Dictionary<string, CommandScript> Functions;

        /// <summary>
        /// All script events this command system is aware of.
        /// </summary>
        public Dictionary<string, ScriptEvent> Events;

        /// <summary>
        /// All known identifiers for the 'once' block command.
        /// </summary>
        public HashSet<string> OnceBlocks = new HashSet<string>();
        
        /// <summary>
        /// A random number generator.
        /// </summary>
        public Random random = new Random();

        /// <summary>
        /// Reloads the entire command system.
        /// </summary>
        public void Reload()
        {
            OnceBlocks.Clear();
            Functions.Clear();
            foreach (ScriptEvent evt in Events.Values)
            {
                foreach (KeyValuePair<int, CommandScript> handl in new List<KeyValuePair<int, CommandScript>>(evt.Handlers))
                {
                    evt.RemoveEventHandler(handl.Value.Name);
                }
            }
            Output.Reload();
        }

        /// <summary>
        /// Executes a command script.
        /// Returns the determined value(s).
        /// </summary>
        /// <param name="script">The script to execute.</param>
        /// <param name="Variables">What variables to add to the commandqueue.</param>
        /// <param name="queue">Outputs the generated queue (already ran or running).</param>
        /// <param name="mode">The debug mode to run it in.</param>
        public void ExecuteScript(CommandScript script, ref Dictionary<string, ObjectHolder> Variables, out CommandQueue queue, DebugMode mode = DebugMode.FULL)
        {
            queue = script.ToQueue(this);
            if (Variables != null)
            {
                // TODO: Restore variable tracking!
                /*foreach (KeyValuePair<string, ObjectHolder> variable in Variables)
                {
                    queue.SetVariable(variable.Key, variable.Value.Internal);
                }*/
            }
            CommandStackEntry cse = queue.CommandStack.Peek();
            cse.Debug = mode;
            queue.Execute();
            //Variables = queue.LowestVariables;
        }

        /// <summary>
        /// Gets a script saved in the command system by name, or creates one from file.
        /// </summary>
        /// <param name="script">The name of the script.</param>
        /// <returns>A script, or null if there's no match.</returns>
        public CommandScript GetScript(string script)
        {
            return CommandScript.GetByFileName(script, this);
        }

        /// <summary>
        /// Gets a function saved in the command system by name.
        /// </summary>
        /// <param name="script">The name of the script.</param>
        /// <returns>A script, or null if there's no match.</returns>
        public CommandScript GetFunction(string script)
        {
            CommandScript commandscript;
            if (Functions.TryGetValue(script.ToLowerFast(), out commandscript))
            {
                return commandscript;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Precalculates a script file to potentially be run.
        /// </summary>
        /// <param name="name">The name of the script.</param>
        /// <param name="script">The script to run.</param>
        public void PrecalcScript(string name, string script)
        {
            try
            {
                script = script.Replace("\r", "").Replace("\0", "\\0");
                string[] dat = script.SplitFast('\n');
                bool shouldarun = false;
                int arun = 0;
                for (int i = 0; i < dat.Length; i++)
                {
                    string trimmed = dat[i].Trim();
                    if (trimmed.Length == 0)
                    {
                        continue;
                    }
                    if (trimmed.StartsWith("///"))
                    {
                        string[] args = trimmed.Substring(3).SplitFast('=');
                        string mode = args[0].Trim().ToLowerFast();
                        if (mode == "autorun")
                        {
                            shouldarun = true;
                            arun = FreneticScriptUtilities.StringToInt(args[1]);
                        }
                        continue;
                    }
                    break;
                }
                if (shouldarun)
                {
                    CommandScript cscript = CommandScript.SeparateCommands(name, script, this);
                    if (cscript == null)
                    {
                        return;
                    }
                    for (int i = 0; i < scriptsToRun.Count; i++)
                    {
                        if (scriptsToRun[i].Key == arun)
                        {
                            scriptsToRun[i].Value.Add(cscript);
                            return;
                        }
                    }
                    scriptsToRun.Add(new KeyValuePair<int, List<CommandScript>>(arun, new List<CommandScript>() { cscript }));
                    return;
                }
            }
            catch (Exception ex)
            {
                if (ex is System.Threading.ThreadAbortException)
                {
                    throw ex;
                }
                Output.BadOutput("Found exception while precalculating script '" + name + "'...: " + ex.ToString());
            }
        }

        /// <summary>
        /// Runs any precalculated scripts.
        /// </summary>
        public void RunPrecalculated()
        {
            scriptsToRun.Sort((one, two) => (one.Key.CompareTo(two.Key)));
            for (int i = 0; i < scriptsToRun.Count; i++)
            {
                for (int x = 0; x < scriptsToRun[i].Value.Count; x++)
                {
                    CommandQueue queue = scriptsToRun[i].Value[x].ToQueue(this);
                    queue.CommandStack.Peek().Debug = DebugMode.MINIMAL;
                    queue.Execute();
                }
            }
            scriptsToRun.Clear();
        }

        List<KeyValuePair<int, List<CommandScript>>> scriptsToRun = new List<KeyValuePair<int, List<CommandScript>>>();

        /// <summary>
        /// A function to invoke when output is generated.
        /// </summary>
        public delegate void OutputFunction(string message, MessageType type);

        /// <summary>
        /// Executes an arbitrary list of command inputs (separated by newlines, semicolons, ...)
        /// </summary>
        /// <param name="commands">The command string to parse.</param>
        /// <param name="outputter">The output function to call, or null if none.</param>
        public void ExecuteCommands(string commands, OutputFunction outputter)
        {
            CommandScript cs = CommandScript.SeparateCommands("command_line", commands, this);
            if (cs == null)
            {
                outputter?.Invoke("Invalid commands specified, error outputted to logs.", MessageType.BAD);
                return;
            }
            cs.Debug = DebugMode.FULL;
            CommandQueue queue = cs.ToQueue(this);
            queue.Outputsystem = outputter;
            queue.Execute();
        }
        
        /// <summary>
        /// Adds a command to the registered command list.
        /// </summary>
        /// <param name="command">The command to register.</param>
        public void RegisterCommand(AbstractCommand command)
        {
            command.Name = command.Name.ToLowerFast(); // Just a quick backup in case somebody messed up.
            command.CommandSystem = this;
            if (RegisteredCommands.ContainsKey(command.Name))
            {
                Output.BadOutput("Multiply registered command: " + command.Name + "!");
                return;
            }
            RegisteredCommands.Add(command.Name, command);
            RegisteredCommandList.Add(command);
        }

        /// <summary>
        /// Removes a command from the registered command list.
        /// Silently fails if command is not registered.
        /// </summary>
        /// <param name="name">The name of the command to remove.</param>
        public void UnregisterCommand(string name)
        {
            string namelow = name.ToLowerFast();
            AbstractCommand cmd;
            if (RegisteredCommands.TryGetValue(namelow, out cmd))
            {
                RegisteredCommands.Remove(namelow);
                RegisteredCommandList.Remove(cmd);
            }
        }

        /// <summary>
        /// Registers a script event to the system.
        /// </summary>
        /// <param name="newevent">The event to register.</param>
        public void RegisterEvent(ScriptEvent newevent)
        {
            Events.Add(newevent.Name, newevent);
        }
        
        /// <summary>
        /// Prepares the command system, registering all base commands.
        /// </summary>
        public void Init()
        {
            RegisteredCommands = new Dictionary<string, AbstractCommand>(30);
            RegisteredCommandList = new List<AbstractCommand>(30);
            Functions = new Dictionary<string, CommandScript>(30);
            Events = new Dictionary<string, ScriptEvent>(30);
            Queues = new List<CommandQueue>(20);
            TagSystem = new TagParser();
            TagSystem.Init(this);

            // Queue-related Commands
            RegisterCommand(new AssertCommand());
            RegisterCommand(new BreakCommand());
            RegisterCommand(new CallCommand());
            RegisterCommand(new CatchCommand());
            RegisterCommand(new DebugCommand());
            RegisterCommand(new DelayCommand());
            RegisterCommand(new ElseCommand());
            RegisterCommand(new ErrorCommand());
            RegisterCommand(new EventCommand());
            RegisterCommand(new ForeachCommand());
            RegisterCommand(new FunctionCommand());
            RegisterCommand(new GotoCommand());
            RegisterCommand(new IfCommand());
            RegisterCommand(new InjectCommand());
            RegisterCommand(new MarkCommand());
            RegisterCommand(new OnceCommand());
            RegisterCommand(new ParsingCommand());
            RegisterCommand(new RepeatCommand());
            RegisterCommand(new RequireCommand());
            RegisterCommand(TheRunCommand = new RunCommand());
            RegisterCommand(new StopCommand());
            RegisterCommand(new TryCommand());
            RegisterCommand(new VarCommand());
            RegisterCommand(new WaitCommand());
            RegisterCommand(new WhileCommand());

            // Register debug command
            RegisterCommand(DebugInvalidCommand = new DebugOutputInvalidCommand());
            RegisterCommand(DebugVarSetCommand = new DebugVarSetCommand());

            // Common Commands
            RegisterCommand(new CleanmemCommand());
            RegisterCommand(new CvarinfoCommand());
            RegisterCommand(new EchoCommand());
            RegisterCommand(new HelpCommand());
            RegisterCommand(new NoopCommand());
            RegisterCommand(new ReloadCommand());
            RegisterCommand(new SetCommand());
            RegisterCommand(new ToggleCommand());

            // Command-Related Events
            RegisterEvent(new ScriptRanPreScriptEvent(this));
            RegisterEvent(new ScriptRanScriptEvent(this));
            RegisterEvent(new ScriptRanPostScriptEvent(this));
        }

        /// <summary>
        /// Final preparation for the command system, after all data has been registered.
        /// </summary>
        public void PostInit()
        {
            TagSystem.PostInit();
        }

        /// <summary>
        /// The registered RunCommand instance.
        /// </summary>
        public RunCommand TheRunCommand;

        /// <summary>
        /// Advances any running command queues.
        /// </summary>
        /// <param name="Delta">The time passed this tick.</param>
        public void Tick(double Delta)
        {
            for (int i = 0; i < Queues.Count; i++)
            {
                Queues[i].Tick(Delta);
                if (!Queues[i].Running)
                {
                    Queues.RemoveAt(i);
                    i--;
                }
            }
        }
    }
}
