﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace FreneticScript.TagHandlers.Objects
{
    /// <summary>
    /// Represents binary data.
    /// </summary>
    public class BinaryTag : TemplateObject
    {
        // <--[object]
        // @Type BinaryTag
        // @SubType TextTag
        // @Group Mathematics
        // @Description Represents binary data.
        // @Other Text form is little-endian hexadecimal.
        // -->

        /// <summary>
        /// The binary data this tag represents.
        /// </summary>
        public byte[] Internal;

        /// <summary>
        /// Get a binary tag relevant to the specified input, erroring on the command system if invalid input is given (Returns null in that case).
        /// </summary>
        /// <param name="dat">The TagData used to construct this BinaryTag.</param>
        /// <param name="input">The input text to create binary data from.</param>
        /// <returns>The binary tag.</returns>
        public static BinaryTag For(TagData dat, string input)
        {
            try
            {
                return new BinaryTag(StringToBytes(input));
            }
            catch (Exception ex)
            {
                if (ex is ThreadAbortException)
                {
                    throw ex;
                }
                if (!dat.HasFallback)
                {
                    dat.TagSystem.CommandSystem.Output.WriteLine(ex.ToString());
                    dat.Error("Invalid binary data: '" + TagParser.Escape(input) + "'!");
                }
                return null;
            }
        }

        static byte[] StringToBytes(string hex)
        {
            int l = hex.Length >> 1;
            byte[] arr = new byte[l];
            for (int i = 0; i < l; i++)
            {
                arr[i] = (byte)((GetHexVal(hex[i << 1])) + (GetHexVal(hex[(i << 1) + 1]) << 4));
            }
            return arr;
        }

        static int GetHexVal(char chr)
        {
            return chr - (chr < 58 ? 48 : (chr < 97 ? 55 : 87));
        }

        static char GetHexChar(int val)
        {
            return (char)((val < 10) ? ('0' + val) : ('A' + (val - 10)));
        }

        /// <summary>
        /// Get a binary tag relevant to the specified input, erroring on the command system if invalid input is given (Returns null in that case).
        /// </summary>
        /// <param name="dat">The TagData used to construct this BinaryTag.</param>
        /// <param name="input">The input to create or get binary data from.</param>
        /// <returns>The binary tag.</returns>
        public static BinaryTag For(TagData dat, TemplateObject input)
        {
            return input as BinaryTag ?? For(dat, input.ToString());
        }

        /// <summary>
        /// Constructs a binary tag.
        /// </summary>
        /// <param name="_val">The internal binary data to use.</param>
        public BinaryTag(byte[] _val)
        {
            Internal = _val.ToArray();
        }

        /// <summary>
        /// The BinaryTag type.
        /// </summary>
        public const string TYPE = "binarytag";

        /// <summary>
        /// Creates a BinaryTag for the given input data.
        /// </summary>
        /// <param name="dat">The tag data.</param>
        /// <param name="input">The text input.</param>
        /// <returns>A valid binary tag.</returns>
        public static TemplateObject CreateFor(TagData dat, TemplateObject input)
        {
            return input is BinaryTag ? (BinaryTag)input : For(dat, input.ToString());
        }

#pragma warning disable 1591

        [TagMeta(TagType = TYPE, Name = "byte_at", Group = "Binary Data", ReturnType = IntegerTag.TYPE, Returns = "The integer version of the byte at a specific 1-based index.",
            Examples = new string[] { "'102030' .byte_at[1] returns '1'." })]
        public static TemplateObject Tag_Byte_At(TagData data, TemplateObject obj)
        {
            byte[] Internal = (obj as BinaryTag).Internal;
            int ind = (int)IntegerTag.For(data, data.GetModifier(0)).Internal;
            if (ind < 1 || ind > Internal.Length)
            {
                if (!data.HasFallback)
                {
                    data.Error("Invalid byte_at tag: " + ind + " is not in the exclusive range of 1 to " + Internal.Length);
                }
                return new NullTag();
            }
            return new IntegerTag(Internal[ind - 1]);
        }

        [TagMeta(TagType = TYPE, Name = "byte_list", Group = "Binary Data", ReturnType = ListTag.TYPE, Returns = "A list of integer versions of the bytes in this binary tag.",
            Examples = new string[] { "'102030' .byte_list returns '1|2|3|'." })]
        public static TemplateObject Tag_Byte_List(TagData data, TemplateObject obj)
        {
            byte[] Internal = (obj as BinaryTag).Internal;
            List<TemplateObject> objs = new List<TemplateObject>(Internal.Length);
            for (int i = 0; i < Internal.Length; i++)
            {
                objs.Add(new IntegerTag(Internal[i]));
            }
            return new ListTag(objs);
        }

        [TagMeta(TagType = TYPE, Name = "range", Group = "Binary Data", ReturnType = TYPE, Returns = "The specified set of bytes in the binary data.",
            Examples = new string[] { "'10203040' .range[2,3] returns '2030'.", "'10203040' .range[2,2] returns '20'." }, Others = new String[] { "Note that indices are one-based." })]
        public static TemplateObject Tag_Range(TagData data, TemplateObject obj)
        {
            byte[] Internal = (obj as BinaryTag).Internal;
            string modif = data.GetModifier(0);
            string[] split = modif.SplitFast(',');
            if (split.Length != 2)
            {
                data.Error("Invalid comma-separated-twin-number input: '" + TagParser.Escape(modif) + "'!");
                return new NullTag();
            }
            IntegerTag num1 = IntegerTag.For(data, split[0]);
            IntegerTag num2 = IntegerTag.For(data, split[1]);
            if (Internal.Length == 0)
            {
                data.Error("Read 'range' tag on empty BinaryTag!");
                return new NullTag();
            }
            if (num1 == null || num2 == null)
            {
                data.Error("Invalid integer input: '" + TagParser.Escape(modif) + "'!");
                return new NullTag();
            }
            int number = (int)num1.Internal - 1;
            int number2 = (int)num2.Internal - 1;
            if (number < 0)
            {
                number = 0;
            }
            if (number2 < 0)
            {
                number2 = 0;
            }
            if (number >= Internal.Length)
            {
                data.Error("Invalid range tag!");
                return new NullTag();
            }
            if (number2 >= Internal.Length)
            {
                data.Error("Invalid range tag!");
                return new NullTag();
            }
            if (number2 < number)
            {
                data.Error("Invalid range tag!");
                return new NullTag();
            }
            byte[] ndat = new byte[number2 - number + 1];
            Array.Copy(Internal, number, ndat, 0, ndat.Length);
            return new BinaryTag(ndat);
        }

        [TagMeta(TagType = TYPE, Name = "to_integer", Group = "Conversion", ReturnType = IntegerTag.TYPE, Returns = "The internal data converted to an integer value.",
            Examples = new string[] { "'1000000000000000' .to_integer returns '1'." }, Others = new String[] { "Note that this currently must be of length: 1, 2, 4, or 8 bytes." })]
        public static TemplateObject Tag_To_Integer(TagData data, TemplateObject obj)
        {
            byte[] Internal = (obj as BinaryTag).Internal;
            switch (Internal.Length)
            {
                case 1:
                    return new IntegerTag(Internal[0]).Handle(data.Shrink());
                case 2:
                    return new IntegerTag(BitConverter.ToInt16(Internal, 0));
                case 4:
                    return new IntegerTag(BitConverter.ToInt32(Internal, 0));
                case 8:
                    return new IntegerTag(BitConverter.ToInt64(Internal, 0));
                default:
                    if (!data.HasFallback)
                    {
                        data.Error("Invalid to_integer binary data length: " + Internal.Length);
                    }
                    return new NullTag();
            }
        }

        [TagMeta(TagType = TYPE, Name = "to_number", Group = "Conversion", ReturnType = NumberTag.TYPE, Returns = "The internal data converted to an floating-point number value.",
            Examples = new string[] { "'0000000000000FF3' .to_number returns '1'." }, Others = new String[] { "Note that this currently must be of length: 4, or 8 bytes." })]
        public static TemplateObject Tag_To_Number(TagData data, TemplateObject obj)
        {
            byte[] Internal = (obj as BinaryTag).Internal;
            switch (Internal.Length)
            {
                case 4:
                    return new NumberTag(BitConverter.ToSingle(Internal, 0));
                case 8:
                    return new NumberTag(BitConverter.ToDouble(Internal, 0));
                default:
                    if (!data.HasFallback)
                    {
                        data.Error("Invalid to_number binary data length: " + Internal.Length);
                    }
                    return new NullTag();
            }
        }

        [TagMeta(TagType = TYPE, Name = "from_utf8", Group = "Conversion", ReturnType = TextTag.TYPE, Returns = "The text that is represented by this UTF8 binary data.",
            Examples = new string[] { "'8696' .from_utf8 returns 'hi'." }, Others = new String[] { "Can be reverted via <@link tag TextTag.to_utf8_binary>TextTag.to_utf8_binary<@/link>." })]
        public static TemplateObject Tag_From_UTF8(TagData data, TemplateObject obj)
        {
            return new TextTag(new UTF8Encoding(false).GetString((obj as BinaryTag).Internal));
        }

        [TagMeta(TagType = TYPE, Name = "to_base64", Group = "Conversion", ReturnType = TextTag.TYPE, Returns = "A Base-64 text representation of this binary data.",
            Examples = new string[] { "'8696' .to_base64 returns 'aGk='." })]
        public static TemplateObject Tag_To_Base64(TagData data, TemplateObject obj)
        {
            return new TextTag(Convert.ToBase64String((obj as BinaryTag).Internal));
        }

        [TagMeta(TagType = TYPE, Name = "duplicate", Group = "Tag System", ReturnType = TYPE, Returns = "A perfect duplicate of this object.",
            Examples = new string[] { "'102030' .duplicate returns '102030'." })]
        public static TemplateObject Tag_Duplicate(TagData data, TemplateObject obj)
        {
            return new BinaryTag((obj as BinaryTag).Internal);
        }

        [TagMeta(TagType = TYPE, Name = "type", Group = "Tag System", ReturnType = TagTypeTag.TYPE, Returns = "The type of the tag.",
            Examples = new string[] { "'true' .type returns 'booleantag'." })]
        public static TemplateObject Tag_Type(TagData data, TemplateObject obj)
        {
            return new TagTypeTag(data.TagSystem.Type_Binary);
        }

#pragma warning restore 1591

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
            switch (data[0])
            {
                default:
                    return new TextTag(ToString()).Handle(data);
            }
        }

        /// <summary>
        /// Returns the a string representation of the binary data internally stored by this binary tag.
        /// This returns in little-endian hexadecimal format.
        /// </summary>
        /// <returns>A string representation of the binary data.</returns>
        public override string ToString()
        {
            if (Internal == null)
            {
                return "";
            }
            char[] res = new char[Internal.Length * 2];
            for (int i = 0; i < Internal.Length; i++)
            {
                res[i << 1] = GetHexChar(Internal[i] & 0x0F);
                res[(i << 1) + 1] = GetHexChar((Internal[i] & 0xF0) >> 4);
            }
            return new string(res);
        }
    }
}
