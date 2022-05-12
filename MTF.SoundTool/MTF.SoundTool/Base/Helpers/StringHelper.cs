/*
    This file is part of MTF Sound Tool.
    MTF Sound Tool is free software: you can redistribute it
    and/or modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation, either version 3 of
    the License, or (at your option) any later version.
    MTF Sound Tool is distributed in the hope that it will
    be useful, but WITHOUT ANY WARRANTY; without even the implied
    warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
    See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License
    along with MTF Sound Tool. If not, see <https://www.gnu.org/licenses/>6.
*/

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
