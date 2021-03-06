﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreneticScript.TagHandlers.Objects
{
    /// <summary>
    /// Represents a TagType, as a tag.
    /// </summary>
    public class TagTypeTag : TemplateObject
    {
        // <--[object]
        // @Type TagTypeTag
        // @SubType TextTag
        // @Group Tag System
        // @Description Represents the type of a tag.
        // -->

        /// <summary>
        /// The represented tag type.
        /// </summary>
        public TagType Internal;

        /// <summary>
        /// Constructs a new TagTypeTag.
        /// </summary>
        /// <param name="type">The TagType to base this TagTypeTag off of.</param>
        public TagTypeTag(TagType type)
        {
            Internal = type;
        }

        /// <summary>
        /// Returns the type of this tag.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="input">The input text.</param>
        /// <returns>A TagTypeTag, or null.</returns>
        public static TagTypeTag For(TagData data, string input)
        {
            TagType type;
            if (data.TagSystem.Types.TryGetValue(input.ToLowerFast(), out type))
            {
                return new TagTypeTag(type);
            }
            return null;
        }

        /// <summary>
        /// The TagTypeTag type.
        /// </summary>
        public const string TYPE = "tagtypetag";

        /// <summary>
        /// Returns the input object as a TagTypeTag.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="input">The input object.</param>
        /// <returns>A TagTypeTag, or null.</returns>
        public static TagTypeTag For(TagData data, TemplateObject input)
        {
            return input as TagTypeTag ?? For(data, input.ToString());
        }

        /// <summary>
        /// All tag handlers for this tag type.
        /// </summary>
        public static Dictionary<string, TagSubHandler> Handlers = new Dictionary<string, TagSubHandler>();

        static TagTypeTag()
        {
            // <--[tag]
            // @Name TagTypeTag.for[<Tag>]
            // @Group Tag System
            // @ReturnType DynamicTag
            // @Returns a constructed instance of this tag type.
            // @Example "numbertag" .for[3] returns "3".
            // -->
            Handlers.Add("for", new TagSubHandler() { Handle = (data, obj) => new DynamicTag(((TagTypeTag)obj).Internal.TypeGetter(data, data.GetModifierObject(0))), ReturnTypeString = "dynamictag" });
            // Documented in TextTag.
            Handlers.Add("duplicate", new TagSubHandler() { Handle = (data, obj) => new TagTypeTag(((TagTypeTag)obj).Internal), ReturnTypeString = "tagtypetag" });
            // Documented in TextTag.
            Handlers.Add("type", new TagSubHandler() { Handle = (data, obj) => new TagTypeTag(data.TagSystem.Type_TagType), ReturnTypeString = "tagtypetag" });
            // Documented in TextTag.
            Handlers.Add("or_else", new TagSubHandler() { Handle = (data, obj) => obj, ReturnTypeString = "tagtypetag" });
        }

        /// <summary>
        /// Parse any direct tag input values.
        /// </summary>
        /// <param name="data">The input tag data.</param>
        public override TemplateObject Handle(TagData data)
        {
            if (data.Remaining == 0)
            {
                return this;
            }
            TagSubHandler handler;
            if (Handlers.TryGetValue(data[0], out handler))
            {
                return handler.Handle(data, this).Handle(data.Shrink());
            }
            return new TextTag(ToString()).Handle(data);
        }

        /// <summary>
        /// Returns the name of the tag type.
        /// </summary>
        /// <returns>The name.</returns>
        public override string ToString()
        {
            return Internal.TypeName;
        }
    }
}
