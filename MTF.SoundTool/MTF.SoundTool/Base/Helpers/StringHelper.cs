using System.Text.RegularExpressions;

namespace MTF.SoundTool.Base.Helpers
{
    public static class StringHelper
    {
        private static readonly Regex WhiteSpace = new Regex(@"\s+");

        public static string ReplaceWhiteSpace(string Input, string Replacement = "")
        {
            return WhiteSpace.Replace(Input, Replacement);
        }

        public static string ValidatePath(string Path)
        {
            if (Path.Contains("/"))
                Path = Path.Replace("/", @"\");

            if (Path[0] == '\\')
                Path = Path.Substring(1, Path.Length - 1);

            if (Path[Path.Length - 1] != '\\')
                Path += @"\";

            return ReplaceWhiteSpace(Path);
        }

        public static string ValidateName(string Name)
        {
            if (Name.Contains(@"\"))
                Name = Name.Replace(@"\", "");

            if (Name.Contains("/"))
                Name = Name.Replace("/", "");

            if (Name.Contains("-"))
                Name = Name.Replace("-", "");

            return ReplaceWhiteSpace(Name);
        }
    }
}
