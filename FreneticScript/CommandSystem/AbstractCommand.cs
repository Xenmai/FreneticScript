﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FreneticScript.TagHandlers;
using FreneticScript.CommandSystem.Arguments;
using System.Reflection;
using System.Reflection.Emit;

namespace FreneticScript.CommandSystem
{
    /// <summary>
    /// The base for a command.
    /// </summary>
    public abstract class AbstractCommand
    {
        /// <summary>
        /// The name of the command.
        /// </summary>
        public string Name = "NAME:UNSET";

        /// <summary>
        /// The system that owns this command.
        /// </summary>
        public Commands CommandSystem;

        /// <summary>
        /// A short explanation of the arguments of the command.
        /// </summary>
        public string Arguments = "ARGUMENTS:UNSET";

        /// <summary>
        /// A short explanation of what the command does.
        /// </summary>
        public string Description = "DESCRIPTION:UNSET";

        /// <summary>
        /// Whether the command is for debugging purposes.
        /// </summary>
        public bool IsDebug = false;

        /// <summary>
        /// Whether the 'break' command can be used on this command.
        /// </summary>
        public bool IsBreakable = false;

        /// <summary>
        /// Whether the command is part of a script's flow rather than for normal client use.
        /// </summary>
        public bool IsFlow = false;

        /// <summary>
        /// Whether the command can be &amp;waited on.
        /// </summary>
        public bool Waitable = false;

        /// <summary>
        /// Whether the command can be run off the primary tick.
        /// NOTE: These mostly have yet to be confirmed! They are purely theoretical!
        /// </summary>
        public bool Asyncable = false;

        /// <summary>
        /// How many arguments the command can have minimum.
        /// </summary>
        public int MinimumArguments = 0;

        /// <summary>
        /// How many arguments the command can have maximum.
        /// </summary>
        public int MaximumArguments = 100;

        /// <summary>
        /// The expected object type getters for a command.
        /// </summary>
        public List<Func<TemplateObject, TemplateObject>> ObjectTypes = null;
        
        /// <summary>
        /// Tests if the CommandEntry is valid for this command at pre-process time.
        /// </summary>
        /// <param name="entry">The entry to test</param>
        /// <returns>An error message (with tags), or null for none.</returns>
        public virtual string TestForValidity(CommandEntry entry)
        {
            if (entry.Arguments.Count < MinimumArguments)
            {
                return "Not enough arguments. Expected at least: " + MinimumArguments + ". Usage: " + TagParser.Escape(Arguments) + ", found only: " + TagParser.Escape(entry.AllOriginalArguments());
            }
            if (MaximumArguments != -1 && entry.Arguments.Count > MaximumArguments)
            {
                return "Too many arguments. Expected no more than: " + MaximumArguments + ". Usage: " + TagParser.Escape(Arguments) + ", found: " + TagParser.Escape(entry.AllOriginalArguments());
            }
            if (ObjectTypes != null)
            {
                for (int i = 0; i < entry.Arguments.Count; i++)
                {
                    if (entry.Arguments[i].Bits.Count == 1
                        && entry.Arguments[i].Bits[0] is TextArgumentBit
                        && i < ObjectTypes.Count)
                    {
                        TemplateObject obj = ObjectTypes[i].Invoke(((TextArgumentBit)entry.Arguments[i].Bits[0]).InputValue);
                        if (obj == null)
                        {
                            return "Invalid argument '" + TagParser.Escape(entry.Arguments[i].ToString())
                                + "', translates to NULL for this command's input expectation (Command is " + TagParser.Escape(entry.Command.Name) + ").";
                        }
                        ((TextArgumentBit)entry.Arguments[i].Bits[0]).InputValue = obj;
                    }
                }
            }
            return null;
        }
        
        /// <summary>
        /// Gets the follower (callback) entry for an entry.
        /// </summary>
        /// <param name="entry">The entry.</param>
        public CommandEntry GetFollower(CommandEntry entry)
        {
            return new CommandEntry(entry.Name + " \0CALLBACK", entry.BlockStart, entry.BlockEnd, entry.Command, new List<Argument>() { new Argument() { Bits = new List<ArgumentBit>() {
                new TextArgumentBit("\0CALLBACK", false) } } }, entry.Name, 0, entry.ScriptName, entry.ScriptLine, entry.FairTabulation + "    ", entry.System);
        }

        /// <summary>
        /// Adapts a command entry to CIL.
        /// </summary>
        /// <param name="values">The adaptation-relevant values.</param>
        /// <param name="entry">The present entry ID.</param>
        public virtual void AdaptToCIL(CILAdaptationValues values, int entry)
        {
            values.CallExecute(entry);
        }

        /// <summary>
        /// Prepares to adapt a command entry to CIL.
        /// </summary>
        /// <param name="values">The adaptation-relevant values.</param>
        /// <param name="entry">The present entry ID.</param>
        public virtual void PreAdaptToCIL(CILAdaptationValues values, int entry)
        {
            // Do nothing.
        }

        /// <summary>
        /// Adjust list of commands that formed by an inner block.
        /// </summary>
        /// <param name="entry">The producing entry.</param>
        /// <param name="input">The block of commands.</param>
        /// <param name="fblock">The final block to add to the entry.</param>
        public virtual void AdaptBlockFollowers(CommandEntry entry, List<CommandEntry> input, List<CommandEntry> fblock)
        {
            input.Add(GetFollower(entry));
        }

        /// <summary>
        /// Executes the command.
        /// </summary>
        /// <param name="queue">The command queue involved.</param>
        /// <param name="entry">Entry to be executed.</param>
        public abstract void Execute(CommandQueue queue, CommandEntry entry);

        /// <summary>
        /// Displays the usage information on a command to the console.
        /// </summary>
        /// <param name="queue">The associated queue.</param>
        /// <param name="entry">The CommandEntry data to show usage help to.</param>
        /// <param name="doError">Whether to end with an error.</param>
        public void ShowUsage(CommandQueue queue, CommandEntry entry, bool doError = true)
        {
            if (entry.ShouldShowGood(queue))
            {
                entry.InfoOutput(queue, TextStyle.Color_Separate + Name + TextStyle.Color_Base + ": " + Description);
                entry.InfoOutput(queue, TextStyle.Color_Commandhelp + "Usage: /" + Name + " " + Arguments);
                if (IsDebug)
                {
                    entry.InfoOutput(queue, "Note: This command is intended for debugging purposes.");
                }
            }
            if (doError)
            {
                queue.HandleError(entry, "Invalid arguments or not enough arguments!");
            }
        }
    }

    /// <summary>
    /// Holder of CIL Variable data.
    /// </summary>
    public class CILVariables
    {
        /// <summary>
        /// A map of local variables to track.
        /// </summary>
        public List<Tuple<int, string, TagType>> LVariables = new List<Tuple<int, string, TagType>>();
    }

    /// <summary>
    /// Holds all data needed for CIL adaptation.
    /// </summary>
    public class CILAdaptationValues
    {
        /// <summary>
        /// The compiled CSE involved.
        /// </summary>
        public CompiledCommandStackEntry Entry;

        /// <summary>
        /// The compiling script.
        /// </summary>
        public CommandScript Script;

        /// <summary>
        /// Represents the field "CSEntry" in the class CompiledCommandRunnable.
        /// </summary>
        public static FieldInfo EntryField = typeof(CompiledCommandRunnable).GetField("CSEntry");

        /// <summary>
        /// Represents the field "Entries" in the class CommandStackEntry.
        /// </summary>
        public static FieldInfo EntriesField = typeof(CommandStackEntry).GetField("Entries");

        /// <summary>
        /// Represents the field "Command" in the class CommandEntry.
        /// </summary>
        public static FieldInfo Entry_CommandField = typeof(CommandEntry).GetField("Command");

        /// <summary>
        /// Represents the field "GetArgumentObject" in the class CommandEntry.
        /// </summary>
        public static MethodInfo Entry_GetArgumentObjectMethod = typeof(CommandEntry).GetMethod("GetArgumentObject", new Type[] { typeof(CommandQueue), typeof(int) });

        /// <summary>
        /// Represents the field "Internal" in the class IntHolder.
        /// </summary>
        public static FieldInfo IntHolder_InternalField = typeof(IntHolder).GetField("Internal");

        /// <summary>
        /// Represents the "Execute(queue, entry)" method.
        /// </summary>
        public static MethodInfo ExecuteMethod = typeof(AbstractCommand).GetMethod("Execute", new Type[] { typeof(CommandQueue), typeof(CommandEntry) });

        /// <summary>
        /// Represents the "SetLocalVar(c, value)" method in the class CommandQueue.
        /// </summary>
        public static MethodInfo Queue_SetLocalVarMethod = typeof(CommandQueue).GetMethod("SetLocalVar", new Type[] { typeof(int), typeof(TemplateObject) });
        
        /// <summary>
        /// The type of the class CommandEntry.
        /// </summary>
        public static Type CommandEntryType = typeof(CommandEntry);

        /// <summary>
        /// Tracks generated IL.
        /// </summary>
        public class ILGeneratorTracker
        {
            /// <summary>
            /// Internal generator.
            /// </summary>
            public ILGenerator Internal;

            /// <summary>
            /// All codes generated.
            /// </summary>
            public List<KeyValuePair<OpCode, object>> Codes = new List<KeyValuePair<OpCode, object>>();

            /// <summary>
            /// Defines a label.
            /// </summary>
            /// <returns>The label.</returns>
            public Label DefineLabel()
            {
                return Internal.DefineLabel();
            }

            /// <summary>
            /// Marks a label.
            /// </summary>
            /// <param name="label">The label.</param>
            public void MarkLabel(Label label)
            {
                Internal.MarkLabel(label);
                Codes.Add(new KeyValuePair<OpCode, object>(OpCodes.Nop, "<Mark label>: " + label));
            }

            /// <summary>
            /// Emits an operation.
            /// </summary>
            /// <param name="code">The operation code.</param>
            public void Emit(OpCode code)
            {
                Internal.Emit(code);
                Codes.Add(new KeyValuePair<OpCode, object>(code, null));
            }

            /// <summary>
            /// Emits an operation.
            /// </summary>
            /// <param name="code">The operation code.</param>
            /// <param name="dat">The associated data.</param>
            public void Emit(OpCode code, FieldInfo dat)
            {
                Internal.Emit(code, dat);
                Codes.Add(new KeyValuePair<OpCode, object>(code, dat));
            }

            /// <summary>
            /// Emits an operation.
            /// </summary>
            /// <param name="code">The operation code.</param>
            /// <param name="dat">The associated data.</param>
            public void Emit(OpCode code, Label[] dat)
            {
                Internal.Emit(code, dat);
                Codes.Add(new KeyValuePair<OpCode, object>(code, dat));
            }

            /// <summary>
            /// Emits an operation.
            /// </summary>
            /// <param name="code">The operation code.</param>
            /// <param name="dat">The associated data.</param>
            public void Emit(OpCode code, MethodInfo dat)
            {
                Internal.Emit(code, dat);
                Codes.Add(new KeyValuePair<OpCode, object>(code, dat + ": " + dat.DeclaringType.Name));
            }

            /// <summary>
            /// Emits an operation.
            /// </summary>
            /// <param name="code">The operation code.</param>
            /// <param name="dat">The associated data.</param>
            public void Emit(OpCode code, Label dat)
            {
                Internal.Emit(code, dat);
                Codes.Add(new KeyValuePair<OpCode, object>(code, dat));
            }

            /// <summary>
            /// Emits an operation.
            /// </summary>
            /// <param name="code">The operation code.</param>
            /// <param name="dat">The associated data.</param>
            public void Emit(OpCode code, string dat)
            {
                Internal.Emit(code, dat);
                Codes.Add(new KeyValuePair<OpCode, object>(code, dat));
            }

            /// <summary>
            /// Emits an operation.
            /// </summary>
            /// <param name="code">The operation code.</param>
            /// <param name="dat">The associated data.</param>
            public void Emit(OpCode code, int dat)
            {
                Internal.Emit(code, dat);
                Codes.Add(new KeyValuePair<OpCode, object>(code, dat));
            }

            /// <summary>
            /// Declares a local.
            /// </summary>
            /// <param name="t">The type.</param>
            public void DeclareLocal(Type t)
            {
                Internal.DeclareLocal(t);
                Codes.Add(new KeyValuePair<OpCode, object>(OpCodes.Nop, "<Declare local>: " + t.FullName));
            }
        }

        /// <summary>
        /// The IL code generator.
        /// </summary>
        public ILGeneratorTracker ILGen;
        
        /// <summary>
        /// The method being constructed.
        /// </summary>
        public MethodBuilder Method;

        /// <summary>
        /// Returns the location of a local variable's name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The location.</returns>
        public int LocalVariableLocation(string name)
        {
            foreach (int i in LVarIDs)
            {
                for (int x = 0; x < CLVariables[i].LVariables.Count; x++)
                {
                    if (CLVariables[i].LVariables[x].Item2 == name)
                    {
                        return CLVariables[i].LVariables[x].Item1;
                    }
                }
            }
            return -1;
        }

        /// <summary>
        /// Pushes a new set of variables, to start a scope.
        /// </summary>
        public void PushVarSet()
        {
            LVarIDs.Push(CLVariables.Count);
            CLVariables.Add(new CILVariables());
        }

        /// <summary>
        /// Pops the newest set of variables, to end a scope.
        /// </summary>
        public void PopVarSet()
        {
            LVarIDs.Pop();
        }

        /// <summary>
        /// Adds a variable at the highest scope.
        /// </summary>
        /// <param name="var">The variable name.</param>
        /// <param name="type">The variable value.</param>
        public void AddVariable(string var, TagType type)
        {
            CLVariables[LVarIDs.Peek()].LVariables.Add(new Tuple<int, string, TagType>(CLVarID++, var, type));
        }

        /// <summary>
        /// All known CIL Variable data sets.
        /// </summary>
        public List<CILVariables> CLVariables = new List<CILVariables>();

        /// <summary>
        /// The current stackl of LVarIDs.
        /// </summary>
        public Stack<int> LVarIDs = new Stack<int>();
        
        /// <summary>
        /// The current CIL Var ID.
        /// </summary>
        public int CLVarID = 0;

        /// <summary>
        /// Load the entry onto the stack.
        /// </summary>
        public void LoadEntry(int entry)
        {
            ILGen.Emit(OpCodes.Ldarg_0);
            ILGen.Emit(OpCodes.Ldfld, EntryField);
            ILGen.Emit(OpCodes.Ldfld, EntriesField);
            ILGen.Emit(OpCodes.Ldc_I4, entry);
            ILGen.Emit(OpCodes.Ldelem_Ref);
        }

        /// <summary>
        /// Load the queue variable onto the stack.
        /// </summary>
        public void LoadQueue()
        {
            ILGen.Emit(OpCodes.Ldarg_1);
        }

        /// <summary>
        /// Marks the command as the correct entry. Should be called with every command!
        /// </summary>
        /// <param name="entry">The entry location.</param>
        public void MarkCommand(int entry)
        {
            ILGen.Emit(OpCodes.Ldarg_2);
            ILGen.Emit(OpCodes.Ldc_I4, entry);
            ILGen.Emit(OpCodes.Stfld, IntHolder_InternalField);
        }

        /// <summary>
        /// Loads the command, the entry, and the queue, for calling an execution function.
        /// </summary>
        /// <param name="entry">The entry location.</param>
        public void PrepareExecutionCall(int entry)
        {
            MarkCommand(entry);
            LoadEntry(entry);
            ILGen.Emit(OpCodes.Ldfld, Entry_CommandField);
            LoadQueue();
            LoadEntry(entry); // Awkward -> avoid duplicate call?
        }

        /// <summary>
        /// Call the "Execute(queue, entry)" method with appropriate parameters.
        /// </summary>
        public void CallExecute(int entry)
        {
            PrepareExecutionCall(entry);
            ILGen.Emit(OpCodes.Callvirt, ExecuteMethod);
        }
    }
}
