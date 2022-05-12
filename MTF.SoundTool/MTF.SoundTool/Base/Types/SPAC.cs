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

using System.Collections.Generic;

namespace MTF.SoundTool.Base.Types
{
    public class SPAC
    {
        public List<FWSE> FWSEFiles { get; set; }
        public List<XSEW> XSEWFiles { get; set; }

        public string Format { get; set; }
        public int Version { get; set; }
        public int Sounds { get; set; }
        public int UnknownDataA { get; set; }
        public int UnknownDataB { get; set; }
        public int MetaAStart { get; set; }
        public int MetaBStart { get; set; }
        public int SoundDataStart { get; set; }

        public byte[] MetaA { get; set; }
        public byte[] MetaB { get; set; }
    }
}
