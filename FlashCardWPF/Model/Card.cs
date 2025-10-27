using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlashCardWPF.Model
{
    public class Card
    {
        public string Front {  get; set; }
        public string Back { get; set; }

        public DateTime ShowCardWhen { get; set; } = DateTime.Now;

        public Card(string question, string answer)
        {
            Front = question;
            Back = answer;
        }

        
    }
}
