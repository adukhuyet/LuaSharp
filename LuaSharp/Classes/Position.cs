using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuaSharp.Classes
{
    class Position
    {
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }

        public Position(float x, float y, float z = 0)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }
}
