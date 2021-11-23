using System;
using Models.Core;

namespace Models
{
    [Serializable]
    public class Script : Model
    {
        [Description("x")]
        public string X { get; set; }
    }
}
