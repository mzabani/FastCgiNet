using System;

namespace FastCgiNet
{
    internal class Html
    {
        /// <summary>
        /// Escapes Url Fragments.
        /// </summary>
        public static string EscapeUrlFragment(string contents)
        {
            //TODO: Fully implement this
            return contents.Replace(" ", "%20");
        }
    }
}
