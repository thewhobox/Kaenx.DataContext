using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.DataContext.Catalog
{
    public class AppComObject
    {
        [Key]
        public int UId { get; set; }
        public int Id { get; set; }
        public int ApplicationId { get; set; }

        [MaxLength(100)]
        public string Text { get; set; }
        [MaxLength(100)]
        public string FunctionText { get; set; }

        public bool Flag_Read { get; set; }
        public bool Flag_Write { get; set; }
        public bool Flag_Communicate { get; set; }
        public bool Flag_Transmit { get; set; }
        public bool Flag_Update { get; set; }
        public bool Flag_ReadOnInit { get; set; }

        public string Group { get; set; }
        public int Number { get; set; }
        public int Size { get; set; }
        public int Datapoint { get; set; }
        public int DatapointSub { get; set; }

        public void LoadComp(AppComObject com)
        {
            ApplicationId = com.ApplicationId;
            Text = com.Text;
            FunctionText = com.FunctionText;
            Flag_Read = com.Flag_Read;
            Flag_Write = com.Flag_Write;
            Flag_Communicate = com.Flag_Communicate;
            Flag_Transmit = com.Flag_Transmit;
            Flag_Update = com.Flag_Update;
            Flag_ReadOnInit = com.Flag_ReadOnInit;
            Group = com.Group;
            Number = com.Number;
            Size = com.Size;
            Datapoint = com.Datapoint;
            DatapointSub = com.DatapointSub;
        }

        public void SetSize(string size)
        {
            if (string.IsNullOrEmpty(size))
            {
                Size = -1;
                return;
            }
            string[] splitted = size.Split(' ');
            int i = int.Parse(splitted[0]);
            int m = (splitted[1].StartsWith("Byte")) ? 8 : 1;
            Size = i * m;
        }

        public void SetDatapoint(string dp)
        {
            if (string.IsNullOrEmpty(dp))
            {
                Datapoint = -1;
                DatapointSub = -1;
                return;
            }

            if (dp.Contains(" "))
                dp = dp.Substring(0, dp.IndexOf(' '));

            string[] splitted = dp.Split('-');

            if (splitted[0] == "DPT")
            {
                Datapoint = int.Parse(splitted[1]);
                DatapointSub = -1;
            }
            else
            {
                Datapoint = int.Parse(splitted[1]);
                DatapointSub = int.Parse(splitted[2]);
            }
        }
    }
}
