using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xwcs.core.net
{
    public class MimeInfo
    {

        /// <summary>
        /// Whole mime f.e. "image/png"
        /// </summary>
        public string Mime { get; private set; }
        /// <summary>
        /// type name   f.e. "image"
        /// </summary>
        public string Type { get; private set; }
        /// <summary>
        /// file extension fe.  ".png"
        /// </summary>
        public string Extension { get; private set; }

        private string GetMediaType()
        {
            switch (Extension)
            {
                case "rtf": return "rtf";
                case "pdf": return "pdf";
                default:
                    switch (Mime.GetUntilOrEmpty("/"))
                    {
                        case "image": return "image";
                        default: return "not_categorized";
                    }
            }
        }

        public MimeInfo (string fName)
        {
            Extension = Path.GetExtension(fName).Trim('.').ToLower();
            Mime = MimeTypes.MimeTypeMap.GetMimeType(Extension);
            Type = GetMediaType();
        }
    }

    static class Helper
    {
        public static string GetUntilOrEmpty(this string text, string stopAt = "-")
        {
            if (!String.IsNullOrWhiteSpace(text))
            {
                int charLocation = text.IndexOf(stopAt, StringComparison.Ordinal);

                if (charLocation > 0)
                {
                    return text.Substring(0, charLocation);
                }
            }

            return String.Empty;
        }
    }
}
