/*
    This file is part of RESIDENT EVIL SPC Tool.
    RESIDENT EVIL STQ Tool is free software: you can redistribute it
    and/or modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation, either version 3 of
    the License, or (at your option) any later version.
    RESIDENT EVIL STQ Tool is distributed in the hope that it will
    be useful, but WITHOUT ANY WARRANTY; without even the implied
    warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
    See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License
    along with RESIDENT EVIL SPC Tool. If not, see <https://www.gnu.org/licenses/>6.
*/

using System;
using System.Net.NetworkInformation;

namespace MTF.SoundTool.Base.Helpers
{
    public static class Utility
    {
        public static bool TestConnection(string HostNameOrAddress, int Timeout = 1000)
        {
            try
            {
                Ping myPing = new Ping();
                PingReply reply = myPing.Send(HostNameOrAddress, Timeout, new byte[32]);
                return (reply.Status == IPStatus.Success);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static int Clamp(int Value, int Min, int Max)
        {
            return Value < Min ? Min : Value > Max ? Max : Value;
        }
    }
}
