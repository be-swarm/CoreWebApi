using System;

namespace BeSwarm.CoreWebApi;

    //
    // validation fiels
    //
    // not null control for object
    //
    public class NotNull : Attribute
    {
        public NotNull()
        {
        }
    }

    //
    // hidden
    // used to hide value
    //
    public class Hidden : Attribute
    {
        public Hidden()
        {
        }
    }
    //
    // len control for strings
    //
    public class Len: Attribute
    {
        public int min { get; }   //-1=not controlled
        public int max { get; }   //-1=not controlled
        public Len(int _min, int _max)
        {
            this.min = _min;
            this.max = _max;
        }
    }
    //
    // range control for numeric,lists,dictionarys 
    //
    public class Dim : Attribute
    {
        public int min { get; }   //-1=not controlled
        public int max { get; }   //-1=not controlled
        public Dim(int _min, int _max)
        {
            this.min = _min;
            this.max = _max;
        }
    }
   
    // 
    // field description used by swagger
    //
    [System.AttributeUsage(System.AttributeTargets.Property)]
    public class Description : Attribute
    {
        public string description { get; set; }

        public Description(string _description)
        {
            description = _description;
        }
    }
