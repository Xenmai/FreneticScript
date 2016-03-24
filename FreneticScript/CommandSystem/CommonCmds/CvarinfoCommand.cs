﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FreneticScript.CommandSystem;
using FreneticScript.TagHandlers;
using FreneticScript.TagHandlers.Objects;

namespace FreneticScript.CommandSystem.CommonCmds
{
    class CvarinfoCommand: AbstractCommand
    {
        // TODO: Meta!

        public CvarinfoCommand()
        {
            Name = "cvarinfo";
            Arguments = "[CVar to get info on]";
            Description = "Shows information on a specified CVar, or all of them if one isn't specified.";
            MinimumArguments = 0;
            MaximumArguments = 1;
            ObjectTypes = new List<Func<TemplateObject, TemplateObject>>()
            {
                (input) =>
                {
                    return new TextTag(input.ToString());
                }
            };
        }

        /// <summary>
        /// Executes the command.
        /// </summary>
        /// <param name="entry">Entry to be executed.</param>
        public override void Execute(CommandEntry entry)
        {
            if (entry.Arguments.Count < 1)
            {
                entry.Info("Listing <{text_color.emphasis}>" + entry.Output.CVarSys.CVars.Count + "<{text_color.base}> CVars...");
                for (int i = 0; i < entry.Output.CVarSys.CVars.Count; i++)
                {
                    CVar cvar = entry.Output.CVarSys.CVarList[i];
                    entry.Info("<{text_color.emphasis}>" + (i + 1).ToString() + "<{text_color.simple}>)<{text_color.emphasis}> " + TagParser.Escape(cvar.Info()));
                }
            }
            else
            {
                string target = entry.GetArgument(0).ToLowerInvariant();
                List<CVar> cvars = new List<CVar>();
                for (int i = 0; i < entry.Output.CVarSys.CVars.Count; i++)
                {
                    if (entry.Output.CVarSys.CVarList[i].Name.StartsWith(target))
                    {
                        cvars.Add(entry.Output.CVarSys.CVarList[i]);
                    }
                }
                if (cvars.Count == 0)
                {
                    entry.Error("CVar '<{text_color.emphasis}>" + TagParser.Escape(target) + "<{text_color.base}>' does not exist!");
                }
                else
                {
                    entry.Info("Listing <{text_color.emphasis}>" + cvars.Count + "<{text_color.base}> CVars...");
                    for (int i = 0; i < cvars.Count; i++)
                    {
                        CVar cvar = cvars[i];
                        entry.Info("<{text_color.emphasis}>" + (i + 1).ToString() + "<{text_color.simple}>)<{text_color.emphasis}> " + TagParser.Escape(cvar.Info()));
                    }
                }
            }
        }
    }
}
