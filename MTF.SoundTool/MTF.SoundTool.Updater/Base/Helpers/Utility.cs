/*
    This file is part of RESIDENT EVIL STQ Tool.
    RESIDENT EVIL STQ Tool is free software: you can redistribute it
    and/or modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation, either version 3 of
    the License, or (at your option) any later version.
    RESIDENT EVIL STQ Tool is distributed in the hope that it will
    be useful, but WITHOUT ANY WARRANTY; without even the implied
    warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
    See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License
    along with RESIDENT EVIL STQ Tool. If not, see <https://www.gnu.org/licenses/>6.
*/

using System;

namespace MTF.SoundTool.Updater.Base.Helpers
{
    public class Utility
    {
        public static string StringBetween(string strSource, string strStart, string strEnd)
        {
            if (!strSource.Contains(strStart) || !strSource.Contains(strEnd)) 
                return "";

            int Start = strSource.IndexOf(strStart, 0) + strStart.Length;
            int End = strSource.IndexOf(strEnd, Start, StringComparison.Ordinal);
            return strSource.Substring(Start, End - Start);
        }
    }
}
