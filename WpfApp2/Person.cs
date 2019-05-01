using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Lab01
{
    public class Person
    {
        
        private string name;
        private string filename;
        public string Name
        {
            get { return name; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Username is required.");
                name = value;
            }
        }
        public int Age { get; set; }
        public string Filename
        {
            get { return filename; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("You have to load image.");
                filename = value;
            }
        }
    }
}
