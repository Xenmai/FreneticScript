﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FreneticScript.TagHandlers.Objects;

namespace FreneticScript.TagHandlers.Common
{
    class TagTypeBase : TemplateTagBase
    {
        // <--[tagbase]
        // @Base tagtype[<BinaryTag>]
        // @Group Common Base Types
        // @ReturnType TagTypeTag
        // @Returns the input data as a TagTypeTag.
        // -->

        public TagTypeBase()
        {
            Name = "tagtype";
            ResultTypeString = "tagtypetag";
        }

        public override TemplateObject HandleOne(TagData data)
        {
            return TagTypeTag.For(data, data.GetModifierObject(0));
        }
    }
}
