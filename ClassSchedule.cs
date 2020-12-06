using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MsTeamsBot
{
    class ClassSchedule
    {
        public string Name;
        public string SubjectChannel;
        public string Teacher;
        public DateTime Start;
        public DateTime End;
        public bool Completed;
        public ClassSchedule((string, string, string, DateTime, DateTime) p)
        {
            Completed = false;
            Name = p.Item1;
            SubjectChannel = p.Item2;
            Teacher = p.Item3;
            Start = p.Item4;
            End = p.Item5;
        }
        public (string, string, string, DateTime, DateTime) GetTuple()
        {
            return (Name, SubjectChannel, Teacher, Start, End);
        }
    }
}
