// Original code by Ben Ratzlaff
// http://www.codeproject.com/Articles/5996/Populating-a-PropertyGrid-using-Reflection-Emit

using System;
using System.Runtime.Serialization;

namespace NodeThing
{
    [DataContract]
    public class Setting
    {
        public event EventHandler ValueChanged;

        [DataMember]
        public object Value { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public string Category { get; set; }

        [DataMember]
        public string Name { get; set; }

        public EventHandler Listener
        {
            set { ValueChanged += value; }
        }

        /// <summary>
        /// Allows an external object to force calling the event
        /// </summary>
        public void TriggerUpdate(EventArgs e)
        {
            //I didnt do this in the Value's set method because sometimes I want to set the Value without firing the event
            //I could do the same thing with a second property, but this works fine.
            if (ValueChanged != null)
                ValueChanged(this, e);
        }
    }
}
