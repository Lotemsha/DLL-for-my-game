using System;
using System.Collections.Generic;
using System.Text;

namespace CoreClasses.Models
{
    public class NPC : Character
    {
        public string DialogueText { get; set; }
        public bool IsFriendly { get; set; }

        public NPC(string name) : base(name, 100, 2f, 100)
        {
            DialogueText = "Hello there!";
            IsFriendly = true;
        }
        public override void Movement() { }
    }
}
