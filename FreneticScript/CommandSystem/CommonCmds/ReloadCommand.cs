﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FreneticScript.CommandSystem;
using FreneticScript.TagHandlers;
using FreneticScript.TagHandlers.Objects;

namespace FreneticScript.CommandSystem.CommonCmds
{
    class ReloadCommand : AbstractCommand
    {
        // TODO: Meta!

        public ReloadCommand()
        {
            Name = "reload";
            Arguments = "";
            Description = "Reloads the command engine.";
            MinimumArguments = 0;
            MaximumArguments = 0;
            ObjectTypes = new List<Func<TemplateObject, TemplateObject>>();
        }

        /// <summary>
        /// Executes the command.
        /// </summary>
        /// <param name="queue">The command queue involved.</param>
        /// <param name="entry">Entry to be executed.</param>
        public override void Execute(CommandQueue queue, CommandEntry entry)
        {
            if (entry.ShouldShowGood(queue))
            {
                entry.Good(queue, "Reloading...");
            }
            queue.CommandSystem.Reload();
            if (entry.ShouldShowGood(queue))
            {
                entry.Good(queue, "Reloaded!");
            }
        }
    }
}
