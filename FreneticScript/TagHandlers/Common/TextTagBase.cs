﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FreneticScript.TagHandlers.Objects;

namespace FreneticScript.TagHandlers.Common
{
    // <--[explanation]
    // @Name Text Tags
    // @Description
    // TextTags are any random text, built into the tag system.
    // TODO: Explain better
    // TODO: Link tag system explanation
    // -->
    class TextTagBase : TemplateTagBase
    {
        // <--[tagbase]
        // @Base text[<TextTag>]
        // @Group Common Base Types
        // @ReturnType TextTag
        // @Returns the input text as a TextTag.
        // <@link explanation Text Tags>What are text tags?<@/link>
        // -->

        public TextTagBase()
        {
            Name = "text";
            ResultTypeString = "texttag";
        }

        public override TemplateObject HandleOne(TagData data)
        {
            return new TextTag(data.GetModifier(0));
        }
    }
}
