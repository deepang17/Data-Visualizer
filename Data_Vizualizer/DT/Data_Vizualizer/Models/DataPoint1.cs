using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace Data_Vizualizer.Models
{
    public class DataPoint1
    {
        public DataPoint1(string Label, double[] Y)
        {
            this.label = Label;
            this.y = Y;
        }

        //Explicitly setting the name to be used while serializing to JSON.
        [DataMember(Name = "Label")]
        public string label = "";

        //Explicitly setting the name to be used while serializing to JSON.
        [DataMember(Name = "Y")]
        public double[] y = null;
    }
}