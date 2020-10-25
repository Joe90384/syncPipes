using System;
using UnityEngine;

namespace Oxide.Plugins
{
    public partial class SyncPipes
    {
        /// <summary>
        /// Base class for language attributes
        /// </summary>
        public class LanguageAttribute : Attribute
        {
            protected LanguageAttribute(string text)
            {
                Text = text;
            }

            public string Text { get; }
        }

        [AttributeUsage(AttributeTargets.Enum)]
        public class EnumWithLanguageAttribute : Attribute { }

        /// <summary>
        /// Helper for Overlay Messages to indicate the message type and allow the overlay helpers to render them correctly
        /// </summary>
        public class MessageTypeAttribute : Attribute
        {
            public MessageTypeAttribute(MessageType type)
            {
                Type = type;
            }
            public MessageType Type { get; }
        }

        /// <summary>
        /// Will be used to get the text for the english language pack
        /// </summary>
        public class EnglishAttribute : LanguageAttribute
        {
            public EnglishAttribute(string text) : base(text) { }
            public const string Language = "en";
        }

        // This will treat the first stirng.format replace as the chat command "{0}"
        /// <summary>
        /// Indicated this item is a Chat command.
        /// The first string.Format "{0}" will be replaced with the chat command string.
        /// All args will continue normally from "{1}"
        /// </summary>
        public class ChatCommandAttribute : Attribute { }

        /// <summary>
        /// Indicated this item is a Binding command.
        /// The first string.Format "{0}" will be replaced with the binding key.
        /// All args will continue normally from "{1}"
        /// </summary>
        public class BindingCommandAttribute : Attribute { }

        /// <summary>
        /// This attribute holds the details of a container entity
        /// </summary>
        public class StorageAttribute : Attribute
        {
            public StorageAttribute(string shortname, string url, float xOffset = 0, float yOffset = 0, float zOffset = 0, bool partialUrl = true)
            {
                ShortName = shortname;
                Url = url;
                PartialUrl = partialUrl;
                Offset = new Vector3(xOffset, yOffset, zOffset);
            }

            /// <summary>
            /// The url or partial url of an container entity
            /// </summary>
            public readonly string Url;

            /// <summary>
            /// The shortname of a container entity. Currently not used but may be useful for debugging
            /// </summary>
            public readonly string ShortName;

            /// <summary>
            /// Indicates if this is attribute contains a full or partial url
            /// </summary>
            public readonly bool PartialUrl;

            /// <summary>
            /// In game offset of the pipe end points
            /// </summary>
            public readonly Vector3 Offset;
        }

    }
}
